package com.saferide.notification.infrastructure.email;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.node.ObjectNode;
import com.saferide.notification.application.port.EmailSender;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;

/**
 * The second adapter behind EmailSender. SendGrid's trial ended, and swapping
 * provider cost one file and one environment variable — no application code
 * changed, because nothing above this layer knows who sends the mail.
 */
@Component
@ConditionalOnProperty(name = "email.provider", havingValue = "brevo")
public class BrevoEmailSender implements EmailSender {

    private static final Logger log = LoggerFactory.getLogger(BrevoEmailSender.class);
    private static final String ENDPOINT = "https://api.brevo.com/v3/smtp/email";

    private final BrevoProperties props;
    private final ObjectMapper mapper = new ObjectMapper();
    private final HttpClient client =
            HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(10)).build();

    public BrevoEmailSender(BrevoProperties props) {
        this.props = props;
    }

    @Override
    public void send(String to, String subject, String htmlBody) {
        // Built with Jackson rather than string concatenation. The body is HTML
        // containing quotes and newlines, and hand-rolled JSON would break on
        // the first template that has an apostrophe in it.
        ObjectNode sender = mapper.createObjectNode();
        sender.put("name", props.fromName());
        sender.put("email", props.fromEmail());

        ObjectNode recipient = mapper.createObjectNode();
        recipient.put("email", to);

        ObjectNode body = mapper.createObjectNode();
        body.set("sender", sender);
        body.putArray("to").add(recipient);
        body.put("subject", subject);
        body.put("htmlContent", htmlBody);

        try {
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(ENDPOINT))
                    .header("api-key", props.apiKey())
                    .header("content-type", "application/json")
                    .header("accept", "application/json")
                    .timeout(Duration.ofSeconds(20))
                    .POST(HttpRequest.BodyPublishers.ofString(mapper.writeValueAsString(body), StandardCharsets.UTF_8))
                    .build();

            HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());

            if (response.statusCode() >= 400) {
                // The status is logged, the body is not — it echoes the
                // recipient address back, and that is a parent's email.
                log.error("Brevo send failed with status {}", response.statusCode());
                throw new RuntimeException("Brevo returned " + response.statusCode());
            }

            log.info("Email sent (status {})", response.statusCode());

        } catch (InterruptedException e) {
            // Restore the flag rather than swallowing it, or the retry
            // interceptor above keeps working on a thread that was told to stop.
            Thread.currentThread().interrupt();
            throw new RuntimeException("Interrupted while sending email", e);
        } catch (RuntimeException e) {
            throw e;
        } catch (Exception e) {
            throw new RuntimeException("Failed to send email", e);
        }
    }
}
