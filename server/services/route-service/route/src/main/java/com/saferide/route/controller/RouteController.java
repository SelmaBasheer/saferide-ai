package com.saferide.route.controller;

import com.saferide.route.constants.ResponseMessages;
import com.saferide.route.dto.*;
import com.saferide.route.exception.AppException;
import com.saferide.route.service.RouteService;
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
@RequestMapping("/api/routes")
public class RouteController {

    private final RouteService routeService;

    public RouteController(RouteService routeService) {
        this.routeService = routeService;
    }

    @PostMapping
    public ResponseEntity<ApiResponse<RouteResponse>> create(
            @AuthenticationPrincipal Jwt jwt, @Valid @RequestBody CreateRouteRequest request) {
        RouteResponse created = routeService.create(schoolId(jwt), request);
        return ResponseEntity.status(HttpStatus.CREATED).body(ApiResponse.ok(created, ResponseMessages.ROUTE_CREATED));
    }

    @GetMapping
    public ApiResponse<PagedResult<RouteResponse>> list(
            @AuthenticationPrincipal Jwt jwt,
            @RequestParam(required = false) String search,
            @RequestParam(defaultValue = "false") boolean includeInactive,
            @RequestParam(defaultValue = "1") int page,
            @RequestParam(defaultValue = "10") int pageSize) {
        return ApiResponse.ok(routeService.list(schoolId(jwt), search, includeInactive, page, pageSize));
    }

    @GetMapping("/{id}")
    public ApiResponse<RouteResponse> getById(@AuthenticationPrincipal Jwt jwt, @PathVariable UUID id) {
        return ApiResponse.ok(routeService.getById(schoolId(jwt), id));
    }

    @PutMapping("/{id}")
    public ApiResponse<RouteResponse> update(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody UpdateRouteRequest request) {
        return ApiResponse.ok(routeService.update(schoolId(jwt), id, request), ResponseMessages.ROUTE_UPDATED);
    }

    @DeleteMapping("/{id}")
    public ApiResponse<Void> deactivate(@AuthenticationPrincipal Jwt jwt, @PathVariable UUID id) {
        routeService.deactivate(schoolId(jwt), id);
        return ApiResponse.ok(null, ResponseMessages.ROUTE_DEACTIVATED);
    }

    @PutMapping("/{id}/stops")
    public ApiResponse<RouteResponse> replaceStops(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody ReplaceStopsRequest request) {
        return ApiResponse.ok(routeService.replaceStops(schoolId(jwt), id, request), ResponseMessages.STOPS_UPDATED);
    }

    @PutMapping("/{id}/path")
    public ApiResponse<RouteResponse> replacePath(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody ReplacePathRequest request) {
        return ApiResponse.ok(routeService.replacePath(schoolId(jwt), id, request), ResponseMessages.PATH_UPDATED);
    }

    @PutMapping("/{id}/bus")
    public ApiResponse<RouteResponse> assignBus(
            @AuthenticationPrincipal Jwt jwt, @PathVariable UUID id, @Valid @RequestBody AssignBusRequest request) {
        return ApiResponse.ok(routeService.assignBus(schoolId(jwt), id, request), ResponseMessages.BUS_ASSIGNED);
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
