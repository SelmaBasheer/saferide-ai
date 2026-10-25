package com.saferide.route.projection;

import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.mongodb.repository.MongoRepository;

public interface BusStatusRepository extends MongoRepository<BusStatus, UUID> {

    Optional<BusStatus> findByBusIdAndSchoolId(UUID busId, UUID schoolId);

    List<BusStatus> findBySchoolIdAndAssignedDriverId(UUID schoolId, UUID assignedDriverId);
}
