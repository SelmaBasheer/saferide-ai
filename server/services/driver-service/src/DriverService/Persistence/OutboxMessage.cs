namespace DriverService.Persistence;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!; // routing key, e.g. "driver.created"
    public string Payload { get; set; } = null!; // JSON
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; } // null = pending
}
