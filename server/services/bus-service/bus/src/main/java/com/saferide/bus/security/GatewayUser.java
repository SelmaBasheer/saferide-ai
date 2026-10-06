package com.saferide.bus.security;

import com.saferide.bus.exception.AppException.ForbiddenException;
import java.util.UUID;

public record GatewayUser(UUID userId, String email, UUID schoolId) {

    public UUID requireSchoolId() {
        if (schoolId == null) throw new ForbiddenException("No school context on this account.");
        return schoolId;
    }

    public UUID requireUserId() {
        if (userId == null) throw new ForbiddenException("No user id on this account.");
        return userId;
    }

    public String requireEmail() {
        if (email == null || email.isBlank()) throw new ForbiddenException("No email on this account.");
        return email;
    }
}
