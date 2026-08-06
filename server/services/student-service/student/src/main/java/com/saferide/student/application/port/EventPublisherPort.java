package com.saferide.student.application.port;

public interface EventPublisherPort {
    void publish(String routingKey, Object event);
}
