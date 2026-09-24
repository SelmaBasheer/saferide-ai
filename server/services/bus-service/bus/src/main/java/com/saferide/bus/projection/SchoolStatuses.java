package com.saferide.bus.projection;

public final class SchoolStatuses {
    public static final String APPROVED = "Approved";
    public static final String SUSPENDED = "Suspended";
    /**
     * Used when a subscription event arrives before any status event has, so
     * the row has to be created without knowing the school's real status.
     * Every "may this school operate" check compares against APPROVED, so an
     * unknown school is refused rather than waved through.
     */
    public static final String UNKNOWN = "Unknown";

    private SchoolStatuses() {}
}
