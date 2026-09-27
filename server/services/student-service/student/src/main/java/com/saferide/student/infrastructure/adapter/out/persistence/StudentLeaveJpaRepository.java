package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.domain.StudentLeave;
import java.time.LocalDate;
import java.util.Collection;
import java.util.List;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface StudentLeaveJpaRepository extends JpaRepository<StudentLeave, UUID> {

    boolean existsByStudentIdAndOnDate(UUID studentId, LocalDate onDate);

    void deleteByStudentIdAndOnDate(UUID studentId, LocalDate onDate);

    List<StudentLeave> findByStudentIdAndOnDateGreaterThanEqualOrderByOnDate(UUID studentId, LocalDate from);

    List<StudentLeave> findByStudentIdInAndOnDate(Collection<UUID> studentIds, LocalDate onDate);
}
