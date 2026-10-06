package com.saferide.route.security;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.util.Arrays;
import java.util.List;
import java.util.UUID;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.filter.OncePerRequestFilter;

/**
 * Builds the caller's identity from the headers the gateway adds, instead of
 * validating a JWT here.
 *
 * <p>The gateway is the only ingress and it strips these headers from anything a
 * client sends, so their presence means the gateway put them there. That is the
 * whole security model — which is why this service must not be reachable except
 * through the gateway.
 */
public class GatewayAuthenticationFilter extends OncePerRequestFilter {

    private static final String USER_ID = "X-SafeRide-UserId";
    private static final String EMAIL = "X-SafeRide-Email";
    private static final String SCHOOL_ID = "X-SafeRide-SchoolId";
    private static final String ROLES = "X-SafeRide-Roles";

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {

        String userId = request.getHeader(USER_ID);

        // No header means the request did not come through the gateway, or came
        // through it unauthenticated. The entry point below reports it.
        if (userId != null && !userId.isBlank()) {
            var principal = new GatewayUser(
                    uuidOrNull(userId), request.getHeader(EMAIL), uuidOrNull(request.getHeader(SCHOOL_ID)));

            SecurityContextHolder.getContext()
                    .setAuthentication(new UsernamePasswordAuthenticationToken(
                            principal, null, authorities(request.getHeader(ROLES))));
        }

        chain.doFilter(request, response);
    }

    /**
     * ROLE_ prefixed, because hasRole("SchoolAdmin") looks for ROLE_SchoolAdmin.
     * The JWT converter did the same, so the matchers keep working unchanged.
     */
    private static List<GrantedAuthority> authorities(String roles) {
        if (roles == null || roles.isBlank()) return List.of();

        return Arrays.stream(roles.split(","))
                .map(String::trim)
                .filter(r -> !r.isEmpty())
                .map(r -> (GrantedAuthority) new SimpleGrantedAuthority("ROLE_" + r))
                .toList();
    }

    /** A malformed id is treated as absent; requireSchoolId reports it. */
    private static UUID uuidOrNull(String value) {
        if (value == null || value.isBlank()) return null;
        try {
            return UUID.fromString(value);
        } catch (IllegalArgumentException e) {
            return null;
        }
    }
}
