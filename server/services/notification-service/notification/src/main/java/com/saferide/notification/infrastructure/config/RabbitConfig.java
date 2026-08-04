package com.saferide.notification.infrastructure.config;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.QueueBuilder;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {

    private static final String DLX = "saferide.dlx";

    @Value("${saferide.rabbitmq.identity-exchange}")
    String identityExchange;

    @Value("${saferide.rabbitmq.otp-queue}")
    String otpQueue;

    @Value("${saferide.rabbitmq.otp-routing-key}")
    String otpRoutingKey;

    @Value("${saferide.rabbitmq.school-exchange}")
    String schoolExchange;

    @Value("${saferide.rabbitmq.school-submitted-queue}")
    String submittedQueue;

    @Value("${saferide.rabbitmq.school-approved-queue}")
    String approvedQueue;

    @Value("${saferide.rabbitmq.school-rejected-queue}")
    String rejectedQueue;

    // ---------- exchanges ----------
    @Bean
    TopicExchange identityExchange() {
        return new TopicExchange(identityExchange, true, false);
    }

    @Bean
    TopicExchange schoolEventsExchange() {
        return new TopicExchange(schoolExchange, true, false);
    }

    @Bean
    TopicExchange deadLetterExchange() {
        return new TopicExchange(DLX, true, false);
    }

    // ---------- helper ----------
    private Queue queueWithDlq(String name) {
        return QueueBuilder.durable(name)
                .withArgument("x-dead-letter-exchange", DLX)
                .withArgument("x-dead-letter-routing-key", name)
                .build();
    }

    // ---------- main queues ----------
    @Bean
    Queue otpQueue() {
        return queueWithDlq(otpQueue);
    }

    @Bean
    Queue schoolSubmittedQueue() {
        return queueWithDlq(submittedQueue);
    }

    @Bean
    Queue schoolApprovedQueue() {
        return queueWithDlq(approvedQueue);
    }

    @Bean
    Queue schoolRejectedQueue() {
        return queueWithDlq(rejectedQueue);
    }

    // ---------- dead-letter queues ----------
    @Bean
    Queue otpDlq() {
        return QueueBuilder.durable(otpQueue + ".dlq").build();
    }

    @Bean
    Queue schoolSubmittedDlq() {
        return QueueBuilder.durable(submittedQueue + ".dlq").build();
    }

    @Bean
    Queue schoolApprovedDlq() {
        return QueueBuilder.durable(approvedQueue + ".dlq").build();
    }

    @Bean
    Queue schoolRejectedDlq() {
        return QueueBuilder.durable(rejectedQueue + ".dlq").build();
    }

    // ---------- bindings ----------
    @Bean
    Binding otpBinding() {
        return BindingBuilder.bind(otpQueue()).to(identityExchange()).with(otpRoutingKey);
    }

    @Bean
    Binding submittedBinding() {
        return BindingBuilder.bind(schoolSubmittedQueue())
                .to(schoolEventsExchange())
                .with("school-submitted-for-approval");
    }

    @Bean
    Binding approvedBinding() {
        return BindingBuilder.bind(schoolApprovedQueue())
                .to(schoolEventsExchange())
                .with("school-approved");
    }

    @Bean
    Binding rejectedBinding() {
        return BindingBuilder.bind(schoolRejectedQueue())
                .to(schoolEventsExchange())
                .with("school-rejected");
    }

    // ---------- DLQ bindings ----------
    @Bean
    Binding otpDlqBinding() {
        return BindingBuilder.bind(otpDlq()).to(deadLetterExchange()).with(otpQueue);
    }

    @Bean
    Binding submittedDlqBinding() {
        return BindingBuilder.bind(schoolSubmittedDlq())
                .to(deadLetterExchange())
                .with(submittedQueue);
    }

    @Bean
    Binding approvedDlqBinding() {
        return BindingBuilder.bind(schoolApprovedDlq()).to(deadLetterExchange()).with(approvedQueue);
    }

    @Bean
    Binding rejectedDlqBinding() {
        return BindingBuilder.bind(schoolRejectedDlq()).to(deadLetterExchange()).with(rejectedQueue);
    }

    @Bean
    Jackson2JsonMessageConverter jsonConverter() {
        ObjectMapper m = new ObjectMapper();
        m.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true);
        m.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        return new Jackson2JsonMessageConverter(m);
    }
}
