package com.saferide.student.infrastructure.adapter.out.persistence;

import com.saferide.student.domain.Student;
import java.util.UUID;
import org.springframework.data.jpa.domain.Specification;

/**
 * Query fragments, one condition each. Composing them means the generated SQL
 * carries only the conditions that actually apply — unlike a single statement
 * with ":search is null or ...", where one query plan has to serve both "every
 * student" and "students matching a term".
 */
public final class StudentSpecifications {

    private StudentSpecifications() {}

    public static Specification<Student> forSchool(UUID schoolId) {
        return (root, query, cb) -> cb.equal(root.get("schoolId"), schoolId);
    }

    /**
     * Returns null when there is nothing to search for. Spring Data reads a null
     * specification as "no condition", so the caller needs no branch of its own.
     */
    public static Specification<Student> matching(String search) {
        if (search == null || search.isBlank()) {
            return null;
        }

        String pattern = "%" + search.trim().toLowerCase() + "%";

        return (root, query, cb) -> cb.or(
                cb.like(cb.lower(root.get("firstName")), pattern),
                cb.like(cb.lower(root.get("lastName")), pattern),
                cb.like(cb.lower(root.get("admissionNumber")), pattern),
                cb.like(cb.lower(root.get("parentEmail")), pattern));
    }
}
