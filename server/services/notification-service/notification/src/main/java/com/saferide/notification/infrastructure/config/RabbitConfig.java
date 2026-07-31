package com.saferide.notification.infrastructure.config;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {

    @Value("${saferide.rabbitmq.identity-exchange}")
    String exchange;

    @Value("${saferide.rabbitmq.otp-queue}")
    String queue;

    @Value("${saferide.rabbitmq.otp-routing-key}")
    String routingKey;

    @Bean
    TopicExchange identityExchange() {
        return new TopicExchange(exchange, true, false);
    }

    @Bean
    Queue otpQueue() {
        return new Queue(queue, true);
    }

    @Bean
    Binding otpBinding() {
        return BindingBuilder.bind(otpQueue()).to(identityExchange()).with(routingKey);
    }

    @Bean
    Jackson2JsonMessageConverter jsonConverter() {
        ObjectMapper m = new ObjectMapper();
        m.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true);
        m.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        return new Jackson2JsonMessageConverter(m);
    }
}
