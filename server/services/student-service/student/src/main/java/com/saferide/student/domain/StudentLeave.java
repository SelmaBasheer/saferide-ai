package com.saferide.student.domain;

import jakarta.persistence.*;
import java.time.Instant;
import java.time.LocalDate;
import java.util.UUID;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

@Entity
@Table(
        name = "student_leaves",
        uniqueConstraints =
                @UniqueConstraint(
                        name = "uk_student_leave_date",
                        columnNames = {"studentId", "onDate"}))
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED) // JPA
public class StudentLeave {

    @Id
    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(length = 36)
    private UUID id;

    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, length = 36)
    private UUID studentId;

    /** Carried here as well as on the student, so a leave can be scoped without loading one. */
    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, length = 36)
    private UUID schoolId;

    @Column(nullable = false)
    private LocalDate onDate;

    @Column(length = 200)
    private String reason;

    /** The parent who marked it, from their token. */
    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(nullable = false, length = 36)
    private UUID markedBy;

    @Column(nullable = false)
    private Instant createdAt;

    public static StudentLeave mark(UUID studentId, UUID schoolId, LocalDate onDate, String reason, UUID markedBy) {
        var leave = new StudentLeave();
        leave.id = UUID.randomUUID();
        leave.studentId = studentId;
        leave.schoolId = schoolId;
        leave.onDate = onDate;
        leave.reason = reason == null || reason.isBlank() ? null : reason.trim();
        leave.markedBy = markedBy;
        leave.createdAt = Instant.now();
        return leave;
    }
}
