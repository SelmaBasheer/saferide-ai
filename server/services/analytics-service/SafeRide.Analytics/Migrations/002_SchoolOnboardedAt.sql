-- When a school first became live. Deliberately separate from
-- school_event_at_utc, which moves every time the status changes: a school
-- suspended today would otherwise report as having onboarded today.
ALTER TABLE dim_school ADD onboarded_at_utc DATETIME2 NULL;
GO

-- Existing rows: the best available guess is their last status change, which
-- for a school that was never suspended is exactly right.
UPDATE dim_school
SET onboarded_at_utc = school_event_at_utc
WHERE onboarded_at_utc IS NULL AND status = 'Approved';
