-- Keyed by (trip_id, student_id), which is the natural key: a student appears
-- once per trip. That makes redelivery harmless on its own.
IF NOT EXISTS (
    SELECT 1 FROM fact_attendance WHERE trip_id = @TripId AND student_id = @StudentId
)
    INSERT INTO fact_attendance
        (trip_id, student_id, school_id, trip_date,
         student_name, stop_id, stop_name, route_code, route_name,
         bus_id, status, marked_at_utc)
    VALUES
        (@TripId, @StudentId, @SchoolId, @TripDate,
         @StudentName, @StopId, @StopName, @RouteCode, @RouteName,
         @BusId, @Status, @MarkedAtUtc);
