package com.saferide.notification.infrastructure.messaging;

import com.saferide.notification.application.event.AlertApproved;
import com.saferide.notification.application.service.AlertEmailService;
import com.saferide.notification.application.service.AlertWhatsAppService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
public class AlertEventConsumer {
    private static final Logger log = LoggerFactory.getLogger(AlertEventConsumer.class);

    private final AlertEmailService alertEmailService;
    private final AlertWhatsAppService alertWhatsAppService;

    public AlertEventConsumer(AlertEmailService alertEmailService, AlertWhatsAppService alertWhatsAppService) {
        this.alertEmailService = alertEmailService;
        this.alertWhatsAppService = alertWhatsAppService;
    }

    @RabbitListener(queues = "${saferide.rabbitmq.alert-approved-queue}")
    public void onApproved(AlertApproved event) {
        // Email first, and its failures are allowed to propagate: the listener
        // retries, and email is the channel of record.
        alertEmailService.sendApprovedAlert(event);

        // WhatsApp failures are swallowed on purpose. If they propagated, the
        // retry would send the email three more times to fix a problem the
        // email never had.
        try {
            alertWhatsAppService.sendApprovedAlert(event);
        } catch (Exception ex) {
            log.error("WhatsApp send failed for alert {}, the email still went", event.alertId(), ex);
        }

        log.info("Notified approved alert {}", event.alertId());
    }
}
