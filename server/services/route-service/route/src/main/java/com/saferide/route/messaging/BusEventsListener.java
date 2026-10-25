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

    /**
     * One record for both event shapes on this queue. The converter ignores
     * unknown properties and leaves absent ones null, so a status event carries
     * no driverId and a driver event carries no registrationNumber.
     *
     * <p>The boxed Boolean is deliberate: a primitive would silently become
     * false on a driver event, which is a different claim from "not stated".
     */
    public record BusEventPayload(
            UUID busId,
            UUID schoolId,
            String registrationNumber,
            Boolean active,
            Boolean documentsValid,
            UUID driverId,
            Instant occurredAtUtc) {}

    private static final Logger log = LoggerFactory.getLogger(BusEventsListener.class);

    private final BusStatusProjector busStatus;

    public BusEventsListener(BusStatusProjector busStatus) {
        this.busStatus = busStatus;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.bus-events-queue}")
    public void handle(BusEventPayload event, @Header(AmqpHeaders.RECEIVED_ROUTING_KEY) String routingKey) {
        if (event == null || event.busId() == null || event.schoolId() == null || event.occurredAtUtc() == null) {
            log.warn("Discarding malformed bus event, routingKey={}", routingKey);
            return;
        }

        switch (routingKey) {
            case "bus-status-changed" -> busStatus.upsert(
                    event.busId(),
                    event.schoolId(),
                    event.registrationNumber(),
                    Boolean.TRUE.equals(event.active()),
                    Boolean.TRUE.equals(event.documentsValid()),
                    event.occurredAtUtc());

            case "bus-driver-assigned" -> {
                if (event.driverId() == null) {
                    log.warn("Driver assignment for bus {} carried no driverId", event.busId());
                    return;
                }
                busStatus.assignDriver(event.busId(), event.schoolId(), event.driverId(), event.occurredAtUtc());
            }

            default -> log.warn("Ignoring unmapped bus routing key: {}", routingKey);
        }
    }
}
