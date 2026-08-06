package com.saferide.student.domain;

import jakarta.persistence.*;
import java.time.Instant;
import java.util.UUID;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Table(
        name = "students",
        uniqueConstraints =
                @UniqueConstraint(
                        name = "uk_students_school_admission",
                        columnNames = {"schoolId", "admissionNumber"}))
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED) // JPA
public class Student {

    @Id
    private UUID id;

    @Column(nullable = false)
    private UUID schoolId; // tenant key — from JWT claim

    @Column(nullable = false, length = 75)
    private String firstName;

    @Column(nullable = false, length = 75)
    private String lastName;

    @Column(nullable = false, length = 30)
    private String admissionNumber;

    @Column(nullable = false, length = 20)
    private String grade;

    // one parent per child (current scope) — flat columns, promoted to an entity when needed
    @Column(nullable = false, length = 75)
    private String parentFirstName;

    @Column(nullable = false, length = 75)
    private String parentLastName;

    @Column(nullable = false, length = 255)
    private String parentEmail; // invitation target

    @Column(nullable = false, length = 20)
    private String parentPhone;

    @Enumerated(EnumType.ORDINAL)
    @Column(nullable = false)
    private StudentStatus status;

    @Column(nullable = false)
    private Instant createdAt;

    private Instant updatedAt;

    public static Student create(
            UUID schoolId,
            String firstName,
            String lastName,
            String admissionNumber,
            String grade,
            String parentFirstName,
            String parentLastName,
            String parentEmail,
            String parentPhone) {
        var s = new Student();
        s.id = UUID.randomUUID();
        s.schoolId = schoolId;
        s.firstName = firstName.trim();
        s.lastName = lastName.trim();
        s.admissionNumber = admissionNumber.trim();
        s.grade = grade.trim();
        s.parentFirstName = parentFirstName.trim();
        s.parentLastName = parentLastName.trim();
        s.parentEmail = parentEmail.trim().toLowerCase();
        s.parentPhone = parentPhone.trim();
        s.status = StudentStatus.ACTIVE;
        s.createdAt = Instant.now();
        return s;
    }
}
