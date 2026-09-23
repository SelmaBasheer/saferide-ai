package com.saferide.notification.application.service;

import com.saferide.notification.application.event.AlertApproved;
import com.saferide.notification.application.port.EmailSender;
import org.springframework.stereotype.Service;
import org.thymeleaf.context.Context;
import org.thymeleaf.spring6.SpringTemplateEngine;

@Service
public class AlertEmailService {
    private final EmailSender emailSender;
    private final SpringTemplateEngine templateEngine;

    public AlertEmailService(EmailSender emailSender, SpringTemplateEngine templateEngine) {
        this.emailSender = emailSender;
        this.templateEngine = templateEngine;
    }

    public void sendApprovedAlert(AlertApproved e) {
        Context ctx = new Context();
        ctx.setVariable("routeCode", e.routeCode());
        ctx.setVariable("routeName", e.routeName());
        ctx.setVariable("anomalyType", readable(e.anomalyType()));
        ctx.setVariable("message", e.message());

        emailSender.send(
                e.recipientEmail(),
                readable(e.anomalyType()) + " on route " + e.routeCode(),
                templateEngine.process("alert-approved", ctx));
    }

    /** RouteDeviation reads badly in an email. Route deviation reads fine. */
    private static String readable(String enumName) {
        if (enumName == null || enumName.isBlank()) {
            return "Alert";
        }
        String spaced = enumName.replaceAll("(?<!^)([A-Z])", " $1");
        return spaced.substring(0, 1).toUpperCase() + spaced.substring(1).toLowerCase();
    }
}
