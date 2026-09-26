package com.saferide.notification.application.service;

import com.saferide.notification.application.event.SubscriptionExpiring;
import com.saferide.notification.application.port.EmailSender;
import java.time.format.DateTimeFormatter;
import java.util.Locale;
import org.springframework.stereotype.Service;
import org.thymeleaf.context.Context;
import org.thymeleaf.spring6.SpringTemplateEngine;

@Service
public class SubscriptionEmailService {

    private static final DateTimeFormatter DATE = DateTimeFormatter.ofPattern("d MMMM yyyy", Locale.ENGLISH);

    private final EmailSender emailSender;
    private final SpringTemplateEngine templateEngine;

    public SubscriptionEmailService(EmailSender emailSender, SpringTemplateEngine templateEngine) {
        this.emailSender = emailSender;
        this.templateEngine = templateEngine;
    }

    public void sendExpiring(SubscriptionExpiring e) {
        Context ctx = new Context();
        ctx.setVariable("schoolName", e.schoolName());
        ctx.setVariable("planName", e.planName());
        ctx.setVariable("endsOn", e.endsOn().format(DATE));
        ctx.setVariable("daysRemaining", e.daysRemaining());

        // The subject carries the number, so three reminders read as three
        // different messages rather than one sent three times.
        String subject = e.daysRemaining() == 1
                ? "Your SafeRide subscription ends tomorrow"
                : "Your SafeRide subscription ends in " + e.daysRemaining() + " days";

        emailSender.send(e.adminEmail(), subject, templateEngine.process("subscription-expiring", ctx));
    }
}
