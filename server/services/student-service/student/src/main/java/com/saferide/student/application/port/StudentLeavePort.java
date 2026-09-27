package com.saferide.student.application.port;

import com.saferide.student.domain.StudentLeave;
import java.time.LocalDate;
import java.util.Collection;
import java.util.List;
import java.util.Set;
import java.util.UUID;

public interface StudentLeavePort {

    void save(StudentLeave leave);

    boolean exists(UUID studentId, LocalDate onDate);

    void delete(UUID studentId, LocalDate onDate);

    /** What a parent sees on the child's card: today onwards. */
    List<StudentLeave> findFrom(UUID studentId, LocalDate from);

    /**
     * One query for a whole roster rather than one per child — a route with
     * forty students would otherwise be forty round trips at every trip start.
     */
    Set<UUID> idsOnLeave(Collection<UUID> studentIds, LocalDate onDate);
}
