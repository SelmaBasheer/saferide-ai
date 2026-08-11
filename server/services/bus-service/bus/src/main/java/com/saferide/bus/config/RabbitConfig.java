package com.saferide.bus.config;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.QueueBuilder;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {

    private static final String DLX = "saferide.dlx";

    @Value("${saferide.rabbitmq.bus-exchange}")
    String busExchange;

    @Value("${saferide" + ".rabbitmq.school-exchange}")
    String schoolExchange;

    @Value("${saferide.rabbitmq.school-events-queue}")
    String schoolEventsQueue;

    @Bean
    TopicExchange busEventsExchange() {
        return new TopicExchange(busExchange, true, false);
    }

    @Bean
    TopicExchange schoolEventsExchange() {
        return new TopicExchange(schoolExchange, true, false);
    }

    @Bean
    Queue schoolEventsQueueBean() {
        return QueueBuilder.durable(schoolEventsQueue)
                .withArgument("x-dead-letter-exchange", DLX)
                .withArgument("x-dead-letter-routing-key", schoolEventsQueue)
                .build();
    }

    @Bean
    TopicExchange deadLetterExchange() {
        return new TopicExchange(DLX, true, false);
    }

    @Bean
    Queue schoolEventsDlq() {
        return QueueBuilder.durable(schoolEventsQueue + ".dlq").build();
    }

    @Bean
    Binding schoolEventsDlqBinding() {
        return BindingBuilder.bind(schoolEventsDlq()).to(deadLetterExchange()).with(schoolEventsQueue);
    }

    @Bean
    Binding schoolApprovedBinding() {
        return BindingBuilder.bind(schoolEventsQueueBean())
                .to(schoolEventsExchange())
                .with("school-approved");
    }

    @Bean
    Binding schoolSuspendedBinding() {
        return BindingBuilder.bind(schoolEventsQueueBean())
                .to(schoolEventsExchange())
                .with("school-suspended");
    }

    @Bean
    RabbitTemplate rabbitTemplate(ConnectionFactory cf, Jackson2JsonMessageConverter conv) {
        var template = new RabbitTemplate(cf);
        template.setMessageConverter(conv);
        return template;
    }

    @Bean
    Jackson2JsonMessageConverter jsonConverter() {
        ObjectMapper mapper = new ObjectMapper();
        mapper.registerModule(new JavaTimeModule()); // Java time support
        mapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS); // ISO-8601 strings, not numbers
        mapper.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true);
        mapper.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        return new Jackson2JsonMessageConverter(mapper);
    }
}
