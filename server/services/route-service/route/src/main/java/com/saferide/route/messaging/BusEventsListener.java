package com.saferide.route.messaging;

import com.saferide.route.projection.BusStatusProjector;
import java.time.Instant;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.stereotype.Component;

@Component
public class BusEventsListener {

    public record BusStatusPayload(
            UUID busId,
            UUID schoolId,
            String registrationNumber,
            boolean active,
            boolean documentsValid,
            Instant occurredAtUtc) {}

    private static final Logger log = LoggerFactory.getLogger(BusEventsListener.class);

    private final BusStatusProjector busStatus;

    public BusEventsListener(BusStatusProjector busStatus) {
        this.busStatus = busStatus;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.bus-events-queue}")
    public void handle(BusStatusPayload event, @Header(AmqpHeaders.RECEIVED_ROUTING_KEY) String routingKey) {
        if (event == null || event.busId() == null || event.schoolId() == null || event.occurredAtUtc() == null) {
            log.warn("Discarding malformed bus event, routingKey={}", routingKey);
            return;
        }

        if (!"bus-status-changed".equals(routingKey)) {
            log.warn("Ignoring unmapped bus routing key: {}", routingKey);
            return;
        }

        busStatus.upsert(
                event.busId(),
                event.schoolId(),
                event.registrationNumber(),
                event.active(),
                event.documentsValid(),
                event.occurredAtUtc());
    }
}
