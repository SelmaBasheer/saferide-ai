-- One row per student per trip. Names and stops come from the fact row, not a
-- join: they were snapshotted when the trip ended, so a child who moves route
-- in October does not rewrite September.
SELECT
    trip_date     AS TripDate,
    route_code    AS RouteCode,
    route_name    AS RouteName,
    student_name  AS StudentName,
    stop_name     AS StopName,
    status        AS Status,
    marked_at_utc AS MarkedAtUtc
FROM fact_attendance
WHERE school_id = @SchoolId AND trip_date BETWEEN @From AND @To
ORDER BY trip_date DESC, route_code, student_name;
