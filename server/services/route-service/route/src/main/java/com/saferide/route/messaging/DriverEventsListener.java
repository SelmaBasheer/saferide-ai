package com.saferide.route.messaging;

import com.saferide.route.projection.DriverStatus;
import java.time.Instant;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.query.Criteria;
import org.springframework.data.mongodb.core.query.Query;
import org.springframework.data.mongodb.core.query.Update;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.stereotype.Component;

/**
 * Driver identity has two forms in this system: the driver *record* id, which
 * bus-service uses when assigning a driver to a bus, and the identity *user*
 * id, which the gateway puts on every request. Nothing joins them except the
 * email address, so route-service projects it — along with whether the driver
 * is still employed, since a deactivated driver must stop seeing their route.
 */
@Component
public class DriverEventsListener {

    /** One record for every event on this queue; absent fields arrive as null. */
    public record DriverEventPayload(UUID id, UUID schoolId, String email, String status, Instant occurredAt) {}

    private static final Logger log = LoggerFactory.getLogger(DriverEventsListener.class);

    private final MongoTemplate mongo;

    public DriverEventsListener(MongoTemplate mongo) {
        this.mongo = mongo;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.driver-events-queue:route.driver-events}")
    public void handle(DriverEventPayload event, @Header(AmqpHeaders.RECEIVED_ROUTING_KEY) String routingKey) {
        if (event == null || event.id() == null || event.schoolId() == null) {
            log.warn("Discarding malformed driver event, routingKey={}", routingKey);
            return;
        }

        Instant occurredAt = event.occurredAt() != null ? event.occurredAt() : Instant.now();

        switch (routingKey) {
            case "driver-created" -> created(event, occurredAt);
            case "driver-deactivated" -> statusChanged(event, "Inactive", occurredAt);
            case "driver-reactivated" -> statusChanged(event, "Active", occurredAt);
            default -> log.warn("Ignoring unmapped driver routing key: {}", routingKey);
        }
    }

    private void created(DriverEventPayload event, Instant occurredAt) {
        if (event.email() == null) {
            log.warn("Driver {} created without an email — cannot resolve logins", event.id());
            return;
        }

        Query newerThanStored = new Query(Criteria.where("_id")
                .is(event.id())
                .orOperator(
                        Criteria.where("eventAtUtc").is(null),
                        Criteria.where("eventAtUtc").lt(occurredAt)));

        Update update = new Update()
                .set("schoolId", event.schoolId())
                .set("email", event.email().trim().toLowerCase())
                .set("eventAtUtc", occurredAt)
                .set("updatedAt", Instant.now())
                // setOnInsert, not set: a replayed creation must never revive a
                // driver who has since been deactivated.
                .setOnInsert("status", "Active");

        if (mongo.updateFirst(newerThanStored, update, DriverStatus.class).getMatchedCount() == 0) {
            mongo.upsert(new Query(Criteria.where("_id").is(event.id())), update, DriverStatus.class);
        }

        log.info("Driver {} projected", event.id());
    }

    private void statusChanged(DriverEventPayload event, String status, Instant occurredAt) {
        Query newerThanStored = new Query(Criteria.where("_id")
                .is(event.id())
                .orOperator(
                        Criteria.where("statusEventAtUtc").is(null),
                        Criteria.where("statusEventAtUtc").lt(occurredAt)));

        Update update = new Update()
                .set("schoolId", event.schoolId())
                .set("status", status)
                .set("statusEventAtUtc", occurredAt)
                .set("updatedAt", Instant.now());

        if (mongo.updateFirst(newerThanStored, update, DriverStatus.class).getMatchedCount() == 0) {
            // The status event can outrun the creation event. Inserting a row
            // with no email is correct — it cannot match a login until the
            // creation event fills the email in, which is the safe direction.
            mongo.upsert(new Query(Criteria.where("_id").is(event.id())), update, DriverStatus.class);
        }

        log.info("Driver {} status projected: {}", event.id(), status);
    }
}
