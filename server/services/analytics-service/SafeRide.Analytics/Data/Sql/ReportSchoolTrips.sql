-- The join to dim_bus is what the dimension exists for: the trip fact stores a
-- bus id, and a report needs the registration plate a human recognises.
SELECT
    t.trip_date      AS TripDate,
    t.route_code     AS RouteCode,
    t.route_name     AS RouteName,
    b.registration_number AS BusRegistration,
    t.started_at_utc AS StartedAtUtc,
    t.ended_at_utc   AS EndedAtUtc,
    t.student_count  AS StudentCount,
    t.boarded_count  AS BoardedCount,
    t.absent_count   AS AbsentCount,
    t.unmarked_count AS UnmarkedCount
FROM fact_trip t
LEFT JOIN dim_bus b ON b.bus_id = t.bus_id
WHERE t.school_id = @SchoolId AND t.trip_date BETWEEN @From AND @To
ORDER BY t.trip_date DESC, t.started_at_utc DESC;
