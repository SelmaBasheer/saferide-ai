package com.saferide.bus.messaging;

import com.saferide.bus.projection.SchoolStatusProjector;
import com.saferide.bus.projection.SchoolStatuses;
import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.stereotype.Component;

@Component
public class SchoolEventsListener {

    /**
     * One record for every school event, because they share a queue and the
     * routing key decides which fields matter. Approve and suspend leave the
     * last three null; a subscription change fills them. Unknown properties
     * are ignored by the converter, so neither side has to know about the
     * other's fields.
     *
     * <p>{@code status} here is the <em>subscription</em> status, because that
     * is what the publisher calls it. The school's own status is derived from
     * the routing key, not from a field.
     */
    public record SchoolEventPayload(
            UUID schoolId, Instant occurredAtUtc, String status, Integer busLimit, LocalDate endsOn) {}

    private static final Logger log = LoggerFactory.getLogger(SchoolEventsListener.class);

    private final SchoolStatusProjector schoolStatus;

    public SchoolEventsListener(SchoolStatusProjector schoolStatus) {
        this.schoolStatus = schoolStatus;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.school-events-queue}")
    public void handle(SchoolEventPayload event, @Header(AmqpHeaders.RECEIVED_ROUTING_KEY) String routingKey) {
        if (event == null || event.schoolId() == null || event.occurredAtUtc() == null) {
            log.warn("Discarding malformed school event, routingKey={}", routingKey);
            return;
        }

        switch (routingKey) {
            case "school-approved" -> project(event, SchoolStatuses.APPROVED);
            case "school-suspended" -> project(event, SchoolStatuses.SUSPENDED);
            case "school-subscription-changed" -> {
                schoolStatus.upsertEntitlement(
                        event.schoolId(), event.busLimit(), event.status(), event.endsOn(), event.occurredAtUtc());

                log.info(
                        "School {} entitlement projected: subscription={} busLimit={}",
                        event.schoolId(),
                        event.status(),
                        event.busLimit());
            }
            default -> log.warn("Ignoring unmapped school routing key: {}", routingKey);
        }
    }

    private void project(SchoolEventPayload event, String status) {
        schoolStatus.upsert(event.schoolId(), status, event.occurredAtUtc());
        log.info("School {} projected as {}", event.schoolId(), status);
    }
}
