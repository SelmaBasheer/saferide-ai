package com.saferide.bus.repository;

import com.saferide.bus.entity.Bus;
import com.saferide.bus.entity.BusStatus;
import java.util.UUID;
import org.springframework.data.jpa.domain.Specification;

/**
 * Query fragments, one condition each. Composing them means the generated SQL
 * carries only the conditions that actually apply — unlike a single statement
 * with ":search is null or ...", where one query plan has to serve both "every
 * bus" and "buses matching a term".
 */
public final class BusSpecifications {

    private BusSpecifications() {}

    public static Specification<Bus> forSchool(UUID schoolId) {
        return (root, query, cb) -> cb.equal(root.get("schoolId"), schoolId);
    }

    /** Null when inactive buses are wanted too, so the caller needs no branch. */
    public static Specification<Bus> activeOnly(boolean includeInactive) {
        return includeInactive ? null : (root, query, cb) -> cb.equal(root.get("status"), BusStatus.ACTIVE);
    }

    /**
     * Returns null when there is nothing to search for. Spring Data reads a null
     * specification as "no condition".
     */
    public static Specification<Bus> matching(String search) {
        if (search == null || search.isBlank()) {
            return null;
        }

        String pattern = "%" + search.trim().toLowerCase() + "%";

        return (root, query, cb) -> cb.or(
                cb.like(cb.lower(root.get("registrationNumber")), pattern),
                cb.like(cb.lower(root.get("model")), pattern));
    }
}
