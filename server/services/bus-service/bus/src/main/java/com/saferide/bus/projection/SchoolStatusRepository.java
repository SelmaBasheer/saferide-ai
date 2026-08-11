package com.saferide.bus.projection;

import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface SchoolStatusRepository extends JpaRepository<SchoolStatus, UUID> {
    boolean existsBySchoolIdAndStatus(UUID schoolId, String status);
}
