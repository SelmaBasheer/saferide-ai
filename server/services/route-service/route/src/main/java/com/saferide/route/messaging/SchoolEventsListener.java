package com.saferide.route.messaging;

import com.saferide.route.projection.SchoolStatusProjector;
import com.saferide.route.projection.SchoolStatuses;
import java.time.Instant;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.stereotype.Component;

@Component
public class SchoolEventsListener {

    public record SchoolEventPayload(UUID schoolId, Instant occurredAtUtc) {}

    private static final Logger log = LoggerFactory.getLogger(SchoolEventsListener.class);

    private final SchoolStatusProjector schoolStatus;

    public SchoolEventsListener(SchoolStatusProjector schoolStatus) {
        this.schoolStatus = schoolStatus;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.school-events-queue}")
    public void handle(SchoolEventPayload event, @Header(AmqpHeaders.RECEIVED_ROUTING_KEY) String routingKey) {
        String status;
        if (event == null || event.schoolId() == null || event.occurredAtUtc() == null) {
            log.warn("Discarding malformed school event, routingKey={}", routingKey);
            return;
        }
        if ("school-approved".equals(routingKey)) {
            status = SchoolStatuses.APPROVED;
        } else if ("school-suspended".equals(routingKey)) {
            status = SchoolStatuses.SUSPENDED;
        } else {
            log.warn("Ignoring unmapped school routing key: {}", routingKey);
            return;
        }
        schoolStatus.upsert(event.schoolId(), status, event.occurredAtUtc());
        log.info("School {} projected as {}", event.schoolId(), status);
    }
}
