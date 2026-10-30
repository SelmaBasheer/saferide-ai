package com.saferide.route.config;

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

    @Value("${saferide.rabbitmq.route-exchange}")
    String routeExchange;

    @Value("${saferide.rabbitmq.school-exchange}")
    String schoolExchange;

    @Value("${saferide.rabbitmq.school-events-queue}")
    String schoolEventsQueue;

    @Value("${saferide.rabbitmq.bus-exchange}")
    String busExchange;

    @Value("${saferide.rabbitmq.bus-events-queue}")
    String busEventsQueue;

    // Defaults inline so no application.yml change is needed to add this stream.
    @Value("${saferide.rabbitmq.driver-exchange:driver.events}")
    String driverExchange;

    @Value("${saferide.rabbitmq.driver-events-queue:route.driver-events}")
    String driverEventsQueue;

    @Bean
    TopicExchange routeEventsExchange() {
        return new TopicExchange(routeExchange, true, false);
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
    TopicExchange busEventsExchange() {
        return new TopicExchange(busExchange, true, false);
    }

    @Bean
    Queue busEventsQueueBean() {
        return QueueBuilder.durable(busEventsQueue)
                .withArgument("x-dead-letter-exchange", DLX)
                .withArgument("x-dead-letter-routing-key", busEventsQueue)
                .build();
    }

    @Bean
    Queue busEventsDlq() {
        return QueueBuilder.durable(busEventsQueue + ".dlq").build();
    }

    @Bean
    Binding busEventsDlqBinding() {
        return BindingBuilder.bind(busEventsDlq()).to(deadLetterExchange()).with(busEventsQueue);
    }

    @Bean
    Binding busStatusChangedBinding() {
        return BindingBuilder.bind(busEventsQueueBean()).to(busEventsExchange()).with("bus-status-changed");
    }

    // Route-service needs to know which driver is on which bus, so a driver can
    // be shown only the route they are assigned to.
    @Bean
    Binding busDriverAssignedBinding() {
        return BindingBuilder.bind(busEventsQueueBean()).to(busEventsExchange()).with("bus-driver-assigned");
    }

    @Bean
    TopicExchange driverEventsExchange() {
        return new TopicExchange(driverExchange, true, false);
    }

    @Bean
    Queue driverEventsQueueBean() {
        return QueueBuilder.durable(driverEventsQueue)
                .withArgument("x-dead-letter-exchange", DLX)
                .withArgument("x-dead-letter-routing-key", driverEventsQueue)
                .build();
    }

    @Bean
    Queue driverEventsDlq() {
        return QueueBuilder.durable(driverEventsQueue + ".dlq").build();
    }

    @Bean
    Binding driverEventsDlqBinding() {
        return BindingBuilder.bind(driverEventsDlq()).to(deadLetterExchange()).with(driverEventsQueue);
    }

    // Carries the driver's email, the only value shared with the identity
    // token — bus-service knows the driver record id and nothing else.
    @Bean
    Binding driverCreatedBinding() {
        return BindingBuilder.bind(driverEventsQueueBean())
                .to(driverEventsExchange())
                .with("driver-created");
    }

    // A deactivated driver must stop seeing their route immediately, so this
    // service tracks employment status as well as identity.
    @Bean
    Binding driverDeactivatedBinding() {
        return BindingBuilder.bind(driverEventsQueueBean())
                .to(driverEventsExchange())
                .with("driver-deactivated");
    }

    @Bean
    Binding driverReactivatedBinding() {
        return BindingBuilder.bind(driverEventsQueueBean())
                .to(driverEventsExchange())
                .with("driver-reactivated");
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
