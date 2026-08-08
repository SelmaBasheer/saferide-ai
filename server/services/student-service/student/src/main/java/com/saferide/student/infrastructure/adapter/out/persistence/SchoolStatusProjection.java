package com.saferide.student.infrastructure.adapter.out.persistence;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;
import java.util.UUID;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(name = "school_status")
public class SchoolStatusProjection {

    @Id
    @JdbcTypeCode(SqlTypes.CHAR)
    private UUID schoolId;

    @Column(nullable = false, length = 20)
    private String status;

    @Column(nullable = false)
    private Instant updatedAt;

    @Column
    private Instant eventAtUtc; // source event time — stale-replay guard; null = pre-guard row

    public Instant getEventAtUtc() {
        return eventAtUtc;
    }

    protected SchoolStatusProjection() {}

    public SchoolStatusProjection(UUID schoolId, String status, Instant occurredAtUtc) {
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

    public String getStatus() {
        return status;
    }
}
