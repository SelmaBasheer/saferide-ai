package com.saferide.notification.infrastructure.whatsapp;

import com.saferide.notification.application.port.WhatsAppSender;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.List;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Component;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.RestClient;

@Component
@ConditionalOnProperty(name = "whatsapp.provider", havingValue = "twilio", matchIfMissing = true)
public class TwilioWhatsAppSender implements WhatsAppSender {

    private static final Logger log = LoggerFactory.getLogger(TwilioWhatsAppSender.class);

    private static final int MAX_BODY_LENGTH = 1500;

    private static final Map<String, String> TEMPLATES = Map.of(
            "saferide_alert", "SafeRide alert on route %s: %s. Please check with the driver.",
            "saferide_expiry", "SafeRide: %s for %s expires in %s days.");

    private final RestClient restClient;

    @Value("${whatsapp.twilio.account-sid}")
    private String accountSid;

    @Value("${whatsapp.twilio.auth-token}")
    private String authToken;

    @Value("${whatsapp.twilio.from}")
    private String from;

    @Value("${whatsapp.enabled:true}")
    private boolean enabled;

    public TwilioWhatsAppSender(RestClient.Builder builder) {
        this.restClient = builder.baseUrl("https://api.twilio.com").build();
    }

    @Override
    public void sendTemplate(String toPhoneNumber, String templateName, List<String> parameters) {
        if (!enabled) {
            log.info("WhatsApp is disabled, skipping {}", templateName);
            return;
        }

        if (toPhoneNumber == null || toPhoneNumber.isBlank()) {
            log.warn("No WhatsApp recipient configured, skipping {}", templateName);
            return;
        }

        MultiValueMap<String, String> form = new LinkedMultiValueMap<>();
        form.add("From", from);
        form.add("To", toPhoneNumber);
        form.add("Body", render(templateName, parameters));

        restClient
                .post()
                .uri("/2010-04-01/Accounts/{sid}/Messages.json", accountSid)
                .header("Authorization", basicAuth())
                .contentType(MediaType.APPLICATION_FORM_URLENCODED)
                .body(form)
                .retrieve()
                .toBodilessEntity();

        // The template name, not the body. The body describes a real trip.
        log.info("Sent WhatsApp message {} via Twilio", templateName);
    }

    private String basicAuth() {
        String pair = accountSid + ":" + authToken;
        return "Basic " + Base64.getEncoder().encodeToString(pair.getBytes(StandardCharsets.UTF_8));
    }

    private static String render(String templateName, List<String> parameters) {
        Object[] values = parameters.stream().map(TwilioWhatsAppSender::clean).toArray();

        String format = TEMPLATES.get(templateName);

        // An unknown template should still deliver something readable rather
        // than throwing away a notification someone is waiting for.
        String body = format == null
                ? String.join(
                        " · ",
                        parameters.stream().map(TwilioWhatsAppSender::clean).toList())
                : String.format(format, values);

        return body.length() <= MAX_BODY_LENGTH ? body : body.substring(0, MAX_BODY_LENGTH - 1) + "…";
    }

    private static String clean(String value) {
        return value == null ? "" : value.replaceAll("\\s+", " ").trim();
    }
}
