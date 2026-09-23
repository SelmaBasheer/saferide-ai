package com.saferide.notification.application.port;

import java.util.List;

public interface WhatsAppSender {
    void sendTemplate(String toPhoneNumber, String templateName, List<String> parameters);
}
