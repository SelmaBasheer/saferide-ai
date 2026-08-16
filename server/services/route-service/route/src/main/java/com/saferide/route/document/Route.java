package com.saferide.route.document;

import java.time.Instant;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import lombok.Getter;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.geo.GeoJsonLineString;
import org.springframework.data.mongodb.core.index.CompoundIndex;
import org.springframework.data.mongodb.core.mapping.Document;

@Getter
@Document(collection = "routes")
@CompoundIndex(name = "uk_route_school_code", def = "{'schoolId': 1, 'code': 1}", unique = true)
public class Route {

    @Id
    private UUID id;

    private UUID schoolId;
    private String code;
    private String name;
    private RouteStatus status;
    private UUID assignedBusId;

    private List<Stop> stops = new ArrayList<>();

    private GeoJsonLineString path; // GeoJSON LineString

    private Instant createdAt;
    private Instant updatedAt;

    protected Route() {}

    public Route(UUID schoolId, String code, String name) {
        this.id = UUID.randomUUID();
        this.schoolId = schoolId;
        this.code = normalizeCode(code);
        this.name = name.trim();
        this.status = RouteStatus.ACTIVE;
        this.stops = new ArrayList<>();
        this.createdAt = Instant.now();
        this.updatedAt = this.createdAt;
    }

    public static String normalizeCode(String code) {
        return code.trim().toUpperCase();
    }

    public void update(String code, String name) {
        this.code = normalizeCode(code);
        this.name = name.trim();
        this.updatedAt = Instant.now();
    }

    public void replaceStops(List<Stop> stops) {
        this.stops = new ArrayList<>(stops);
        this.updatedAt = Instant.now();
    }

    public void replacePath(GeoJsonLineString path) {
        this.path = path;
        this.updatedAt = Instant.now();
    }

    public void assignBus(UUID busId) {
        this.assignedBusId = busId;
        this.updatedAt = Instant.now();
    }

    public void deactivate() {
        this.status = RouteStatus.INACTIVE;
        this.updatedAt = Instant.now();
    }

    public boolean isActive() {
        return status == RouteStatus.ACTIVE;
    }
}
