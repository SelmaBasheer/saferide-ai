package com.saferide.notification.application.event;

/**
 * Published by the AI service when a school admin approves an alert.
 *
 * It carries the recipient's address rather than just an id, for the same
 * reason SchoolApproved does: this service holds no database, so it cannot
 * look anyone up. Whoever publishes an event is responsible for saying who
 * it is for.
 */
public record AlertApproved(
        String eventId,
        String schoolId,
        String alertId,
        String routeCode,
        String routeName,
        String anomalyType,
        String message,
        String recipientEmail,
        String occurredAtUtc) {}
