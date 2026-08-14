package com.saferide.route.projection;

import java.util.UUID;
import org.springframework.data.mongodb.repository.MongoRepository;

public interface SchoolStatusRepository extends MongoRepository<SchoolStatus, UUID> {
    boolean existsBySchoolIdAndStatus(UUID schoolId, String status);
}
