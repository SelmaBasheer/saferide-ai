package com.saferide.notification.application.event;

public record SchoolSubmittedForApproval(
        String schoolId, String schoolName, String adminEmail, String submittedAtUtc) {}
