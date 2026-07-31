package com.saferide.notification.infrastructure.messaging;

import com.saferide.notification.application.event.OtpEmailRequested;
import com.saferide.notification.application.service.OtpEmailService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
public class OtpEventConsumer {
    private final OtpEmailService otpEmailService;
    private static final Logger log = LoggerFactory.getLogger(OtpEventConsumer.class);
    public OtpEventConsumer(OtpEmailService otpEmailService) { this.otpEmailService = otpEmailService; }

    @RabbitListener(queues = "${saferide.rabbitmq.otp-queue}")
    public void handle(OtpEmailRequested event) {
        otpEmailService.sendOtp(event);
        log.info("Received OTP event for {} -> code {}", event.email(), event.code());
    }
}
