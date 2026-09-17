package com.saferide.bus.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Index;
import jakarta.persistence.Table;
import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(
        name = "bus_documents",
        indexes = @Index(name = "ix_bus_document_bus_type", columnList = "bus_id, type, uploaded_at"))
public class BusDocument {

    @Id
    @JdbcTypeCode(SqlTypes.CHAR)
    private UUID id;

    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, updatable = false)
    private UUID busId;

    /** Carried here as well as on the bus, so a document can be authorised without loading one. */
    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, updatable = false)
    private UUID schoolId;

    @Enumerated(EnumType.ORDINAL)
    @Column(nullable = false, updatable = false)
    private BusDocumentType type;

    /** The key in blob storage. Never shown to a user, and never a URL. */
    @Column(nullable = false, updatable = false, length = 300)
    private String blobName;

    /** What the user called it, used for the download filename. */
    @Column(nullable = false, updatable = false, length = 260)
    private String originalFileName;

    @Column(nullable = false, updatable = false, length = 100)
    private String contentType;

    @Column(nullable = false, updatable = false)
    private long sizeBytes;

    /** A date, not an instant: certificates expire on a day, not at a moment. */
    @Column(nullable = false)
    private LocalDate expiresOn;

    @Column(nullable = false, updatable = false)
    private Instant uploadedAt;

    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, updatable = false)
    private UUID uploadedBy;

    protected BusDocument() {}

    private BusDocument(
            UUID busId,
            UUID schoolId,
            BusDocumentType type,
            String blobName,
            String originalFileName,
            String contentType,
            long sizeBytes,
            LocalDate expiresOn,
            UUID uploadedBy) {
        this.id = UUID.randomUUID();
        this.busId = busId;
        this.schoolId = schoolId;
        this.type = type;
        this.blobName = blobName;
        this.originalFileName = originalFileName;
        this.contentType = contentType;
        this.sizeBytes = sizeBytes;
        this.expiresOn = expiresOn;
        this.uploadedAt = Instant.now();
        this.uploadedBy = uploadedBy;
    }

    public static BusDocument upload(
            UUID busId,
            UUID schoolId,
            BusDocumentType type,
            String blobName,
            String originalFileName,
            String contentType,
            long sizeBytes,
            LocalDate expiresOn,
            UUID uploadedBy) {
        return new BusDocument(
                busId, schoolId, type, blobName, originalFileName, contentType, sizeBytes, expiresOn, uploadedBy);
    }

    /**
     * Takes today's date rather than reading the clock, so this can be tested on
     * both sides of an expiry without waiting for tomorrow.
     */
    public boolean isExpired(LocalDate today) {
        return expiresOn.isBefore(today);
    }

    public UUID getId() {
        return id;
    }

    public UUID getBusId() {
        return busId;
    }

    public UUID getSchoolId() {
        return schoolId;
    }

    public BusDocumentType getType() {
        return type;
    }

    public String getBlobName() {
        return blobName;
    }

    public String getOriginalFileName() {
        return originalFileName;
    }

    public String getContentType() {
        return contentType;
    }

    public long getSizeBytes() {
        return sizeBytes;
    }

    public LocalDate getExpiresOn() {
        return expiresOn;
    }

    public Instant getUploadedAt() {
        return uploadedAt;
    }

    public UUID getUploadedBy() {
        return uploadedBy;
    }
}
