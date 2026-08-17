package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.application.port.StudentRepositoryPort;
import com.saferide.student.domain.Student;
import com.saferide.student.domain.StudentStatus;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Component;

@Component
public class StudentRepositoryAdapter implements StudentRepositoryPort {

    private final StudentJpaRepository jpa;

    public StudentRepositoryAdapter(StudentJpaRepository jpa) {
        this.jpa = jpa;
    }

    @Override
    public Student save(Student student) {
        return jpa.save(student);
    }

    @Override
    public boolean existsBySchoolIdAndAdmissionNumber(UUID schoolId, String admissionNumber) {
        return jpa.existsBySchoolIdAndAdmissionNumber(schoolId, admissionNumber);
    }

    @Override
    public Page<Student> findPage(UUID schoolId, String search, Pageable pageable) {
        return jpa.search(schoolId, (search == null || search.isBlank()) ? null : search.trim(), pageable);
    }

    @Override
    public Optional<Student> findByIdAndSchoolId(UUID id, UUID schoolId) {
        return jpa.findByIdAndSchoolId(id, schoolId);
    }

    @Override
    public List<Student> findRoster(UUID schoolId, UUID routeId) {
        return jpa.findBySchoolIdAndRouteIdAndStatus(schoolId, routeId, StudentStatus.ACTIVE);
    }
}
