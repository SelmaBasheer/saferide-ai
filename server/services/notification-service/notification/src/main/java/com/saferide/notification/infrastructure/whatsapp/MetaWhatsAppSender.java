package com.saferide.notification.infrastructure.whatsapp;

import com.saferide.notification.application.port.WhatsAppSender;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

@Component
@ConditionalOnProperty(name = "whatsapp.provider", havingValue = "meta")
public class MetaWhatsAppSender implements WhatsAppSender {

    private static final Logger log = LoggerFactory.getLogger(MetaWhatsAppSender.class);

    private static final int MAX_PARAMETER_LENGTH = 700;

    private final RestClient restClient;

    @Value("${whatsapp.meta.api-version}")
    private String apiVersion;

    @Value("${whatsapp.meta.phone-number-id}")
    private String phoneNumberId;

    @Value("${whatsapp.meta.access-token}")
    private String accessToken;

    @Value("${whatsapp.enabled:true}")
    private boolean enabled;

    public MetaWhatsAppSender(RestClient.Builder builder) {
        this.restClient = builder.baseUrl("https://graph.facebook.com").build();
    }

    @Override
    public void sendTemplate(String toPhoneNumber, String templateName, List<String> parameters) {
        if (!enabled) {
            log.info("WhatsApp is disabled, skipping template {}", templateName);
            return;
        }

        if (toPhoneNumber == null || toPhoneNumber.isBlank()) {
            log.warn("No WhatsApp recipient configured, skipping template {}", templateName);
            return;
        }

        List<Map<String, String>> body = parameters.stream()
                .map(p -> Map.of("type", "text", "text", clean(p)))
                .toList();

        Map<String, Object> payload = Map.of(
                "messaging_product",
                "whatsapp",
                "to",
                toPhoneNumber,
                "type",
                "template",
                "template",
                Map.of(
                        "name", templateName,
                        "language", Map.of("code", "en"),
                        "components", List.of(Map.of("type", "body", "parameters", body))));

        restClient
                .post()
                .uri("/{version}/{phoneNumberId}/messages", apiVersion, phoneNumberId)
                .header("Authorization", "Bearer " + accessToken)
                .contentType(MediaType.APPLICATION_JSON)
                .body(payload)
                .retrieve()
                .toBodilessEntity();

        log.info("Sent WhatsApp template {} via Meta", templateName);
    }

    private static String clean(String value) {
        if (value == null) {
            return "";
        }

        String collapsed = value.replaceAll("\\s+", " ").trim();

        return collapsed.length() <= MAX_PARAMETER_LENGTH
                ? collapsed
                : collapsed.substring(0, MAX_PARAMETER_LENGTH - 1) + "…";
    }
}
