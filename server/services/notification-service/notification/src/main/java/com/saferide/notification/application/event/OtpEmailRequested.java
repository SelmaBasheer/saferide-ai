package com.saferide.notification.application.event;

public record OtpEmailRequested(
    String userId,
    String email,
    String firstName,
    String code,
    String purpose,
    String occurredAtUtc
) {}
