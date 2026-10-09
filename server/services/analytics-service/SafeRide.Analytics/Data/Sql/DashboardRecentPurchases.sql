-- LEFT JOIN, not INNER. A payment from a school this projection has never seen
-- is exactly the one somebody needs to find.
SELECT TOP (@Take)
    p.payment_date                        AS PaymentDate,
    COALESCE(s.name, N'(unknown school)') AS SchoolName,
    COALESCE(p.plan_name, N'(no plan)')   AS PlanName,
    p.amount_paise                        AS AmountPaise,
    p.status                              AS Status
FROM fact_payment p
LEFT JOIN dim_school s ON s.school_id = p.school_id
WHERE p.payment_date BETWEEN @From AND @To
ORDER BY p.payment_date DESC, p.captured_at_utc DESC;
