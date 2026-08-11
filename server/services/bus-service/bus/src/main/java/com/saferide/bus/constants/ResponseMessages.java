package com.saferide.bus.constants;

public final class ResponseMessages {

    public static final String BUS_CREATED = "Bus created successfully.";
    public static final String BUS_UPDATED = "Bus updated successfully.";
    public static final String BUS_DEACTIVATED = "Bus deactivated successfully.";
    public static final String DRIVER_ASSIGNED = "Driver assigned successfully.";

    public static final String BUS_NOT_FOUND = "Bus not found.";
    public static final String REGISTRATION_EXISTS = "A bus with this registration number already exists.";
    public static final String SCHOOL_NOT_APPROVED = "This school is not approved to manage buses.";
    public static final String MISSING_SCHOOL_CLAIM = "Token does not carry a school identifier.";

    private ResponseMessages() {}
}
