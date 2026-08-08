package com.saferide.student.application.port;

import java.util.UUID;

public interface SchoolStatusPort {
    boolean isApproved(UUID schoolId);

    void upsert(UUID schoolId, String status);
}
