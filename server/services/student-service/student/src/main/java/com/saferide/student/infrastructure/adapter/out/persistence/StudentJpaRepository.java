package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.domain.Student;
import com.saferide.student.domain.StudentStatus;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;

public interface StudentJpaRepository extends JpaRepository<Student, UUID>, JpaSpecificationExecutor<Student> {

    boolean existsBySchoolIdAndAdmissionNumber(UUID schoolId, String admissionNumber);

    Optional<Student> findByIdAndSchoolId(UUID id, UUID schoolId);

    List<Student> findBySchoolIdAndRouteIdAndStatus(UUID schoolId, UUID routeId, StudentStatus status);
}
