package com.saferide.bus.projection;

import java.time.Instant;
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
}
