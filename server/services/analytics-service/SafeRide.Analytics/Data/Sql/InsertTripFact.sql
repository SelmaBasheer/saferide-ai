-- Facts are inserted, never updated: a completed trip cannot un-complete.
-- Idempotency comes from the trip's own id, so a redelivered message is a
-- no-op without needing an inbox table.
IF NOT EXISTS (SELECT 1 FROM fact_trip WHERE trip_id = @TripId)
    INSERT INTO fact_trip
        (trip_id, school_id, route_id, bus_id, driver_id,
         route_code, route_name, driver_name, trip_date,
         started_at_utc, ended_at_utc,
         student_count, boarded_count, absent_count, unmarked_count)
    VALUES
        (@TripId, @SchoolId, @RouteId, @BusId, @DriverId,
         @RouteCode, @RouteName, @DriverName, @TripDate,
         @StartedAtUtc, @EndedAtUtc,
         @StudentCount, @BoardedCount, @AbsentCount, @UnmarkedCount);
