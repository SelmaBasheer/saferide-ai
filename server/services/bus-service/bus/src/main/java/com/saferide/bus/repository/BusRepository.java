package com.saferide.bus.repository;

import com.saferide.bus.entity.Bus;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

@Repository
public interface BusRepository extends JpaRepository<Bus, UUID> {

    @Query(
            """
        select b from Bus b
        where b.schoolId = :schoolId
          and (:includeInactive = true or b.status = com.saferide.bus.entity.BusStatus.ACTIVE)
          and (:search is null
               or lower(b.registrationNumber) like lower(concat('%', :search, '%'))
               or lower(b.model) like lower(concat('%', :search, '%')))
        """)
    Page<Bus> search(
            @Param("schoolId") UUID schoolId,
            @Param("search") String search,
            @Param("includeInactive") boolean includeInactive,
            Pageable pageable);

    Optional<Bus> findByIdAndSchoolId(UUID id, UUID schoolId);

    boolean existsBySchoolIdAndRegistrationNumber(UUID schoolId, String registrationNumber);

    boolean existsBySchoolIdAndRegistrationNumberAndIdNot(UUID schoolId, String registrationNumber, UUID id);
}
