-- Facts are inserted, never updated: a payment that happened cannot unhappen.
-- Idempotency comes from the payment's own id, so a redelivered message is a
-- no-op without needing an inbox table.
IF NOT EXISTS (SELECT 1 FROM fact_payment WHERE payment_id = @PaymentId)
    INSERT INTO fact_payment
        (payment_id, school_id, subscription_id, plan_name,
         amount_paise, status, captured_at_utc, payment_date)
    VALUES
        (@PaymentId, @SchoolId, @SubscriptionId, @PlanName,
         @AmountPaise, @Status, @CapturedAtUtc, @PaymentDate);
