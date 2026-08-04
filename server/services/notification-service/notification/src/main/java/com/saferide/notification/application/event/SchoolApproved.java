package com.saferide.notification.application.event;

public record SchoolApproved(
        String schoolId, String adminUserId, String schoolName, String adminEmail, String occurredAtUtc) {}
