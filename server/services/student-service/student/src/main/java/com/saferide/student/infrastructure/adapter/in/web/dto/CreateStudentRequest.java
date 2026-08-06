package com.saferide.student.infrastructure.adapter.in.web.dto;

import jakarta.validation.constraints.*;

public record CreateStudentRequest(
        @NotBlank @Size(max = 75) String firstName,
        @NotBlank @Size(max = 75) String lastName,
        @NotBlank @Size(max = 30) String admissionNumber,
        @NotBlank @Size(max = 20) String grade,
        @NotBlank @Size(max = 75) String parentFirstName,
        @NotBlank @Size(max = 75) String parentLastName,
        @NotBlank @Email @Size(max = 255) String parentEmail,
        @NotBlank @Pattern(regexp = "^\\+?[0-9]{10,15}$") String parentPhone) {}
