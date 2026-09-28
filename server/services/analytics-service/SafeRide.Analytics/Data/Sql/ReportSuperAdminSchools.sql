SELECT
    s.name      AS Name,
    s.city      AS City,
    s.status    AS Status,
    s.plan_name AS PlanName,
    -- Derived here rather than trusted from the column. A subscription whose
    -- end date has passed is expired whether or not an event has said so yet,
    -- which is the same rule the School service applies.
    CASE
        WHEN s.subscription_status IS NULL          THEN 'None'
        WHEN s.subscription_status = 'Cancelled'    THEN 'Cancelled'
        WHEN s.subscription_ends_on < @Today        THEN 'Expired'
        ELSE s.subscription_status
    END         AS SubscriptionStatus,
    s.subscription_ends_on AS SubscriptionEndsOn,
    s.bus_limit            AS BusLimit,
    (SELECT COUNT(*) FROM dim_bus b
      WHERE b.school_id = s.school_id AND b.is_deleted = 0) AS BusesInUse
FROM dim_school s
WHERE s.is_deleted = 0
ORDER BY s.name;
