package com.saferide.notification.application.event;

public record SchoolRejected(
        String schoolId,
        String adminUserId,
        String schoolName,
        String adminEmail,
        String reason,
        String occurredAtUtc) {}
