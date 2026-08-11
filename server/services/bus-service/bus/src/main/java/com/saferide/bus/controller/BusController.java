package com.saferide.bus.controller;

import com.saferide.bus.constants.ResponseMessages;
import com.saferide.bus.dto.ApiResponse;
import com.saferide.bus.dto.AssignDriverRequest;
import com.saferide.bus.dto.BusResponse;
import com.saferide.bus.dto.CreateBusRequest;
import com.saferide.bus.dto.PagedResult;
import com.saferide.bus.dto.UpdateBusRequest;
import com.saferide.bus.exception.AppException;
import com.saferide.bus.service.BusService;
import jakarta.validation.Valid;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/buses")
public class BusController {

    private final BusService busService;

    public BusController(BusService busService) {
        this.busService = busService;
    }

    @PostMapping
    public ResponseEntity<ApiResponse<BusResponse>> create(
            @AuthenticationPrincipal Jwt jwt, @Valid @RequestBody CreateBusRequest request) {
        BusResponse created = busService.create(schoolId(jwt), request);
        return ResponseEntity.status(HttpStatus.CREATED).body(ApiResponse.ok(created, ResponseMessages.BUS_CREATED));
    }

    @GetMapping
    public ApiResponse<PagedResult<BusResponse>> list(
            @AuthenticationPrincipal Jwt jwt,
            @RequestParam(required = false) String search,
            @RequestParam(defaultValue = "false") boolean includeInactive,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "10") int pageSize) {
        return ApiResponse.ok(busService.list(schoolId(jwt), search, includeInactive, page, pageSize));
    }

    @GetMapping("/{id}")
    public ApiResponse<BusResponse> getById(@AuthenticationPrincipal Jwt jwt, @PathVariable UUID id) {
        return ApiResponse.ok(busService.getById(schoolId(jwt), id));
    }

    @PutMapping("/{id}")
    public ApiResponse<BusResponse> update(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody UpdateBusRequest request) {
        return ApiResponse.ok(busService.update(schoolId(jwt), id, request), ResponseMessages.BUS_UPDATED);
    }

    @PutMapping("/{id}/driver")
    public ApiResponse<BusResponse> assignDriver(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody AssignDriverRequest request) {
        return ApiResponse.ok(busService.assignDriver(schoolId(jwt), id, request), ResponseMessages.DRIVER_ASSIGNED);
    }

    @DeleteMapping("/{id}")
    public ApiResponse<Void> deactivate(@AuthenticationPrincipal Jwt jwt, @PathVariable UUID id) {
        busService.deactivate(schoolId(jwt), id);
        return ApiResponse.ok(null, ResponseMessages.BUS_DEACTIVATED);
    }

    private static UUID schoolId(Jwt jwt) {
        var claim = jwt.getClaimAsString("schoolId");
        if (claim == null) {
            throw new AppException.ForbiddenException(ResponseMessages.MISSING_SCHOOL_CLAIM);
        }
        try {
            return UUID.fromString(claim);
        } catch (IllegalArgumentException e) {
            throw new AppException.ForbiddenException(ResponseMessages.MISSING_SCHOOL_CLAIM);
        }
    }
}
