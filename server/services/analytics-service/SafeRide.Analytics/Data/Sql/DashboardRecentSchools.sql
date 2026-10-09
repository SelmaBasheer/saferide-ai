-- Schools that came on board in the range, newest first. Capped because this is
-- a dashboard panel, not the schools page — "show me more" is a link, not a
-- longer list.
SELECT TOP (@Take)
    s.name                   AS Name,
    s.city                   AS City,
    s.plan_name              AS PlanName,
    s.onboarded_at_utc       AS OnboardedAtUtc,
    s.status                 AS Status
FROM dim_school s
WHERE s.is_deleted = 0
  AND s.onboarded_at_utc IS NOT NULL
  AND CAST(s.onboarded_at_utc AS DATE) BETWEEN @From AND @To
ORDER BY s.onboarded_at_utc DESC;
