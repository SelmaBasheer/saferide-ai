package com.saferide.notification.infrastructure.messaging;

import com.saferide.notification.application.event.SchoolApproved;
import com.saferide.notification.application.event.SchoolRejected;
import com.saferide.notification.application.event.SchoolSubmittedForApproval;
import com.saferide.notification.application.event.SubscriptionExpiring;
import com.saferide.notification.application.service.SchoolEmailService;
import com.saferide.notification.application.service.SubscriptionEmailService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
public class SchoolEventConsumer {
    private final SchoolEmailService schoolEmailService;
    private final SubscriptionEmailService subscriptionEmailService;
    private static final Logger log = LoggerFactory.getLogger(SchoolEventConsumer.class);

    public SchoolEventConsumer(
            SchoolEmailService schoolEmailService, SubscriptionEmailService subscriptionEmailService) {
        this.schoolEmailService = schoolEmailService;
        this.subscriptionEmailService = subscriptionEmailService;
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

    @RabbitListener(queues = "${saferide.rabbitmq.subscription-expiring-queue}")
    public void onSubscriptionExpiring(SubscriptionExpiring event) {
        subscriptionEmailService.sendExpiring(event);
        log.info(
                "Sent subscription expiry email for school {} ({} days left)", event.schoolId(), event.daysRemaining());
    }
}
