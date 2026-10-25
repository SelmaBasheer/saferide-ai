package com.saferide.route.messaging;

import com.saferide.route.projection.DriverStatus;
import java.time.Instant;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.query.Criteria;
import org.springframework.data.mongodb.core.query.Query;
import org.springframework.data.mongodb.core.query.Update;
import org.springframework.stereotype.Component;

/**
 * Driver identity has two forms in this system: the driver *record* id, which
 * bus-service uses when assigning a driver to a bus, and the identity *user*
 * id, which the gateway puts on every request. Nothing joins them except the
 * email address, so route-service projects it.
 */
@Component
public class DriverEventsListener {

    /** Mirrors DriverService's DriverCreatedEvent; unknown fields are ignored. */
    public record DriverCreatedPayload(UUID id, UUID schoolId, String email, Instant occurredAt) {}

    private static final Logger log = LoggerFactory.getLogger(DriverEventsListener.class);

    private final MongoTemplate mongo;

    public DriverEventsListener(MongoTemplate mongo) {
        this.mongo = mongo;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.driver-events-queue:route.driver-events}")
    public void handle(DriverCreatedPayload event) {
        if (event == null || event.id() == null || event.schoolId() == null || event.email() == null) {
            log.warn("Discarding malformed driver event");
            return;
        }

        Instant occurredAt = event.occurredAt() != null ? event.occurredAt() : Instant.now();

        Query newerThanStored = new Query(Criteria.where("_id")
                .is(event.id())
                .orOperator(
                        Criteria.where("eventAtUtc").is(null),
                        Criteria.where("eventAtUtc").lt(occurredAt)));

        Update update = new Update()
                .set("schoolId", event.schoolId())
                .set("email", event.email().trim().toLowerCase())
                .set("eventAtUtc", occurredAt)
                .set("updatedAt", Instant.now());

        if (mongo.updateFirst(newerThanStored, update, DriverStatus.class).getMatchedCount() == 0) {
            // No row yet. Replaying an older event onto an existing row is the
            // only other case, and the guarded update above already refused it.
            mongo.upsert(new Query(Criteria.where("_id").is(event.id())), update, DriverStatus.class);
        }

        log.info("Driver {} projected", event.id());
    }
}
