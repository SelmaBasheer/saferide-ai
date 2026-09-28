SELECT
    COALESCE(plan_name, N'(no plan)') AS PlanName,
    COUNT(*)          AS PaymentCount,
    SUM(amount_paise) AS RevenuePaise
FROM fact_payment
WHERE status = 'Captured' AND payment_date BETWEEN @From AND @To
GROUP BY plan_name
ORDER BY SUM(amount_paise) DESC;
