package com.saferide.notification.infrastructure.messaging;

import com.saferide.notification.application.event.SchoolApproved;
import com.saferide.notification.application.event.SchoolRejected;
import com.saferide.notification.application.event.SchoolSubmittedForApproval;
import com.saferide.notification.application.service.SchoolEmailService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
public class SchoolEventConsumer {
    private final SchoolEmailService schoolEmailService;
    private static final Logger log = LoggerFactory.getLogger(OtpEventConsumer.class);

    public SchoolEventConsumer(SchoolEmailService schoolEmailService) {
        this.schoolEmailService = schoolEmailService;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.school-submitted-queue}")
    public void onSubmitted(SchoolSubmittedForApproval event) {
        schoolEmailService.sendSubmitted(event);
        log.info("Sent submitted-for-review email for school {}", event.schoolId());
    }

    @RabbitListener(queues = "${saferide.rabbitmq.school-approved-queue}")
    public void onApproved(SchoolApproved event) {
        schoolEmailService.sendApproved(event);
        log.info("Sent school approved email for school {}", event.schoolId());
    }

    @RabbitListener(queues = "${saferide.rabbitmq.school-rejected-queue}")
    public void onRejected(SchoolRejected event) {
        schoolEmailService.sendRejected(event);
        log.info("Sent school rejected email for school {}", event.schoolId());
    }
}
