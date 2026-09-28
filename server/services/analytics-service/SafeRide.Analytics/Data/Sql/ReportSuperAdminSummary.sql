SELECT
    (SELECT COUNT(*) FROM dim_school WHERE is_deleted = 0)                        AS TotalSchools,
    (SELECT COUNT(*) FROM dim_school WHERE is_deleted = 0 AND status = 'Approved')  AS ApprovedSchools,
    (SELECT COUNT(*) FROM dim_school WHERE is_deleted = 0 AND status = 'Suspended') AS SuspendedSchools,
    (SELECT ISNULL(SUM(amount_paise), 0) FROM fact_payment
        WHERE status = 'Captured' AND payment_date BETWEEN @From AND @To)         AS RevenuePaise,
    (SELECT COUNT(*) FROM fact_payment
        WHERE payment_date BETWEEN @From AND @To)                                 AS PaymentCount,
    (SELECT COUNT(*) FROM fact_payment
        WHERE status <> 'Captured' AND payment_date BETWEEN @From AND @To)        AS FailedPaymentCount;
