-- Name and city are null on school-suspended, so COALESCE must mean
-- "unchanged" rather than "erase".
UPDATE dim_school
SET name                = COALESCE(@Name, name),
    city                = COALESCE(@City, city),
    status              = @Status,
    school_event_at_utc = @EventAtUtc,
    updated_at_utc      = SYSUTCDATETIME()
WHERE school_id = @SchoolId
  AND (school_event_at_utc IS NULL OR school_event_at_utc < @EventAtUtc);

-- A suspension for a school we have never seen still creates a row, with a
-- placeholder name that backfill will replace. Losing the school entirely
-- would be worse: its payments would have nothing to join to.
IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dim_school WHERE school_id = @SchoolId)
    INSERT INTO dim_school
        (school_id, name, city, status, school_event_at_utc, updated_at_utc)
    VALUES
        (@SchoolId, COALESCE(@Name, N'(pending backfill)'), @City, @Status,
         @EventAtUtc, SYSUTCDATETIME());
