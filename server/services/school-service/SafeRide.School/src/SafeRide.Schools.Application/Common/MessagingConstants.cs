namespace SafeRide.Schools.Application.Common;

public static class MessagingConstants
{
    // exchanges
    public const string SchoolEventsExchange = "school.events";
    public const string IdentityEventsExchange = "identity.events";

    // published routing keys
    public const string SchoolCreatedKey = "school-created";
    public const string SchoolSubmittedForApprovalKey = "school-submitted-for-approval";
    public const string SchoolApprovedKey = "school-approved";
    public const string SchoolRejectedKey = "school-rejected";
    public const string SchoolSuspendedKey = "school-suspended";

    // consumed routing keys
    public const string SchoolAdminRegisteredKey = "school-admin-registered";

    // queues owned by this service
    public const string IdentityEventsQueue = "school.identity-events";
}
