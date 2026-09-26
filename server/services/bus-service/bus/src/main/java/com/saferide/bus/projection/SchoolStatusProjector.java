package com.saferide.bus.projection;

import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Component
public class SchoolStatusProjector {

    private static final Logger log = LoggerFactory.getLogger(SchoolStatusProjector.class);

    private final SchoolStatusRepository repository;

    public SchoolStatusProjector(SchoolStatusRepository repository) {
        this.repository = repository;
    }

    /** Applies the event only if it is newer than the stored row (out-of-order delivery guard). */
    @Transactional
    public void upsert(UUID schoolId, String status, Instant occurredAtUtc) {
        repository
                .findById(schoolId)
                .ifPresentOrElse(
                        existing -> {
                            Instant stored = existing.getEventAtUtc();
                            if (stored == null || occurredAtUtc.isAfter(stored)) {
                                existing.update(status, occurredAtUtc);
                            } else {
                                log.debug(
                                        "Ignoring stale school event for {}: event={} stored={}",
                                        schoolId,
                                        occurredAtUtc,
                                        stored);
                            }
                        },
                        () -> repository.save(new SchoolStatus(schoolId, status, occurredAtUtc)));
    }

    /**
     * Same guard, its own timestamp. A subscription change and a status change
     * are separate decisions, so comparing one against the other would drop
     * events that are perfectly fresh.
     */
    @Transactional
    public void upsertEntitlement(
            UUID schoolId, Integer busLimit, String subscriptionStatus, LocalDate endsOn, Instant occurredAtUtc) {
        repository
                .findById(schoolId)
                .ifPresentOrElse(
                        existing -> {
                            Instant stored = existing.getEntitlementEventAtUtc();
                            if (stored == null || occurredAtUtc.isAfter(stored)) {
                                existing.updateEntitlement(busLimit, subscriptionStatus, endsOn, occurredAtUtc);
                            } else {
                                log.debug(
                                        "Ignoring stale subscription event for {}: event={} stored={}",
                                        schoolId,
                                        occurredAtUtc,
                                        stored);
                            }
                        },
                        () -> {
                            // The subscription event beat the status event here.
                            // The row is created as Unknown rather than guessing
                            // Approved, so every permission check fails closed.
                            SchoolStatus created = new SchoolStatus(schoolId, SchoolStatuses.UNKNOWN, null);
                            created.updateEntitlement(busLimit, subscriptionStatus, endsOn, occurredAtUtc);
                            repository.save(created);
                        });
    }
}
