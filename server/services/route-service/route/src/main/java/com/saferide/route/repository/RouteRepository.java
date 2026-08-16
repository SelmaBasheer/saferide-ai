package com.saferide.route.repository;

import com.saferide.route.document.Route;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.mongodb.repository.MongoRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface RouteRepository extends MongoRepository<Route, UUID> {

    Optional<Route> findByIdAndSchoolId(UUID id, UUID schoolId);

    boolean existsBySchoolIdAndCode(UUID schoolId, String code);

    boolean existsBySchoolIdAndCodeAndIdNot(UUID schoolId, String code, UUID id);
}
