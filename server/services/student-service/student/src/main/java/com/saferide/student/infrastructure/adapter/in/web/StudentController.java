package com.saferide.student.infrastructure.adapter.in.web;

import com.saferide.student.application.service.StudentLeaveService;
import com.saferide.student.application.service.StudentService;
import com.saferide.student.domain.Student;
import com.saferide.student.infrastructure.adapter.in.web.dto.*;
import com.saferide.student.infrastructure.config.GatewayUser;
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
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/students")
public class StudentController {

    public static final String STUDENT_CREATED_MSG = "Student created successfully.";
    public static final String LEAVE_MARKED_MSG = "Leave recorded.";
    public static final String LEAVE_CANCELLED_MSG = "Leave cancelled.";

    private final StudentService service;
    private final StudentLeaveService leaveService;
    private final StudentMapper mapper;

    public StudentController(StudentService service, StudentLeaveService leaveService, StudentMapper mapper) {
        this.service = service;
        this.leaveService = leaveService;
        this.mapper = mapper;
    }

    @PostMapping
    public ResponseEntity<ApiResponse<StudentResponse>> create(
            @AuthenticationPrincipal GatewayUser user, @Valid @RequestBody CreateStudentRequest req) {

        var student = service.create(
                user.requireSchoolId(),
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
            @AuthenticationPrincipal GatewayUser user,
            @RequestParam(required = false) String search,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "10") int pageSize) {

        page = Math.max(page, 1);
        pageSize = Math.clamp(pageSize, 1, 50);

        var result = service.list(
                user.requireSchoolId(),
                search,
                PageRequest.of(page - 1, pageSize, Sort.by(Sort.Direction.DESC, "createdAt")));

        var items = result.getContent().stream().map(mapper::toResponse).toList();
        return ApiResponse.ok(new PagedResult<>(items, result.getTotalElements(), page, pageSize));
    }

    @PutMapping("/{id}/route")
    public ApiResponse<StudentResponse> assignRoute(
            @AuthenticationPrincipal GatewayUser user,
            @PathVariable UUID id,
            @Valid @RequestBody AssignRouteRequest req) {
        var student =
                service.assignRoute(user.requireSchoolId(), id, req.routeId(), req.pickupStopId(), req.dropStopId());
        return ApiResponse.ok(mapper.toResponse(student), "Route assigned successfully.");
    }

    @GetMapping("/roster")
    public ApiResponse<List<RosterEntryResponse>> roster(
            @AuthenticationPrincipal GatewayUser user, @RequestParam UUID routeId) {
        var students = service.roster(user.requireSchoolId(), routeId);

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
    public ApiResponse<StudentResponse> get(@AuthenticationPrincipal GatewayUser user, @PathVariable UUID id) {
        return ApiResponse.ok(mapper.toResponse(service.getById(user.requireSchoolId(), id)));
    }

    // ----- parent endpoints -----
    //
    // None of these read schoolId, because the authorisation is ownership: the
    // student record has to carry the caller's email. A school admin calling
    // /mine gets an empty list rather than an error.

    @GetMapping("/mine")
    public ApiResponse<List<MyChildResponse>> myChildren(@AuthenticationPrincipal GatewayUser user) {
        var items = leaveService.myChildren(user.requireEmail()).stream()
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
            @AuthenticationPrincipal GatewayUser user,
            @PathVariable UUID id,
            @Valid @RequestBody MarkLeaveRequest req) {

        leaveService.mark(user.requireEmail(), id, req.date(), req.reason(), user.requireUserId());
        return ApiResponse.ok(null, LEAVE_MARKED_MSG);
    }

    @DeleteMapping("/{id}/leave/{date}")
    public ApiResponse<Void> cancelLeave(
            @AuthenticationPrincipal GatewayUser user,
            @PathVariable UUID id,
            @PathVariable @DateTimeFormat(iso = DateTimeFormat.ISO.DATE) LocalDate date) {

        leaveService.cancel(user.requireEmail(), id, date);
        return ApiResponse.ok(null, LEAVE_CANCELLED_MSG);
    }
}
