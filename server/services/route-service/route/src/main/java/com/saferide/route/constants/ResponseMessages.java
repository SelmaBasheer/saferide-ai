package com.saferide.route.constants;

public final class ResponseMessages {

    public static final String ROUTE_CREATED = "Route created successfully.";
    public static final String ROUTE_UPDATED = "Route updated successfully.";
    public static final String ROUTE_DEACTIVATED = "Route deactivated successfully.";
    public static final String DRIVER_ASSIGNED = "Driver assigned successfully.";

    public static final String ROUTE_NOT_FOUND = "Route not found.";
    public static final String CODE_EXISTS = "A Route with this code already exists.";
    public static final String SCHOOL_NOT_APPROVED = "This school is not approved to manage buses.";
    public static final String MISSING_SCHOOL_CLAIM = "Token does not carry a school identifier.";

    public static final String STOPS_UPDATED = "Stops updated successfully.";
    public static final String DUPLICATE_STOP_ID = "The same stop appears more than once.";
    public static final String PATH_UPDATED = "Route path updated successfully.";
    public static final String BUS_ASSIGNED = "Bus assigned successfully.";

    private ResponseMessages() {}
}
