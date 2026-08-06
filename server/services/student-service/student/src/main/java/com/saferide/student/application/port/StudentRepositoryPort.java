package com.saferide.student.application.port;

import com.saferide.student.domain.Student;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

public interface StudentRepositoryPort {
    Student save(Student student);

    boolean existsBySchoolIdAndAdmissionNumber(UUID schoolId, String admissionNumber);

    Page<Student> findPage(UUID schoolId, String search, Pageable pageable);
}
