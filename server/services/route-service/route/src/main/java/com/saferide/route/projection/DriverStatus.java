package com.saferide.route.projection;

import java.time.Instant;
import java.util.UUID;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;

/** Just enough of a driver to answer "which driver record is this login?". */
@Document(collection = "driver_status")
public class DriverStatus {

    @Id
    private UUID driverId;

    private UUID schoolId;
    private String email; // stored lower-case; logins are not case sensitive
    private Instant updatedAt;
    private Instant eventAtUtc;

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
}
