package com.saferide.student.infrastructure.adapter.in.web.dto;

import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.LocalDate;

public record MarkLeaveRequest(@NotNull LocalDate date, @Size(max = 200) String reason) {}
