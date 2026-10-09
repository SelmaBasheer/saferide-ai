-- Grouped on the first of the month as a real date rather than a formatted
-- string, so it sorts correctly and the client decides how to label it.
--
-- Driven by the selected range now, not a hardcoded window — picking a quarter
-- should give three bars, not twelve with nine of them empty.
SELECT
    DATEFROMPARTS(YEAR(payment_date), MONTH(payment_date), 1) AS Month,
    SUM(amount_paise)                                          AS RevenuePaise,
    COUNT(*)                                                   AS PaymentCount
FROM fact_payment
WHERE status = 'Captured'
  AND payment_date BETWEEN @From AND @To
GROUP BY DATEFROMPARTS(YEAR(payment_date), MONTH(payment_date), 1)
ORDER BY 1;
