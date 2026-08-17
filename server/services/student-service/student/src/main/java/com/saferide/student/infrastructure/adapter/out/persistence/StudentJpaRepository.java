package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.domain.Student;
import com.saferide.student.domain.StudentStatus;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface StudentJpaRepository extends JpaRepository<Student, UUID> {

    boolean existsBySchoolIdAndAdmissionNumber(UUID schoolId, String admissionNumber);

    @Query(
            """
        SELECT s FROM Student s
        WHERE s.schoolId = :schoolId
          AND (:search IS NULL
               OR LOWER(s.firstName) LIKE LOWER(CONCAT('%', :search, '%'))
               OR LOWER(s.lastName) LIKE LOWER(CONCAT('%', :search, '%'))
               OR LOWER(s.admissionNumber) LIKE LOWER(CONCAT('%', :search, '%'))
               OR LOWER(s.parentEmail) LIKE LOWER(CONCAT('%', :search, '%')))
        """)
    Page<Student> search(@Param("schoolId") UUID schoolId, @Param("search") String search, Pageable pageable);

    Optional<Student> findByIdAndSchoolId(UUID id, UUID schoolId);

    List<Student> findBySchoolIdAndRouteIdAndStatus(UUID schoolId, UUID routeId, StudentStatus status);
}
