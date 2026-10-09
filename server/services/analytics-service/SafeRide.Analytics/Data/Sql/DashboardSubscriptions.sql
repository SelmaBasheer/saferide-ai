-- Derived from the end date, not the stored status. A subscription that expired
-- this morning still reads Active until the expiry job runs, so the date is the
-- only thing that cannot be stale.
--
-- ExpiringSoon is deliberately a subset of Active, not a separate state — a
-- school expiring in a fortnight is still live, and showing it twice is the
-- point: one number says "working", the other says "needs attention".
SELECT
    ISNULL(SUM(CASE
        WHEN subscription_status <> 'Cancelled' AND subscription_ends_on >= @Today
        THEN 1 ELSE 0 END), 0) AS Active,

    ISNULL(SUM(CASE
        WHEN subscription_status <> 'Cancelled'
         AND subscription_ends_on >= @Today
         AND subscription_ends_on <= @SoonCutoff
        THEN 1 ELSE 0 END), 0) AS ExpiringSoon,

    ISNULL(SUM(CASE
        WHEN subscription_status <> 'Cancelled'
         AND subscription_ends_on < @Today
         AND subscription_ends_on >= @GraceCutoff
        THEN 1 ELSE 0 END), 0) AS InGrace,

    ISNULL(SUM(CASE
        WHEN subscription_status <> 'Cancelled'
         AND subscription_ends_on < @GraceCutoff
        THEN 1 ELSE 0 END), 0) AS Expired,

    ISNULL(SUM(CASE
        WHEN subscription_status = 'Cancelled'
        THEN 1 ELSE 0 END), 0) AS Cancelled
FROM dim_school
WHERE is_deleted = 0 AND subscription_ends_on IS NOT NULL;
