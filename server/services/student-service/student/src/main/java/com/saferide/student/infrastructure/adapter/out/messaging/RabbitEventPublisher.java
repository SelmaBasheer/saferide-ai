package com.saferide.student.infrastructure.messaging;

import com.saferide.student.application.port.EventPublisherPort;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class RabbitEventPublisher implements EventPublisherPort {

    private final RabbitTemplate template;
    private final String exchange;

    public RabbitEventPublisher(
            RabbitTemplate template, @Value("${saferide.rabbitmq.student-exchange}") String exchange) {
        this.template = template;
        this.exchange = exchange;
    }

    @Override
    public void publish(String routingKey, Object event) {
        template.convertAndSend(exchange, routingKey, event);
    }
}
