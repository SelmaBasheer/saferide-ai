package com.saferide.route.projection;

import java.time.Instant;
import java.util.UUID;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;

@Document(collection = "school_status")
public class SchoolStatus {

    @Id
    private UUID schoolId;

    private String status;
    private Instant updatedAt;
    private Instant eventAtUtc; // source event time — stale-replay guard; null = pre-guard row

    protected SchoolStatus() {}

    public SchoolStatus(UUID schoolId, String status, Instant occurredAtUtc) {
        this.schoolId = schoolId;
        this.status = status;
        this.eventAtUtc = occurredAtUtc;
        this.updatedAt = Instant.now();
    }

    public UUID getSchoolId() {
        return schoolId;
    }

    public String getStatus() {
        return status;
    }

    public Instant getEventAtUtc() {
        return eventAtUtc;
    }
}
