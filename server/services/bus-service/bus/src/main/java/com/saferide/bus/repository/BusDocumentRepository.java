package com.saferide.bus.repository;

import com.saferide.bus.entity.BusDocument;
import com.saferide.bus.entity.BusDocumentType;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface BusDocumentRepository extends JpaRepository<BusDocument, UUID> {

    List<BusDocument> findBySchoolIdAndBusIdOrderByUploadedAtDesc(UUID schoolId, UUID busId);

    /** The current document of a type is the most recently uploaded one. */
    Optional<BusDocument> findFirstBySchoolIdAndBusIdAndTypeOrderByUploadedAtDesc(
            UUID schoolId, UUID busId, BusDocumentType type);

    Optional<BusDocument> findByIdAndSchoolId(UUID id, UUID schoolId);
}
