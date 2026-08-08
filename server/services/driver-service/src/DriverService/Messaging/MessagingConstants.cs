namespace DriverService.Messaging;

public static class MessagingConstants
{
    // own exchange (publisher side)
    public const string DriverEventsExchange = "driver.events";
    public const string DriverCreatedKey = "driver-created";

    // consumed from School service
    public const string SchoolEventsExchange = "school.events";
    public const string SchoolEventsQueue = "driver.school-events";
    public const string SchoolApprovedKey = "school-approved";
    public const string SchoolSuspendedKey = "school-suspended";

    // projection vocabulary
    public const string StatusApproved = "Approved";
    public const string StatusSuspended = "Suspended";
}
