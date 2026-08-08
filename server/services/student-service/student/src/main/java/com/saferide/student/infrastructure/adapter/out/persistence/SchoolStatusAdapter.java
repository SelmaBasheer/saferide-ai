package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.application.port.SchoolStatusPort;
import com.saferide.student.application.port.SchoolStatuses;
import java.util.UUID;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Component
public class SchoolStatusAdapter implements SchoolStatusPort {

    private final SchoolStatusJpaRepository jpa;

    public SchoolStatusAdapter(SchoolStatusJpaRepository jpa) {
        this.jpa = jpa;
    }

    @Override
    public boolean isApproved(UUID schoolId) {
        return jpa.existsBySchoolIdAndStatus(schoolId, SchoolStatuses.APPROVED);
    }

    @Override
    @Transactional
    public void upsert(UUID schoolId, String status) {
        jpa.findById(schoolId)
                .ifPresentOrElse(
                        row -> {
                            row.update(status);
                            jpa.save(row);
                        },
                        () -> jpa.save(new SchoolStatusProjection(schoolId, status)));
    }
}
