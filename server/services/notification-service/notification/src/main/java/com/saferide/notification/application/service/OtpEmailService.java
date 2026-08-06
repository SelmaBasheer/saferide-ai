package com.saferide.notification.application.service;

import com.saferide.notification.application.event.OtpEmailRequested;
import com.saferide.notification.application.port.EmailSender;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.thymeleaf.context.Context;
import org.thymeleaf.spring6.SpringTemplateEngine;

import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;

@Service
public class OtpEmailService {
    private static final String OTP_SUBJECT = "Your SafeRide verification code";
    private static final String INVITATION_SUBJECT = "Welcome to SafeRide — set your password";

    private final EmailSender emailSender;
    private final SpringTemplateEngine templateEngine;
    private final String appBaseUrl;

    public OtpEmailService(EmailSender emailSender, SpringTemplateEngine templateEngine,
                           @Value("${saferide.app.base-url}") String appBaseUrl) {
        this.emailSender = emailSender;
        this.templateEngine = templateEngine;
        this.appBaseUrl = appBaseUrl;
    }

    public void sendOtp(OtpEmailRequested event) {
        if (event.email() == null || event.email().isBlank()) {
            throw new IllegalArgumentException("OTP event has no email address");
        }
        boolean invitation = "Invitation".equalsIgnoreCase(event.purpose());

        String name = (event.firstName() == null || event.firstName().isBlank()) ? "there" : event.firstName();
        Context ctx = new Context();
        ctx.setVariable("name", name);
        ctx.setVariable("code", event.code());
        ctx.setVariable("resetUrl", appBaseUrl + "/reset-password?email="
            + URLEncoder.encode(event.email(), StandardCharsets.UTF_8));

        String template = invitation ? "invitation-email" : "otp-email";
        String subject = invitation ? INVITATION_SUBJECT : OTP_SUBJECT;

        String html = templateEngine.process(template, ctx);
        emailSender.send(event.email(), subject, html);
    }
}
