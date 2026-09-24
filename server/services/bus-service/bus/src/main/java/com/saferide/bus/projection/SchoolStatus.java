package com.saferide.bus.projection;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(name = "school_status")
public class SchoolStatus {

    @Id
    @JdbcTypeCode(SqlTypes.CHAR)
    private UUID schoolId;

    @Column(nullable = false, length = 20)
    private String status;

    @Column(nullable = false)
    private Instant updatedAt;

    @Column
    private Instant eventAtUtc; // source event time — stale-replay guard; null = pre-guard row

    // ----- entitlement, from school-subscription-changed -----

    /** Null means unlimited, or that no subscription event has arrived yet. */
    @Column
    private Integer busLimit;

    @Column(length = 20)
    private String subscriptionStatus;

    @Column
    private LocalDate subscriptionEndsOn;

    @Column
    private Instant entitlementEventAtUtc;

    protected SchoolStatus() {}

    public SchoolStatus(UUID schoolId, String status, Instant occurredAtUtc) {
        this.schoolId = schoolId;
        this.status = status;
        this.eventAtUtc = occurredAtUtc;
        this.updatedAt = Instant.now();
    }

    public void update(String status, Instant occurredAtUtc) {
        this.status = status;
        this.eventAtUtc = occurredAtUtc;
        this.updatedAt = Instant.now();
    }

    public void updateEntitlement(
            Integer busLimit, String subscriptionStatus, LocalDate endsOn, Instant occurredAtUtc) {
        this.busLimit = busLimit;
        this.subscriptionStatus = subscriptionStatus;
        this.subscriptionEndsOn = endsOn;
        this.entitlementEventAtUtc = occurredAtUtc;
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

    public Integer getBusLimit() {
        return busLimit;
    }

    public String getSubscriptionStatus() {
        return subscriptionStatus;
    }

    public LocalDate getSubscriptionEndsOn() {
        return subscriptionEndsOn;
    }

    public Instant getEntitlementEventAtUtc() {
        return entitlementEventAtUtc;
    }
}
