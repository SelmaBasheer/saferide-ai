namespace SafeRide.Identity.Application.Common;

public static class MessagingConstants
{
    // exchanges
    public const string IdentityEventsExchange = "identity.events";
    public const string SchoolEventsExchange = "school.events";
    public const string DriverEventsExchange = "driver.events";
    public const string StudentEventsExchange = "student.events";

    // published routing keys
    public const string OtpEmailRequestedKey = "otp-email-requested";
    public const string SchoolAdminRegisteredKey = "school-admin-registered";

    // consumed routing keys
    public const string SchoolCreatedKey = "school-created";
    public const string SchoolApprovedKey = "school-approved";
    public const string SchoolSuspendedKey = "school-suspended";
    public const string DriverCreatedKey = "driver-created";
    public const string StudentCreatedKey = "student-created";

    // queues owned by this service
    public const string SchoolEventsQueue = "identity.school-events";
    public const string DriverEventsQueue = "identity.driver-events";
    public const string StudentEventsQueue = "identity.student-events";
}
