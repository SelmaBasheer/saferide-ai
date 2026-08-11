package com.saferide.bus.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import jakarta.persistence.UniqueConstraint;
import java.time.Instant;
import java.util.UUID;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(
        name = "buses",
        uniqueConstraints =
                @UniqueConstraint(
                        name = "uk_bus_school_registration",
                        columnNames = {"school_id", "registration_number"}))
public class Bus {

    @Id
    @JdbcTypeCode(SqlTypes.CHAR)
    private UUID id;

    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, updatable = false)
    private UUID schoolId;

    @Column(nullable = false, length = 32)
    private String registrationNumber;

    @Column(nullable = false, length = 100)
    private String model;

    @Column(nullable = false)
    private int capacity;

    @Enumerated(EnumType.ORDINAL)
    @Column(nullable = false)
    private BusStatus status;

    @JdbcTypeCode(SqlTypes.CHAR)
    @Column
    private UUID assignedDriverId;

    @Column(nullable = false, updatable = false)
    private Instant createdAt;

    @Column(nullable = false)
    private Instant updatedAt;

    protected Bus() {}

    private Bus(UUID schoolId, String registrationNumber, String model, int capacity) {
        this.id = UUID.randomUUID();
        this.schoolId = schoolId;
        this.registrationNumber = normalizeRegistrationNumber(registrationNumber);
        this.model = model.trim();
        this.capacity = capacity;
        this.status = BusStatus.ACTIVE;
        this.createdAt = Instant.now();
        this.updatedAt = this.createdAt;
    }

    public static Bus create(UUID schoolId, String registrationNumber, String model, int capacity) {
        return new Bus(schoolId, registrationNumber, model, capacity);
    }

    public void update(String registrationNumber, String model, int capacity) {
        this.registrationNumber = normalizeRegistrationNumber(registrationNumber);
        this.model = model.trim();
        this.capacity = capacity;
        this.updatedAt = Instant.now();
    }

    public void assignDriver(UUID driverId) {
        this.assignedDriverId = driverId;
        this.updatedAt = Instant.now();
    }

    public void deactivate() {
        this.status = BusStatus.INACTIVE;
        this.updatedAt = Instant.now();
    }

    public boolean isActive() {
        return status == BusStatus.ACTIVE;
    }

    public static String normalizeRegistrationNumber(String registrationNumber) {
        return registrationNumber.trim().toUpperCase();
    }

    public UUID getId() {
        return id;
    }

    public UUID getSchoolId() {
        return schoolId;
    }

    public String getRegistrationNumber() {
        return registrationNumber;
    }

    public String getModel() {
        return model;
    }

    public int getCapacity() {
        return capacity;
    }

    public BusStatus getStatus() {
        return status;
    }

    public UUID getAssignedDriverId() {
        return assignedDriverId;
    }

    public Instant getCreatedAt() {
        return createdAt;
    }

    public Instant getUpdatedAt() {
        return updatedAt;
    }
}
