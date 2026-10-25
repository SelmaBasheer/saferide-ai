package com.saferide.route.projection;

import java.util.Optional;
import java.util.UUID;
import org.springframework.data.mongodb.repository.MongoRepository;

public interface DriverStatusRepository extends MongoRepository<DriverStatus, UUID> {

    Optional<DriverStatus> findBySchoolIdAndEmail(UUID schoolId, String email);
}
