package com.saferide.student.infrastructure.adapter.out.persistence;

import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface SchoolStatusJpaRepository extends JpaRepository<SchoolStatusProjection, UUID> {
    boolean existsBySchoolIdAndStatus(UUID schoolId, String status);
}
