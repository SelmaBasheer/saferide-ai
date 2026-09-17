package com.saferide.route.projection;

import java.time.Instant;
import java.util.UUID;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;

@Document(collection = "bus_status")
public class BusStatus {

    @Id
    private UUID busId;

    private UUID schoolId;
    private String registrationNumber;
    private boolean active;
    private boolean documentsValid;
    private Instant updatedAt;
    private Instant eventAtUtc; // source event time — stale-replay guard; null = pre-guard row

    protected BusStatus() {}

    public BusStatus(
            UUID busId,
            UUID schoolId,
            String registrationNumber,
            boolean active,
            boolean documentsValid,
            Instant occurredAtUtc) {
        this.busId = busId;
        this.schoolId = schoolId;
        this.registrationNumber = registrationNumber;
        this.active = active;
        this.documentsValid = documentsValid;
        this.eventAtUtc = occurredAtUtc;
        this.updatedAt = Instant.now();
    }

    public UUID getBusId() {
        return busId;
    }

    public UUID getSchoolId() {
        return schoolId;
    }

    public String getRegistrationNumber() {
        return registrationNumber;
    }

    public boolean isActive() {
        return active;
    }

    public boolean isDocumentsValid() {
        return documentsValid;
    }

    public Instant getEventAtUtc() {
        return eventAtUtc;
    }
}
