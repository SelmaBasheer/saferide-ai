package com.saferide.student.application.port;

import com.saferide.student.domain.Student;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

public interface StudentRepositoryPort {
    Student save(Student student);

    boolean existsBySchoolIdAndAdmissionNumber(UUID schoolId, String admissionNumber);

    Page<Student> findPage(UUID schoolId, String search, Pageable pageable);

    Optional<Student> findByIdAndSchoolId(UUID id, UUID schoolId);

    List<Student> findRoster(UUID schoolId, UUID routeId);
}
