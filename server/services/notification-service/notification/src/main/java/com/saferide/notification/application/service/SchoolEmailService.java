package com.saferide.notification.application.service;

import com.saferide.notification.application.event.SchoolApproved;
import com.saferide.notification.application.event.SchoolRejected;
import com.saferide.notification.application.event.SchoolSubmittedForApproval;
import com.saferide.notification.application.port.EmailSender;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.thymeleaf.context.Context;
import org.thymeleaf.spring6.SpringTemplateEngine;

@Service
public class SchoolEmailService {
    private final EmailSender emailSender;
    private final SpringTemplateEngine templateEngine;

    @Value("${saferide.notification.superadmin-email}")
    private String superAdminEmail;

    public SchoolEmailService(EmailSender emailSender, SpringTemplateEngine templateEngine) {
        this.emailSender = emailSender;
        this.templateEngine = templateEngine;
    }

    public void sendSubmitted(SchoolSubmittedForApproval e) {
        Context ctx = new Context();
        ctx.setVariable("schoolName", e.schoolName());
        ctx.setVariable("adminEmail", e.adminEmail());
        emailSender.send(
                superAdminEmail,
                "New school submitted for review: " + e.schoolName(),
                templateEngine.process("school-submitted", ctx));
    }

    public void sendApproved(SchoolApproved e) {
        Context ctx = new Context();
        ctx.setVariable("schoolName", e.schoolName());
        emailSender.send(
                e.adminEmail(), "Your school has been approved 🎉", templateEngine.process("school-approved", ctx));
    }

    public void sendRejected(SchoolRejected e) {
        Context ctx = new Context();
        ctx.setVariable("schoolName", e.schoolName());
        ctx.setVariable("reason", e.reason());
        emailSender.send(
                e.adminEmail(), "Your school submission needs changes", templateEngine.process("school-rejected", ctx));
    }
}
