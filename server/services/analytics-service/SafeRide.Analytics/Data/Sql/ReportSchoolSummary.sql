SELECT
    (SELECT COUNT(*) FROM fact_trip
      WHERE school_id = @SchoolId AND trip_date BETWEEN @From AND @To)      AS Trips,
    ISNULL(SUM(CASE WHEN status = 'Boarded'  THEN 1 ELSE 0 END), 0)         AS Boarded,
    ISNULL(SUM(CASE WHEN status = 'Absent'   THEN 1 ELSE 0 END), 0)         AS Absent,
    ISNULL(SUM(CASE WHEN status = 'Unmarked' THEN 1 ELSE 0 END), 0)         AS Unmarked
FROM fact_attendance
WHERE school_id = @SchoolId AND trip_date BETWEEN @From AND @To;
