package com.saferide.route.config;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.saferide.route.dto.ApiResponse;
import com.saferide.route.security.GatewayAuthenticationFilter;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.http.MediaType;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.web.AuthenticationEntryPoint;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.access.AccessDeniedHandler;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

    @Bean
    AuthenticationEntryPoint apiAuthenticationEntryPoint(ObjectMapper mapper) {
        return (request, response, ex) ->
                writeError(mapper, response, 401, "Auth.Unauthorized", "Authentication is required.");
    }

    @Bean
    AccessDeniedHandler apiAccessDeniedHandler(ObjectMapper mapper) {
        return (request, response, ex) ->
                writeError(mapper, response, 403, "Auth.Forbidden", "You do not have access to this resource.");
    }

    private static void writeError(
            ObjectMapper mapper, HttpServletResponse response, int status, String code, String message)
            throws IOException {
        response.setStatus(status);
        response.setContentType(MediaType.APPLICATION_JSON_VALUE);
        response.setCharacterEncoding(StandardCharsets.UTF_8.name());
        mapper.writeValue(response.getOutputStream(), ApiResponse.fail(code, message));
    }

    @Bean
    SecurityFilterChain filterChain(
            HttpSecurity http,
            AuthenticationEntryPoint apiAuthenticationEntryPoint,
            AccessDeniedHandler apiAccessDeniedHandler)
            throws Exception {
        http.csrf(csrf -> csrf.disable())
                .sessionManagement(s -> s.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                .authorizeHttpRequests(
                        auth -> auth.requestMatchers("/actuator/health", "/swagger-ui/**", "/v3/api-docs/**")
                                .permitAll()
                                .requestMatchers(HttpMethod.GET, "/api/routes", "/api/routes/*")
                                .hasAnyRole("SchoolAdmin", "Driver")
                                .requestMatchers("/api/routes/**")
                                .hasRole("SchoolAdmin")
                                .anyRequest()
                                .authenticated())
                .exceptionHandling(e -> e.authenticationEntryPoint(apiAuthenticationEntryPoint)
                        .accessDeniedHandler(apiAccessDeniedHandler))
                // Identity comes from the gateway's headers; this service no
                // longer validates tokens.
                .addFilterBefore(new GatewayAuthenticationFilter(), UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }
}
