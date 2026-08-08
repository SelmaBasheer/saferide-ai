package com.saferide.student.infrastructure.adapter.in.messaging;

import com.saferide.student.application.port.SchoolStatusPort;
import com.saferide.student.application.port.SchoolStatuses;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.amqp.support.AmqpHeaders;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.stereotype.Component;

@Component
public class SchoolEventsListener {

    public record SchoolEventPayload(UUID schoolId) {}

    private static final Logger log = LoggerFactory.getLogger(SchoolEventsListener.class);
    private final SchoolStatusPort schoolStatus;

    public SchoolEventsListener(SchoolStatusPort schoolStatus) {
        this.schoolStatus = schoolStatus;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.school-events-queue}")
    public void handle(SchoolEventPayload event, @Header(AmqpHeaders.RECEIVED_ROUTING_KEY) String routingKey) {
        String status = "school-approved".equals(routingKey) ? SchoolStatuses.APPROVED : SchoolStatuses.SUSPENDED;
        schoolStatus.upsert(event.schoolId(), status);
        log.info("School {} projected as {}", event.schoolId(), status);
    }
}
