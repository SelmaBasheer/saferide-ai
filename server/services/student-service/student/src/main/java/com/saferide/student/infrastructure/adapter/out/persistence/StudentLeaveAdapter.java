package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.application.port.StudentLeavePort;
import com.saferide.student.domain.StudentLeave;
import java.time.LocalDate;
import java.util.Collection;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

@Component
public class StudentLeaveAdapter implements StudentLeavePort {

    private final StudentLeaveJpaRepository jpa;

    public StudentLeaveAdapter(StudentLeaveJpaRepository jpa) {
        this.jpa = jpa;
    }

    @Override
    public void save(StudentLeave leave) {
        jpa.save(leave);
    }

    @Override
    public boolean exists(UUID studentId, LocalDate onDate) {
        return jpa.existsByStudentIdAndOnDate(studentId, onDate);
    }

    @Override
    @Transactional
    public void delete(UUID studentId, LocalDate onDate) {
        jpa.deleteByStudentIdAndOnDate(studentId, onDate);
    }

    @Override
    public List<StudentLeave> findFrom(UUID studentId, LocalDate from) {
        return jpa.findByStudentIdAndOnDateGreaterThanEqualOrderByOnDate(studentId, from);
    }

    @Override
    public Set<UUID> idsOnLeave(Collection<UUID> studentIds, LocalDate onDate) {
        if (studentIds.isEmpty()) {
            return Set.of();
        }

        return jpa.findByStudentIdInAndOnDate(studentIds, onDate).stream()
                .map(StudentLeave::getStudentId)
                .collect(Collectors.toSet());
    }
}
