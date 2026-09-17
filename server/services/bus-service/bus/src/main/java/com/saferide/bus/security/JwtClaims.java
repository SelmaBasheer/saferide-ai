package com.saferide.bus.security;

import com.saferide.bus.constants.ResponseMessages;
import com.saferide.bus.exception.AppException;
import java.util.UUID;
import org.springframework.security.oauth2.jwt.Jwt;

public final class JwtClaims {

    private JwtClaims() {}

    public static UUID schoolId(Jwt jwt) {
        return required(jwt.getClaimAsString("schoolId"), ResponseMessages.MISSING_SCHOOL_CLAIM);
    }

    public static UUID userId(Jwt jwt) {
        return required(jwt.getSubject(), ResponseMessages.MISSING_USER_CLAIM);
    }

    private static UUID required(String claim, String message) {
        if (claim == null) {
            throw new AppException.ForbiddenException(message);
        }

        try {
            return UUID.fromString(claim);
        } catch (IllegalArgumentException e) {
            throw new AppException.ForbiddenException(message);
        }
    }
}
