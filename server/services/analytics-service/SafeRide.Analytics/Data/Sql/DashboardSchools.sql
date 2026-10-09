-- Only schools that reached approval appear in dim_school — nothing publishes an
-- event for a school still being reviewed. So this is the onboarded population,
-- and the dashboard gets its pending count from the School service instead.
--
-- Two sets of numbers from one pass:
--   * the unprefixed columns describe the platform as it stands today
--   * the InRange* columns describe schools that came on board inside @From..@To,
--     counted by the status they hold now
-- The second set is what a date range can honestly answer about schools:
-- onboarding is an event, status is a current fact.
SELECT
    COUNT(*)                                                           AS Onboarded,
    ISNULL(SUM(CASE WHEN status = 'Approved'  THEN 1 ELSE 0 END), 0)    AS Approved,
    ISNULL(SUM(CASE WHEN status = 'Suspended' THEN 1 ELSE 0 END), 0)    AS Suspended,

    -- Schools that paid once and have since lapsed past grace. The number that
    -- matters commercially, and it is not the same as "suspended".
    ISNULL(SUM(CASE
        WHEN subscription_ends_on IS NOT NULL
         AND subscription_status <> 'Cancelled'
         AND subscription_ends_on < @GraceCutoff
        THEN 1 ELSE 0 END), 0)                                          AS WithExpiredSubscription,

    -- onboarded_at_utc is UTC; the range is in local dates, so shift by the IST
    -- offset before taking the date part. Without this, a school onboarded at
    -- 02:00 IST lands on the previous day and falls outside a range that should
    -- include it — the same boundary bug we have already fixed four times.
    ISNULL(SUM(CASE
        WHEN CAST(DATEADD(minute, 330, onboarded_at_utc) AS date) BETWEEN @From AND @To
        THEN 1 ELSE 0 END), 0)                                          AS InRangeOnboarded,

    ISNULL(SUM(CASE
        WHEN CAST(DATEADD(minute, 330, onboarded_at_utc) AS date) BETWEEN @From AND @To
         AND status = 'Approved'
        THEN 1 ELSE 0 END), 0)                                          AS InRangeApproved,

    ISNULL(SUM(CASE
        WHEN CAST(DATEADD(minute, 330, onboarded_at_utc) AS date) BETWEEN @From AND @To
         AND status = 'Suspended'
        THEN 1 ELSE 0 END), 0)                                          AS InRangeSuspended
FROM dim_school
WHERE is_deleted = 0;
