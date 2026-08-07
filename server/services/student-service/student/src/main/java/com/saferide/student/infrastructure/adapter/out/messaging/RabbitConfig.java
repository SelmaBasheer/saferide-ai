package com.saferide.student.infrastructure.adapter.out.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;

import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {

    @Value("${saferide.rabbitmq.student-exchange}")
    String studentExchange;

    @Bean
    TopicExchange studentEventsExchange() {
        return new TopicExchange(studentExchange, true, false);
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
        mapper.registerModule(new JavaTimeModule());                      // Java time support
        mapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);   // ISO-8601 strings, not numbers
        return new Jackson2JsonMessageConverter(mapper);
    }
}
