package com.saferide.route.controller;

import com.saferide.route.constants.ResponseMessages;
import com.saferide.route.dto.*;
import com.saferide.route.security.GatewayUser;
import com.saferide.route.service.RouteService;
import jakarta.validation.Valid;
import java.util.UUID;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
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
        @AuthenticationPrincipal GatewayUser caller, @Valid @RequestBody CreateRouteRequest request) {
        RouteResponse created = routeService.create(caller.requireSchoolId(), request);
        return ResponseEntity.status(HttpStatus.CREATED).body(ApiResponse.ok(created, ResponseMessages.ROUTE_CREATED));
    }

    @GetMapping
    public ApiResponse<PagedResult<RouteResponse>> list(
        @AuthenticationPrincipal GatewayUser caller,
        @RequestParam(required = false) String search,
        @RequestParam(defaultValue = "false") boolean includeInactive,
        @RequestParam(defaultValue = "1") int page,
        @RequestParam(defaultValue = "10") int pageSize) {
        return ApiResponse.ok(routeService.list(caller.requireSchoolId(), search, includeInactive, page, pageSize));
    }

    @GetMapping("/{id}")
    public ApiResponse<RouteResponse> getById(@AuthenticationPrincipal GatewayUser caller, @PathVariable UUID id) {
        return ApiResponse.ok(routeService.getById(caller.requireSchoolId(), id));
    }

    @PutMapping("/{id}")
    public ApiResponse<RouteResponse> update(
        @AuthenticationPrincipal GatewayUser caller,
        @PathVariable UUID id,
        @Valid @RequestBody UpdateRouteRequest request) {
        return ApiResponse.ok(routeService.update(caller.requireSchoolId(), id, request), ResponseMessages.ROUTE_UPDATED);
    }

    @DeleteMapping("/{id}")
    public ApiResponse<Void> deactivate(@AuthenticationPrincipal GatewayUser caller, @PathVariable UUID id) {
        routeService.deactivate(caller.requireSchoolId(), id);
        return ApiResponse.ok(null, ResponseMessages.ROUTE_DEACTIVATED);
    }

    @PutMapping("/{id}/stops")
    public ApiResponse<RouteResponse> replaceStops(
        @AuthenticationPrincipal GatewayUser caller,
        @PathVariable UUID id,
        @Valid @RequestBody ReplaceStopsRequest request) {
        return ApiResponse.ok(
            routeService.replaceStops(caller.requireSchoolId(), id, request), ResponseMessages.STOPS_UPDATED);
    }

    @PutMapping("/{id}/path")
    public ApiResponse<RouteResponse> replacePath(
        @AuthenticationPrincipal GatewayUser caller,
        @PathVariable UUID id,
        @Valid @RequestBody ReplacePathRequest request) {
        return ApiResponse.ok(
            routeService.replacePath(caller.requireSchoolId(), id, request), ResponseMessages.PATH_UPDATED);
    }

    @PutMapping("/{id}/bus")
    public ApiResponse<RouteResponse> assignBus(
        @AuthenticationPrincipal GatewayUser caller,
        @PathVariable UUID id,
        @Valid @RequestBody AssignBusRequest request) {
        return ApiResponse.ok(routeService.assignBus(caller.requireSchoolId(), id, request), ResponseMessages.BUS_ASSIGNED);
    }

    @PostMapping("/{id}/path/generate")
    public ApiResponse<RouteResponse> generatePath(
        @AuthenticationPrincipal GatewayUser caller, @PathVariable UUID id) {
        return ApiResponse.ok(routeService.generatePath(caller.requireSchoolId(), id), ResponseMessages.PATH_GENERATED);
    }
}
