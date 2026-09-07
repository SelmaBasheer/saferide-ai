package com.saferide.route.client;

import com.saferide.route.constants.ResponseMessages;
import com.saferide.route.exception.AppException;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.geo.Point;
import org.springframework.http.client.SimpleClientHttpRequestFactory;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestClient;

@Component
public class RoutingClient {

    private static final Logger log = LoggerFactory.getLogger(RoutingClient.class);

    private final RestClient client;

    public RoutingClient(
        @Value("${saferide.routing.base-url}") String baseUrl,
        @Value("${saferide.routing.timeout-seconds:10}") int timeoutSeconds) {

        SimpleClientHttpRequestFactory factory = new SimpleClientHttpRequestFactory();
        factory.setConnectTimeout((int) Duration.ofSeconds(timeoutSeconds).toMillis());
        factory.setReadTimeout((int) Duration.ofSeconds(timeoutSeconds).toMillis());

        this.client = RestClient.builder()
            .baseUrl(baseUrl)
            .requestFactory(factory)
            .build();
    }

    /**
     * Asks the routing engine for the road geometry through the given waypoints.
     * Waypoints and the result are both Point(x = longitude, y = latitude).
     */
    public List<Point> roadPathThrough(List<Point> waypoints) {
        String coordinates =
            waypoints.stream().map(p -> p.getX() + "," + p.getY()).collect(Collectors.joining(";"));

        String uri = "/route/v1/driving/" + coordinates + "?overview=full&geometries=geojson";

        OsrmResponse response;
        try {
            response = client.get().uri(uri).retrieve().body(OsrmResponse.class);
        } catch (Exception ex) {
            log.warn("Routing engine unreachable", ex);
            throw new AppException.UpstreamException(ResponseMessages.ROUTING_UNAVAILABLE);
        }

        if (response == null
            || !"Ok".equalsIgnoreCase(response.code())
            || response.routes() == null
            || response.routes().isEmpty()) {
            throw new AppException.ValidationException(ResponseMessages.NO_ROAD_ROUTE);
        }

        List<List<Double>> coords = response.routes().get(0).geometry().coordinates();

        List<Point> points = new ArrayList<>();
        for (List<Double> c : coords) {
            Point point = new Point(c.get(0), c.get(1)); // [lng, lat]
            if (points.isEmpty() || !points.get(points.size() - 1).equals(point)) {
                points.add(point);
            }
        }
        return points;    }
}
