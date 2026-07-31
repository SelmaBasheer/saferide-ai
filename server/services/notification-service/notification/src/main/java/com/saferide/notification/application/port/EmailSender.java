package com.saferide.notification.application.port;

public interface EmailSender {
    void send(String to, String subject, String htmlBody);
}
