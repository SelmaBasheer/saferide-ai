package com.saferide.notification.infrastructure.email;

import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "brevo")
public record BrevoProperties(String apiKey, String fromEmail, String fromName) {}
