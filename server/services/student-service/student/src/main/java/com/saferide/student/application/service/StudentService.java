package com.saferide.student.application.service;

import com.saferide.student.application.event.StudentCreated;
import com.saferide.student.application.exception.AppException.ConflictException;
import com.saferide.student.application.port.EventPublisherPort;
import com.saferide.student.application.port.StudentRepositoryPort;
import com.saferide.student.domain.Student;
import java.time.Instant;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class StudentService {

    public static final String STUDENT_CREATED_KEY = "student-created";
    public static final String DUPLICATE_ADMISSION = "A student with this admission number already exists.";

    private final StudentRepositoryPort repository;
    private final EventPublisherPort events;

    public StudentService(StudentRepositoryPort repository, EventPublisherPort events) {
        this.repository = repository;
        this.events = events;
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
}
