package com.saferide.route.service;

import com.saferide.route.client.RoutingClient;
import com.saferide.route.constants.MessagingConstants;
import com.saferide.route.constants.ResponseMessages;
import com.saferide.route.document.Route;
import com.saferide.route.document.RouteStatus;
import com.saferide.route.document.Stop;
import com.saferide.route.dto.*;
import com.saferide.route.exception.AppException;
import com.saferide.route.mapper.RouteMapper;
import com.saferide.route.messaging.RabbitEventPublisher;
import com.saferide.route.messaging.RouteBusAssigned;
import com.saferide.route.messaging.RouteCreated;
import com.saferide.route.projection.SchoolStatusRepository;
import com.saferide.route.projection.SchoolStatuses;
import com.saferide.route.repository.RouteRepository;
import java.time.Instant;
import java.util.*;
import java.util.regex.Pattern;
import java.util.stream.Collectors;
import org.springframework.data.domain.Sort;
import org.springframework.data.geo.Point;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.geo.GeoJsonLineString;
import org.springframework.data.mongodb.core.query.Criteria;
import org.springframework.data.mongodb.core.query.Query;
import org.springframework.stereotype.Service;

@Service
public class RouteService {

    private static final int MAX_PAGE_SIZE = 50;

    // India bounding box, incl. Andaman & Nicobar and Lakshadweep
    private static final double MIN_LAT = 6.0;
    private static final double MAX_LAT = 37.6;
    private static final double MIN_LNG = 68.0;
    private static final double MAX_LNG = 97.5;

    // Tracking marks a stop reached within 100 m. Two stops closer than that
    // cannot be told apart — the bus would arrive at both at once.
    private static final double MIN_STOP_SEPARATION_METRES = 100;
    private static final double EARTH_RADIUS_METRES = 6_371_000;

    private final RouteRepository routeRepository;
    private final SchoolStatusRepository schoolStatusRepository;
    private final MongoTemplate mongo;
    private final RouteMapper routeMapper;
    private final RabbitEventPublisher publisher;
    private final RoutingClient routingClient;

    public RouteService(
            RouteRepository routeRepository,
            SchoolStatusRepository schoolStatusRepository,
            MongoTemplate mongo,
            RouteMapper routeMapper,
            RabbitEventPublisher publisher,
            RoutingClient routingClient) {
        this.routeRepository = routeRepository;
        this.schoolStatusRepository = schoolStatusRepository;
        this.mongo = mongo;
        this.routeMapper = routeMapper;
        this.publisher = publisher;
        this.routingClient = routingClient;
    }

    public RouteResponse create(UUID schoolId, CreateRouteRequest request) {
        requireApprovedSchool(schoolId);

        String code = Route.normalizeCode(request.code());
        if (routeRepository.existsBySchoolIdAndCode(schoolId, code)) {
            throw new AppException.ConflictException(ResponseMessages.CODE_EXISTS);
        }

        Route route = routeRepository.save(new Route(schoolId, code, request.name()));

        publisher.publish(
                MessagingConstants.ROUTE_CREATED,
                new RouteCreated(route.getId(), route.getSchoolId(), route.getCode(), route.getName(), Instant.now()));

        return routeMapper.toResponse(route);
    }

    public PagedResult<RouteResponse> list(
            UUID schoolId, String search, boolean includeInactive, int page, int pageSize) {

        int safePage = Math.max(page, 1);
        int safeSize = Math.clamp(pageSize, 1, MAX_PAGE_SIZE);

        Criteria criteria = Criteria.where("schoolId").is(schoolId);
        if (!includeInactive) {
            criteria = criteria.and("status").is(RouteStatus.ACTIVE);
        }
        if (search != null && !search.isBlank()) {
            String quoted = Pattern.quote(search.trim());
            criteria = criteria.orOperator(
                    Criteria.where("code").regex(quoted, "i"),
                    Criteria.where("name").regex(quoted, "i"));
        }

        Query query = new Query(criteria);
        long total = mongo.count(query, Route.class);

        query.with(Sort.by(Sort.Direction.ASC, "code"))
                .skip((long) (safePage - 1) * safeSize)
                .limit(safeSize);

        List<RouteResponse> items = mongo.find(query, Route.class).stream()
                .map(routeMapper::toResponse)
                .toList();

        return new PagedResult<>(items, total, safePage, safeSize);
    }

    public RouteResponse getById(UUID schoolId, UUID id) {
        return routeMapper.toResponse(findOwned(schoolId, id));
    }

    public RouteResponse update(UUID schoolId, UUID id, UpdateRouteRequest request) {
        Route route = findOwned(schoolId, id);

        String code = Route.normalizeCode(request.code());
        if (routeRepository.existsBySchoolIdAndCodeAndIdNot(schoolId, code, id)) {
            throw new AppException.ConflictException(ResponseMessages.CODE_EXISTS);
        }

        route.update(code, request.name());
        return routeMapper.toResponse(routeRepository.save(route));
    }

    public void deactivate(UUID schoolId, UUID id) {
        Route route = findOwned(schoolId, id);
        if (route.isActive()) {
            route.deactivate();
            routeRepository.save(route);
        }
    }

    Route findOwned(UUID schoolId, UUID id) {
        return routeRepository
                .findByIdAndSchoolId(id, schoolId)
                .orElseThrow(() -> new AppException.NotFoundException(ResponseMessages.ROUTE_NOT_FOUND));
    }

    private void requireApprovedSchool(UUID schoolId) {
        if (!schoolStatusRepository.existsBySchoolIdAndStatus(schoolId, SchoolStatuses.APPROVED)) {
            throw new AppException.ForbiddenException(ResponseMessages.SCHOOL_NOT_APPROVED);
        }
    }

    public RouteResponse replaceStops(UUID schoolId, UUID routeId, ReplaceStopsRequest request) {
        Route route = findOwned(schoolId, routeId);

        Set<UUID> existingIds = route.getStops().stream().map(Stop::getStopId).collect(Collectors.toSet());

        Set<UUID> seen = new HashSet<>();
        List<Stop> stops = new ArrayList<>();

        int sequence = 1;
        String previousTime = null;
        for (StopInput input : request.stops()) {
            requireInsideIndia(input.latitude(), input.longitude(), "Stop '" + input.name() + "'");
            if (previousTime != null && input.pickupTime().compareTo(previousTime) <= 0) {
                throw new AppException.ValidationException("Stop '" + input.name() + "' is scheduled at "
                        + input.pickupTime() + ", which is not after the previous stop at " + previousTime + ".");
            }
            previousTime = input.pickupTime();
            UUID stopId = (input.stopId() != null && existingIds.contains(input.stopId()))
                    ? input.stopId()
                    : UUID.randomUUID();

            if (!seen.add(stopId)) {
                throw new AppException.ConflictException(ResponseMessages.DUPLICATE_STOP_ID);
            }

            stops.add(new Stop(
                    stopId, sequence++, input.name().trim(), input.latitude(), input.longitude(), input.pickupTime()));
        }

        requireStopsFarEnoughApart(request.stops());

        route.replaceStops(stops);
        return routeMapper.toResponse(routeRepository.save(route));
    }

    public RouteResponse generatePath(UUID schoolId, UUID routeId) {
        Route route = findOwned(schoolId, routeId);

        List<Stop> stops = route.getStops();
        if (stops.size() < 2) {
            throw new AppException.ValidationException(ResponseMessages.PATH_NEEDS_TWO_STOPS);
        }

        List<Point> waypoints = stops.stream()
                .map(s -> new Point(s.getLocation().getX(), s.getLocation().getY()))
                .toList();

        route.replacePath(new GeoJsonLineString(routingClient.roadPathThrough(waypoints)));
        return routeMapper.toResponse(routeRepository.save(route));
    }

    public RouteResponse replacePath(UUID schoolId, UUID routeId, ReplacePathRequest request) {
        Route route = findOwned(schoolId, routeId);

        List<Point> points = new ArrayList<>();
        int index = 1;
        for (GeoPointDto p : request.points()) {
            requireInsideIndia(p.latitude(), p.longitude(), "Path point " + index++);
            points.add(new Point(p.longitude(), p.latitude())); // Point(x = lng, y = lat)
        }

        route.replacePath(new GeoJsonLineString(points));
        return routeMapper.toResponse(routeRepository.save(route));
    }

    public RouteResponse assignBus(UUID schoolId, UUID routeId, AssignBusRequest request) {
        requireApprovedSchool(schoolId);
        Route route = findOwned(schoolId, routeId);

        route.assignBus(request.busId());
        Route saved = routeRepository.save(route);

        publisher.publish(
                MessagingConstants.ROUTE_BUS_ASSIGNED,
                new RouteBusAssigned(route.getId(), schoolId, request.busId(), Instant.now()));

        return routeMapper.toResponse(saved);
    }

    private void requireInsideIndia(double latitude, double longitude, String label) {
        if (latitude < MIN_LAT || latitude > MAX_LAT || longitude < MIN_LNG || longitude > MAX_LNG) {
            throw new AppException.ValidationException(
                    label + " is outside India — check that latitude and longitude are not swapped.");
        }
    }

    private void requireStopsFarEnoughApart(List<StopInput> inputs) {
        for (int i = 0; i < inputs.size(); i++) {
            for (int j = i + 1; j < inputs.size(); j++) {
                double metres = metresBetween(
                        inputs.get(i).latitude(),
                        inputs.get(i).longitude(),
                        inputs.get(j).latitude(),
                        inputs.get(j).longitude());

                if (metres < MIN_STOP_SEPARATION_METRES) {
                    throw new AppException.ValidationException(
                            "Stops '" + inputs.get(i).name() + "' and '"
                                    + inputs.get(j).name() + "' are only " + Math.round(metres)
                                    + " m apart. The bus counts as arrived within " + (int) MIN_STOP_SEPARATION_METRES
                                    + " m of a stop, so it could not tell them apart.");
                }
            }
        }
    }

    private static double metresBetween(double lat1, double lon1, double lat2, double lon2) {
        double dLat = Math.toRadians(lat2 - lat1);
        double dLon = Math.toRadians(lon2 - lon1);
        double h = Math.pow(Math.sin(dLat / 2), 2)
                + Math.pow(Math.sin(dLon / 2), 2) * Math.cos(Math.toRadians(lat1)) * Math.cos(Math.toRadians(lat2));
        return 2 * EARTH_RADIUS_METRES * Math.asin(Math.sqrt(h));
    }
}
