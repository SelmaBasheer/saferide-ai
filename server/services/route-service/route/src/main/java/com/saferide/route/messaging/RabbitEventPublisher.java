package com.saferide.route.messaging;

import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
public class RabbitEventPublisher {

    private final RabbitTemplate template;
    private final String exchange;

    public RabbitEventPublisher(
            RabbitTemplate template, @Value("${saferide.rabbitmq.route-exchange}") String exchange) {
        this.template = template;
        this.exchange = exchange;
    }

    public void publish(String routingKey, Object event) {

        template.convertAndSend(exchange, routingKey, event);
    }
}
