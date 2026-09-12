namespace SafeRide.Ai.Domain.Enums;

public enum AnomalyStatus
{
    Detected = 1, // a rule fired
    Classified = 2, // the model (or the fallback) has explained it
    Approved = 3, // an admin sent it
    Dismissed = 4, // an admin judged it not worth sending
}
