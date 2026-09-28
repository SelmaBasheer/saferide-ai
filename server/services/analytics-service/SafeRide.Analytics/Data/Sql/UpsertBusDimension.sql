-- The ordering guard lives in the WHERE clause rather than in C#, so a read and
-- a write cannot be separated by another message arriving between them. A stale
-- event simply updates nothing.
--
-- No inbox table: this is an upsert, so replaying the same message converges on
-- the same row. Idempotency comes from the shape of the write.
UPDATE dim_bus
SET school_id           = @SchoolId,
    registration_number = @RegistrationNumber,
    capacity            = COALESCE(@Capacity, capacity),
    is_deleted          = @IsDeleted,
    event_at_utc        = @EventAtUtc,
    updated_at_utc      = SYSUTCDATETIME()
WHERE bus_id = @BusId AND event_at_utc < @EventAtUtc;

IF @@ROWCOUNT = 0 AND NOT EXISTS (SELECT 1 FROM dim_bus WHERE bus_id = @BusId)
    INSERT INTO dim_bus
        (bus_id, school_id, registration_number, capacity,
         is_deleted, event_at_utc, updated_at_utc)
    VALUES
        (@BusId, @SchoolId, @RegistrationNumber, @Capacity,
         @IsDeleted, @EventAtUtc, SYSUTCDATETIME());
