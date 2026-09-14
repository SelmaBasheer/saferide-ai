package com.saferide.bus.repository;

import com.saferide.bus.entity.Bus;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.JpaSpecificationExecutor;
import org.springframework.stereotype.Repository;

@Repository
public interface BusRepository extends JpaRepository<Bus, UUID>, JpaSpecificationExecutor<Bus> {

    Optional<Bus> findByIdAndSchoolId(UUID id, UUID schoolId);

    boolean existsBySchoolIdAndRegistrationNumber(UUID schoolId, String registrationNumber);

    boolean existsBySchoolIdAndRegistrationNumberAndIdNot(UUID schoolId, String registrationNumber, UUID id);
}
