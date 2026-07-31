package com.saferide.notification.application.service;

import com.saferide.notification.application.event.OtpEmailRequested;
import com.saferide.notification.application.port.EmailSender;
import org.springframework.stereotype.Service;
import org.thymeleaf.context.Context;
import org.thymeleaf.spring6.SpringTemplateEngine;

@Service
public class OtpEmailService {
    private static final String SUBJECT = "Your SafeRide verification code";
    private final EmailSender emailSender;
    private final SpringTemplateEngine templateEngine;

    public OtpEmailService(EmailSender emailSender, SpringTemplateEngine templateEngine) {
        this.emailSender = emailSender;
        this.templateEngine = templateEngine;
    }

    public void sendOtp(OtpEmailRequested event) {
        if (event.email() == null || event.email().isBlank()) {
            throw new IllegalArgumentException("OTP event has no email address");
        }
        String name = (event.firstName() == null || event.firstName().isBlank()) ? "there" : event.firstName();
        Context ctx = new Context();
        ctx.setVariable("name", name);
        ctx.setVariable("code", event.code());
        String html = templateEngine.process("otp-email", ctx); // Thymeleaf → HTML
        emailSender.send(event.email(), SUBJECT, html); // SendGrid → inbox
    }
}
