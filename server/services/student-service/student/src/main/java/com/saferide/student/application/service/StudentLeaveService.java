package com.saferide.student.application.service;

import com.saferide.student.application.exception.AppException;
import com.saferide.student.application.port.StudentLeavePort;
import com.saferide.student.application.port.StudentRepositoryPort;
import com.saferide.student.domain.Student;
import com.saferide.student.domain.StudentLeave;
import java.time.LocalDate;
import java.time.ZoneId;
import java.util.Collection;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class StudentLeaveService {

    public static final String NOT_YOUR_CHILD = "That student is not linked to your account.";
    public static final String DATE_IN_PAST = "Leave can only be marked for today or a future date.";
    public static final String MAX_DAYS_AHEAD = "Leave can be marked up to 90 days ahead.";

    private static final int MAX_AHEAD = 90;

    public record ChildWithLeaves(Student student, List<LocalDate> upcomingLeaves) {}

    private final StudentRepositoryPort students;
    private final StudentLeavePort leaves;

    /**
     * The school's timezone, not the server's. Containers run in UTC, and a
     * 7am trip in India is 1:30am UTC on the same date — which works by luck
     * and stops working for anything before 5:30am local.
     */
    @Value("${saferide.timezone:Asia/Kolkata}")
    private String timezone;

    public StudentLeaveService(StudentRepositoryPort students, StudentLeavePort leaves) {
        this.students = students;
        this.leaves = leaves;
    }

    public LocalDate today() {
        return LocalDate.now(ZoneId.of(timezone));
    }

    /** A parent has one or two children, so a query each is cheaper than a join. */
    @Transactional(readOnly = true)
    public List<ChildWithLeaves> myChildren(String parentEmail) {
        var today = today();

        return students.findByParentEmail(normalise(parentEmail)).stream()
                .map(s -> new ChildWithLeaves(
                        s,
                        leaves.findFrom(s.getId(), today).stream()
                                .map(StudentLeave::getOnDate)
                                .toList()))
                .toList();
    }

    @Transactional
    public void mark(String parentEmail, UUID studentId, LocalDate onDate, String reason, UUID markedBy) {
        var student = requireOwnership(parentEmail, studentId);

        var today = today();

        if (onDate.isBefore(today)) {
            throw new AppException.ValidationException(DATE_IN_PAST);
        }

        if (onDate.isAfter(today.plusDays(MAX_AHEAD))) {
            throw new AppException.ValidationException(MAX_DAYS_AHEAD);
        }

        // Marking the same day twice is not an error — the parent meant it the
        // first time and nothing changes.
        if (leaves.exists(studentId, onDate)) {
            return;
        }

        leaves.save(StudentLeave.mark(studentId, student.getSchoolId(), onDate, reason, markedBy));
    }

    @Transactional
    public void cancel(String parentEmail, UUID studentId, LocalDate onDate) {
        requireOwnership(parentEmail, studentId);
        leaves.delete(studentId, onDate);
    }

    /** Used when building a trip roster — one query for the whole route. */
    @Transactional(readOnly = true)
    public Set<UUID> idsOnLeaveToday(Collection<UUID> studentIds) {
        return leaves.idsOnLeave(studentIds, today());
    }

    /**
     * The only authorisation a parent endpoint needs. A parent names a student
     * id; this refuses unless that student's record carries their email. Same
     * principle as schoolId coming from the claim — the caller never gets to
     * assert whose child they are talking about.
     */
    private Student requireOwnership(String parentEmail, UUID studentId) {
        return students.findByParentEmail(normalise(parentEmail)).stream()
                .filter(s -> s.getId().equals(studentId))
                .findFirst()
                .orElseThrow(() -> new AppException.ForbiddenException(NOT_YOUR_CHILD));
    }

    private static String normalise(String email) {
        return email == null ? "" : email.trim().toLowerCase();
    }
}
