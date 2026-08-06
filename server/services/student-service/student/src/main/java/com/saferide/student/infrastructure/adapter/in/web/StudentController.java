package com.saferide.student.infrastructure.adapter.in.web;

import com.saferide.student.application.exception.AppException.ForbiddenException;
import com.saferide.student.application.service.StudentService;
import com.saferide.student.infrastructure.adapter.in.web.dto.ApiResponse;
import com.saferide.student.infrastructure.adapter.in.web.dto.CreateStudentRequest;
import com.saferide.student.infrastructure.adapter.in.web.dto.PagedResult;
import com.saferide.student.infrastructure.adapter.in.web.dto.StudentResponse;
import jakarta.validation.Valid;
import java.util.UUID;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/students")
public class StudentController {

    public static final String STUDENT_CREATED_MSG = "Student created successfully.";

    private final StudentService service;
    private final StudentMapper mapper;

    public StudentController(StudentService service, StudentMapper mapper) {
        this.service = service;
        this.mapper = mapper;
    }

    private static UUID schoolId(Jwt jwt) {
        var claim = jwt.getClaimAsString("schoolId");
        if (claim == null) throw new ForbiddenException("No school context on this account.");
        return UUID.fromString(claim);
    }

    @PostMapping
    public ResponseEntity<ApiResponse<StudentResponse>> create(
            @AuthenticationPrincipal Jwt jwt, @Valid @RequestBody CreateStudentRequest req) {

        var student = service.create(
                schoolId(jwt),
                req.firstName(),
                req.lastName(),
                req.admissionNumber(),
                req.grade(),
                req.parentFirstName(),
                req.parentLastName(),
                req.parentEmail(),
                req.parentPhone());

        return ResponseEntity.status(HttpStatus.CREATED)
                .body(ApiResponse.ok(mapper.toResponse(student), STUDENT_CREATED_MSG));
    }

    @GetMapping
    public ApiResponse<PagedResult<StudentResponse>> list(
            @AuthenticationPrincipal Jwt jwt,
            @RequestParam(required = false) String search,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "10") int pageSize) {

        page = Math.max(page, 1);
        pageSize = Math.clamp(pageSize, 1, 50);

        var result = service.list(
                schoolId(jwt), search, PageRequest.of(page - 1, pageSize, Sort.by(Sort.Direction.DESC, "createdAt")));

        var items = result.getContent().stream().map(mapper::toResponse).toList();
        return ApiResponse.ok(new PagedResult<>(items, result.getTotalElements(), page, pageSize));
    }
}
