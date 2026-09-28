SELECT
    p.payment_date AS PaymentDate,
    -- LEFT JOIN, not INNER. A payment for a school this projection has never
    -- seen is exactly the one somebody needs to find — dropping it would hide
    -- money we took and cannot account for.
    COALESCE(s.name, N'(unknown school)') AS SchoolName,
    COALESCE(p.plan_name, N'(no plan)')   AS PlanName,
    p.amount_paise AS AmountPaise,
    p.status       AS Status
FROM fact_payment p
LEFT JOIN dim_school s ON s.school_id = p.school_id
WHERE p.payment_date BETWEEN @From AND @To
ORDER BY p.payment_date DESC, s.name;
