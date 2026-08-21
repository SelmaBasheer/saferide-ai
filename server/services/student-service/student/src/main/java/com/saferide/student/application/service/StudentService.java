package com.saferide.student.application.service;

import com.saferide.student.application.event.StudentCreated;
import com.saferide.student.application.exception.AppException;
import com.saferide.student.application.exception.AppException.ConflictException;
import com.saferide.student.application.port.EventPublisherPort;
import com.saferide.student.application.port.SchoolStatusPort;
import com.saferide.student.application.port.StudentRepositoryPort;
import com.saferide.student.domain.Student;
import java.time.Instant;
import java.util.List;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class StudentService {

    public static final String STUDENT_CREATED_KEY = "student-created";
    public static final String DUPLICATE_ADMISSION = "A student with this admission number already exists.";
    public static final String SCHOOL_NOT_APPROVED = "Your school is not approved yet.";
    public static final String STUDENT_NOT_FOUND = "Student not found.";

    private final StudentRepositoryPort repository;
    private final EventPublisherPort events;
    private final SchoolStatusPort schoolStatus;

    public StudentService(StudentRepositoryPort repository, EventPublisherPort events, SchoolStatusPort schoolStatus) {
        this.repository = repository;
        this.events = events;
        this.schoolStatus = schoolStatus;
    }

    @Transactional
    public Student create(
        UUID schoolId,
        String firstName,
        String lastName,
        String admissionNumber,
        String grade,
        String parentFirstName,
        String parentLastName,
        String parentEmail,
        String parentPhone) {

        if (!schoolStatus.isApproved(schoolId)) throw new AppException.ForbiddenException(SCHOOL_NOT_APPROVED);

        if (repository.existsBySchoolIdAndAdmissionNumber(schoolId, admissionNumber.trim()))
            throw new ConflictException(DUPLICATE_ADMISSION);

        var student = Student.create(
            schoolId,
            firstName,
            lastName,
            admissionNumber,
            grade,
            parentFirstName,
            parentLastName,
            parentEmail,
            parentPhone);
        repository.save(student);

        events.publish(
            STUDENT_CREATED_KEY,
            new StudentCreated(
                student.getId(),
                student.getSchoolId(),
                student.getFirstName(),
                student.getLastName(),
                student.getParentFirstName(),
                student.getParentLastName(),
                student.getParentEmail(),
                student.getParentPhone(),
                Instant.now()));

        return student;
    }

    @Transactional(readOnly = true)
    public Page<Student> list(UUID schoolId, String search, Pageable pageable) {
        return repository.findPage(schoolId, search, pageable);
    }

    @Transactional(readOnly = true)
    public Student getById(UUID schoolId, UUID studentId) {
        return repository
            .findByIdAndSchoolId(studentId, schoolId)
            .orElseThrow(() -> new AppException.NotFoundException(STUDENT_NOT_FOUND));
    }

    @Transactional
    public Student assignRoute(UUID schoolId, UUID studentId, UUID routeId, UUID pickupStopId, UUID dropStopId) {
        var student = getById(schoolId, studentId);
        student.assignRoute(routeId, pickupStopId, dropStopId);
        repository.save(student);
        return student;
    }

    @Transactional(readOnly = true)
    public List<Student> roster(UUID schoolId, UUID routeId) {
        return repository.findRoster(schoolId, routeId);
    }
}
