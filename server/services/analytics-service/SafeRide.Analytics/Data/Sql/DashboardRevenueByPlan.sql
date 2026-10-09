-- Range-driven: "what did schools buy in this period". The all-time view lives
-- in the plan report, which is where someone goes to ask a product question.
-- A donut on a dashboard that ignores the date control above it reads as broken,
-- however defensible the number is.
SELECT
    COALESCE(plan_name, N'(no plan)') AS PlanName,
    COUNT(*)                          AS PaymentCount,
    SUM(amount_paise)                 AS RevenuePaise
FROM fact_payment
WHERE status = 'Captured'
  AND payment_date BETWEEN @From AND @To
GROUP BY plan_name
ORDER BY SUM(amount_paise) DESC;
