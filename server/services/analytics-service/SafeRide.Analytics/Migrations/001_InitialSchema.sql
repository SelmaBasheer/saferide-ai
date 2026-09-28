CREATE TABLE dim_school (
    school_id                UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    name                     NVARCHAR(200)    NOT NULL,
    city                     NVARCHAR(100)    NULL,
    status                   NVARCHAR(40)     NOT NULL,
    plan_name                NVARCHAR(100)    NULL,
    subscription_status      NVARCHAR(40)     NULL,
    subscription_ends_on     DATE             NULL,
    bus_limit                INT              NULL,
    is_deleted               BIT              NOT NULL DEFAULT 0,
    -- Two timestamps, not one. School status and subscription changes arrive on
    -- independent streams, so a stale message on one must not roll back the
    -- other. Same reasoning as SchoolStatus in the Bus service.
    school_event_at_utc      DATETIME2        NULL,
    entitlement_event_at_utc DATETIME2        NULL,
    updated_at_utc           DATETIME2        NOT NULL
);

CREATE TABLE dim_bus (
    bus_id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    school_id           UNIQUEIDENTIFIER NOT NULL,
    registration_number NVARCHAR(50)     NOT NULL,
    capacity            INT              NULL,
    -- Soft delete only. A removed bus still has trips in September, and
    -- deleting the row would rewrite that history.
    is_deleted          BIT              NOT NULL DEFAULT 0,
    event_at_utc        DATETIME2        NOT NULL,
    updated_at_utc      DATETIME2        NOT NULL
);

CREATE TABLE fact_trip (
    trip_id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    school_id      UNIQUEIDENTIFIER NOT NULL,
    route_id       UNIQUEIDENTIFIER NOT NULL,
    bus_id         UNIQUEIDENTIFIER NOT NULL,
    driver_id      UNIQUEIDENTIFIER NOT NULL,
    -- Snapshots. Renaming a route in October must not rewrite September.
    route_code     NVARCHAR(50)     NULL,
    route_name     NVARCHAR(200)    NULL,
    driver_name    NVARCHAR(200)    NULL,
    -- Local date, not UTC. Range filters are asked in the school's own
    -- calendar; UTC would misplace early-morning trips.
    trip_date      DATE             NOT NULL,
    started_at_utc DATETIME2        NOT NULL,
    ended_at_utc   DATETIME2        NULL,
    student_count  INT              NOT NULL DEFAULT 0,
    boarded_count  INT              NOT NULL DEFAULT 0,
    absent_count   INT              NOT NULL DEFAULT 0,
    unmarked_count INT              NOT NULL DEFAULT 0
);

CREATE TABLE fact_attendance (
    trip_id       UNIQUEIDENTIFIER NOT NULL,
    student_id    UNIQUEIDENTIFIER NOT NULL,
    school_id     UNIQUEIDENTIFIER NOT NULL,
    trip_date     DATE             NOT NULL,
    -- Snapshotted from the trip roster, which already snapshots at trip start.
    -- There is deliberately no dim_student: a child who changes route in
    -- October must not appear on the new route in September's report.
    student_name  NVARCHAR(200)    NOT NULL,
    stop_id       UNIQUEIDENTIFIER NULL,
    stop_name     NVARCHAR(200)    NULL,
    route_code    NVARCHAR(50)     NULL,
    route_name    NVARCHAR(200)    NULL,
    bus_id        UNIQUEIDENTIFIER NOT NULL,
    status        NVARCHAR(20)     NOT NULL,
    marked_at_utc DATETIME2        NULL,
    CONSTRAINT pk_fact_attendance PRIMARY KEY (trip_id, student_id)
);

CREATE TABLE fact_payment (
    payment_id      UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    school_id       UNIQUEIDENTIFIER NOT NULL,
    subscription_id UNIQUEIDENTIFIER NULL,
    plan_name       NVARCHAR(100)    NULL,
    -- Integer paise, never a decimal. Formatted to rupees at render only.
    amount_paise    BIGINT           NOT NULL,
    status          NVARCHAR(40)     NOT NULL,
    captured_at_utc DATETIME2        NOT NULL,
    payment_date    DATE             NOT NULL
);

-- At-least-once delivery means every message can arrive twice. Facts are
-- inserted rather than upserted, so they need an explicit guard.
CREATE TABLE inbox (
    event_id        NVARCHAR(100) NOT NULL PRIMARY KEY,
    event_type      NVARCHAR(100) NOT NULL,
    received_at_utc DATETIME2     NOT NULL
);

-- Every report filters by school and date range. Without these the reports
-- table-scan, which is the exact thing an analytics store exists to avoid.
CREATE INDEX ix_fact_trip_school_date       ON fact_trip       (school_id, trip_date);
CREATE INDEX ix_fact_attendance_school_date ON fact_attendance (school_id, trip_date);
CREATE INDEX ix_fact_payment_school_date    ON fact_payment    (school_id, payment_date);
CREATE INDEX ix_dim_bus_school              ON dim_bus         (school_id);
