package com.saferide.route.security;

import com.saferide.route.constants.ResponseMessages;
import com.saferide.route.exception.AppException;
import java.util.UUID;

public record GatewayUser(UUID userId, String email, UUID schoolId) {

    public UUID requireSchoolId() {
        if (schoolId == null) {
            throw new AppException.ForbiddenException(ResponseMessages.MISSING_SCHOOL_CLAIM);
        }
        return schoolId;
    }
}
