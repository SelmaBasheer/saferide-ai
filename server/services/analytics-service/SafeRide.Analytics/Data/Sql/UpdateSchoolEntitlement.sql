-- Update only. A subscription change for a school Analytics has never heard of
-- cannot invent one — there would be no name and no status to record. Backfill
-- reads the current subscription state anyway, so nothing is lost.
UPDATE dim_school
SET plan_name                = COALESCE(@PlanName, plan_name),
    subscription_status      = @SubscriptionStatus,
    subscription_ends_on     = @SubscriptionEndsOn,
    bus_limit                = @BusLimit,
    entitlement_event_at_utc = @EventAtUtc,
    updated_at_utc           = SYSUTCDATETIME()
WHERE school_id = @SchoolId
  AND (entitlement_event_at_utc IS NULL OR entitlement_event_at_utc < @EventAtUtc);
