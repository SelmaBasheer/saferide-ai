package com.saferide.notification;

import com.saferide.notification.infrastructure.email.BrevoProperties;
import com.saferide.notification.infrastructure.email.SendGridProperties;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.context.properties.EnableConfigurationProperties;

@SpringBootApplication
@EnableConfigurationProperties({SendGridProperties.class, BrevoProperties.class})
public class NotificationApplication {

    public static void main(String[] args) {
        SpringApplication.run(NotificationApplication.class, args);
    }
}
