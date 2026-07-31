package com.saferide.notification.infrastructure.email;

import com.saferide.notification.application.port.EmailSender;
import com.sendgrid.*;
import com.sendgrid.helpers.mail.Mail;
import com.sendgrid.helpers.mail.objects.Content;
import com.sendgrid.helpers.mail.objects.Email;
import java.io.IOException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;

@Component
public class SendGridEmailSender implements EmailSender {
    private static final Logger log = LoggerFactory.getLogger(SendGridEmailSender.class);
    private final SendGridProperties props;

    public SendGridEmailSender(SendGridProperties props) {
        this.props = props;
    }

    @Override
    public void send(String to, String subject, String htmlBody) {
        Mail mail = new Mail(
                new Email(props.fromEmail(), props.fromName()),
                subject,
                new Email(to),
                new Content("text/html", htmlBody));
        Request request = new Request();
        try {
            request.setMethod(Method.POST);
            request.setEndpoint("mail/send");
            request.setBody(mail.build());
            Response res = new SendGrid(props.apiKey()).api(request);
            if (res.getStatusCode() >= 400) {
                log.error("SendGrid send failed with status {}", res.getStatusCode());
                throw new RuntimeException("SendGrid returned " + res.getStatusCode());
            }
            log.info("Email sent (status {})", res.getStatusCode());
        } catch (IOException e) {
            throw new RuntimeException("Failed to send email", e);
        }
    }
}
