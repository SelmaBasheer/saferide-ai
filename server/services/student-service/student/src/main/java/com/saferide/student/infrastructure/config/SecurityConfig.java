package com.saferide.student.infrastructure.config;

import jakarta.servlet.http.HttpServletResponse;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;

@Configuration
@EnableWebSecurity
public class SecurityConfig {

    @Bean
    SecurityFilterChain filterChain(HttpSecurity http) throws Exception {
        http.csrf(csrf -> csrf.disable())
                .sessionManagement(s -> s.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                .authorizeHttpRequests(
                        auth -> auth.requestMatchers("/actuator/health", "/swagger-ui/**", "/v3/api-docs/**")
                                .permitAll()
                                .requestMatchers("/api/students/roster")
                                .hasAnyRole("SchoolAdmin", "Driver")
                                .requestMatchers(HttpMethod.GET, "/api/students/mine")
                                .hasRole("Parent")
                                .requestMatchers(HttpMethod.POST, "/api/students/*/leave")
                                .hasRole("Parent")
                                .requestMatchers(HttpMethod.DELETE, "/api/students/*/leave/**")
                                .hasRole("Parent")
                                .requestMatchers("/api/students/**")
                                .hasRole("SchoolAdmin")
                                .anyRequest()
                                .authenticated())
                // Without an entry point, an unauthenticated request is refused
                // with 403. The gateway distinguishes 401 from 403 in the error
                // envelope, and "you are not signed in" is a different problem
                // from "you may not do this".
                .exceptionHandling(e -> e.authenticationEntryPoint(
                        (req, res, ex) -> res.setStatus(HttpServletResponse.SC_UNAUTHORIZED)))
                .addFilterBefore(new GatewayAuthenticationFilter(), UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }
}
