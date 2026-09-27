package com.saferide.student.infrastructure.adapter.in.web;

import com.saferide.student.application.exception.AppException.ForbiddenException;
import com.saferide.student.application.service.StudentLeaveService;
import com.saferide.student.application.service.StudentService;
import com.saferide.student.domain.Student;
import com.saferide.student.infrastructure.adapter.in.web.dto.*;
import jakarta.validation.Valid;
import java.time.LocalDate;
import java.util.List;
import java.util.UUID;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Sort;
import org.springframework.format.annotation.DateTimeFormat;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/students")
public class StudentController {

    public static final String STUDENT_CREATED_MSG = "Student created successfully.";
    public static final String LEAVE_MARKED_MSG = "Leave recorded.";
    public static final String LEAVE_CANCELLED_MSG = "Leave cancelled.";

    /** .NET emits the long schema URIs; a hand-built token uses the short names. */
    private static final String DOTNET_EMAIL_CLAIM =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

    private static final String DOTNET_NAMEID_CLAIM =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

    private final StudentService service;
    private final StudentLeaveService leaveService;
    private final StudentMapper mapper;

    public StudentController(StudentService service, StudentLeaveService leaveService, StudentMapper mapper) {
        this.service = service;
        this.leaveService = leaveService;
        this.mapper = mapper;
    }

    private static UUID schoolId(Jwt jwt) {
        var claim = jwt.getClaimAsString("schoolId");
        if (claim == null) throw new ForbiddenException("No school context on this account.");
        try {
            return UUID.fromString(claim);
        } catch (IllegalArgumentException e) {
            throw new ForbiddenException("No school context on this account.");
        }
    }

    /** What links a parent to their children — a student record carries the email. */
    private static String email(Jwt jwt) {
        var claim = jwt.getClaimAsString("email");
        if (claim == null) claim = jwt.getClaimAsString(DOTNET_EMAIL_CLAIM);
        if (claim == null || claim.isBlank()) throw new ForbiddenException("No email on this account.");
        return claim;
    }

    private static UUID userId(Jwt jwt) {
        var claim = jwt.getSubject();
        if (claim == null) claim = jwt.getClaimAsString(DOTNET_NAMEID_CLAIM);
        if (claim == null) throw new ForbiddenException("No user id on this account.");
        try {
            return UUID.fromString(claim);
        } catch (IllegalArgumentException e) {
            throw new ForbiddenException("No user id on this account.");
        }
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

    @PutMapping("/{id}/route")
    public ApiResponse<StudentResponse> assignRoute(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody AssignRouteRequest req) {
        var student = service.assignRoute(schoolId(jwt), id, req.routeId(), req.pickupStopId(), req.dropStopId());
        return ApiResponse.ok(mapper.toResponse(student), "Route assigned successfully.");
    }

    @GetMapping("/roster")
    public ApiResponse<List<RosterEntryResponse>> roster(@AuthenticationPrincipal Jwt jwt, @RequestParam UUID routeId) {
        var students = service.roster(schoolId(jwt), routeId);

        // One query for the whole route rather than one per child.
        var onLeave = leaveService.idsOnLeaveToday(
                students.stream().map(Student::getId).toList());

        var items = students.stream()
                .map(s -> new RosterEntryResponse(
                        s.getId(),
                        s.getFirstName(),
                        s.getLastName(),
                        s.getParentEmail(),
                        s.getPickupStopId(),
                        onLeave.contains(s.getId())))
                .toList();

        return ApiResponse.ok(items);
    }

    @GetMapping("/{id}")
    public ApiResponse<StudentResponse> get(@AuthenticationPrincipal Jwt jwt, @PathVariable UUID id) {
        return ApiResponse.ok(mapper.toResponse(service.getById(schoolId(jwt), id)));
    }

    // ----- parent endpoints -----
    //
    // None of these read schoolId, because the authorisation is ownership: the
    // student record has to carry the caller's email. A school admin calling
    // /mine gets an empty list rather than an error.

    @GetMapping("/mine")
    public ApiResponse<List<MyChildResponse>> myChildren(@AuthenticationPrincipal Jwt jwt) {
        var items = leaveService.myChildren(email(jwt)).stream()
                .map(c -> new MyChildResponse(
                        c.student().getId(),
                        c.student().getFirstName(),
                        c.student().getLastName(),
                        c.student().getGrade(),
                        c.student().getRouteId(),
                        c.student().getPickupStopId(),
                        c.upcomingLeaves()))
                .toList();

        return ApiResponse.ok(items);
    }

    @PostMapping("/{id}/leave")
    public ApiResponse<Void> markLeave(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody MarkLeaveRequest req) {

        leaveService.mark(email(jwt), id, req.date(), req.reason(), userId(jwt));
        return ApiResponse.ok(null, LEAVE_MARKED_MSG);
    }

    @DeleteMapping("/{id}/leave/{date}")
    public ApiResponse<Void> cancelLeave(
            @AuthenticationPrincipal Jwt jwt,
            @PathVariable UUID id,
            @PathVariable @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate date) {

        leaveService.cancel(email(jwt), id, date);
        return ApiResponse.ok(null, LEAVE_CANCELLED_MSG);
    }
}
