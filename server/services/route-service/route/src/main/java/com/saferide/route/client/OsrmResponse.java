package com.saferide.route.client;

import java.util.List;

/** Subset of the OSRM /route response we care about. Unknown fields are ignored. */
public record OsrmResponse(String code, List<OsrmRoute> routes) {

    public record OsrmRoute(OsrmGeometry geometry, double distance, double duration) {}

    public record OsrmGeometry(String type, List<List<Double>> coordinates) {}
}
