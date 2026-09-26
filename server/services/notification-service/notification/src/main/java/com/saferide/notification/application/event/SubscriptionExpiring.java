package com.saferide.notification.application.event;

import java.time.LocalDate;

public record SubscriptionExpiring(
        String schoolId,
        String schoolName,
        String adminEmail,
        String planName,
        LocalDate endsOn,
        int daysRemaining,
        String occurredAtUtc) {}
