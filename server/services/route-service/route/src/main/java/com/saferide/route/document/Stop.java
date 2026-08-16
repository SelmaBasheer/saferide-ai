package com.saferide.route.document;

import java.util.UUID;
import lombok.Getter;
import org.springframework.data.mongodb.core.geo.GeoJsonPoint;
import org.springframework.data.mongodb.core.index.GeoSpatialIndexType;
import org.springframework.data.mongodb.core.index.GeoSpatialIndexed;

@Getter
public class Stop {

    private UUID stopId;
    private int sequence;
    private String name;

    @GeoSpatialIndexed(type = GeoSpatialIndexType.GEO_2DSPHERE)
    private GeoJsonPoint location;

    private String pickupTime; // "HH:mm"

    protected Stop() {}

    public Stop(UUID stopId, int sequence, String name, double latitude, double longitude, String pickupTime) {
        this.stopId = stopId;
        this.sequence = sequence;
        this.name = name;
        this.location = new GeoJsonPoint(longitude, latitude); // GeoJSON is [lng, lat]
        this.pickupTime = pickupTime;
    }

    public double getLatitude() {
        return location.getY();
    }

    public double getLongitude() {
        return location.getX();
    }
}
