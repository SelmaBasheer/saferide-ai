package com.saferide.notification.application.service;

import com.saferide.notification.application.event.AlertApproved;
import com.saferide.notification.application.port.WhatsAppSender;
import java.util.List;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

@Service
public class AlertWhatsAppService {

    private static final String TEMPLATE = "saferide_alert";

    private final WhatsAppSender whatsAppSender;

    /**
     * A fixed recipient, because the Twilio sandbox will only deliver to numbers
     * that have joined it. In production this becomes the school's own contact
     * number, carried on the event alongside the email address.
     */
    @Value("${saferide.notification.alert-whatsapp-to:}")
    private String alertRecipient;

    public AlertWhatsAppService(WhatsAppSender whatsAppSender) {
        this.whatsAppSender = whatsAppSender;
    }

    public void sendApprovedAlert(AlertApproved e) {
        whatsAppSender.sendTemplate(alertRecipient, TEMPLATE, List.of(e.routeCode(), e.message()));
    }
}
