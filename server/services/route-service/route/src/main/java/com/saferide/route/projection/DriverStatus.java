package com.saferide.route.projection;

import java.time.Instant;
import java.util.UUID;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;

/** Just enough of a driver to answer "which driver record is this login, and may they still drive?". */
@Document(collection = "driver_status")
public class DriverStatus {

    @Id
    private UUID driverId;

    private UUID schoolId;
    private String email; // stored lower-case; logins are not case sensitive
    private Instant updatedAt;
    private Instant eventAtUtc;

    /** "Active" or "Inactive". Null on rows projected before this field existed. */
    private String status;

    // Employment status arrives on its own event stream, independent of the
    // driver's details, so it needs its own stale-guard. The same reasoning as
    // BusStatus.driverEventAtUtc.
    private Instant statusEventAtUtc;

    protected DriverStatus() {}

    public UUID getDriverId() {
        return driverId;
    }

    public UUID getSchoolId() {
        return schoolId;
    }

    public String getEmail() {
        return email;
    }

    public Instant getEventAtUtc() {
        return eventAtUtc;
    }

    public String getStatus() {
        return status;
    }

    public Instant getStatusEventAtUtc() {
        return statusEventAtUtc;
    }

    /**
     * Null means this row predates status projection — it was written before
     * driver-service published status events at all. Treating unknown as
     * "allowed" keeps existing drivers working; a real deactivation always
     * arrives as an event and sets the field.
     */
    public boolean isEmployed() {
        return status == null || !"Inactive".equalsIgnoreCase(status);
    }
}
