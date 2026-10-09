-- Captured only. A failed attempt is not revenue, and counting it would make
-- the headline number flattering and wrong.
--
-- Total, this month and this year are fixed by definition and ignore the
-- selected range. The range figures sit alongside them rather than replacing
-- them, so "how are we doing overall" and "how did that period go" are both
-- answerable without changing the filter.
SELECT
    ISNULL(SUM(amount_paise), 0)                                                       AS TotalPaise,
    ISNULL(SUM(CASE WHEN payment_date >= @MonthStart THEN amount_paise ELSE 0 END), 0) AS ThisMonthPaise,
    ISNULL(SUM(CASE WHEN payment_date >= @YearStart  THEN amount_paise ELSE 0 END), 0) AS ThisYearPaise,
    COUNT(*)                                                                            AS PaymentCount,

    ISNULL(SUM(CASE WHEN payment_date BETWEEN @From AND @To
                    THEN amount_paise ELSE 0 END), 0)                                   AS InRangePaise,
    SUM(CASE WHEN payment_date BETWEEN @From AND @To THEN 1 ELSE 0 END)                 AS InRangeCount
FROM fact_payment
WHERE status = 'Captured';
