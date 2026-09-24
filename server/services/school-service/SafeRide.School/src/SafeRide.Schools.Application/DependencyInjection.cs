using Microsoft.Extensions.DependencyInjection;
using SafeRide.Schools.Application.Plans.Command;
using SafeRide.Schools.Application.Plans.Query;
using SafeRide.Schools.Application.Schools.Command;
using SafeRide.Schools.Application.Schools.Query;
using SafeRide.Schools.Application.Subscriptions.Command;
using SafeRide.Schools.Application.Subscriptions.Query;

namespace SafeRide.Schools.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ApproveSchoolHandler>();
        services.AddScoped<GetSchoolsHandler>();
        services.AddScoped<GetMySchoolHandler>();
        services.AddScoped<UpdateSchoolProfileHandler>();
        services.AddScoped<UploadSchoolDocumentHandler>();
        services.AddScoped<SubmitSchoolHandler>();
        services.AddScoped<GetSchoolByIdHandler>();
        services.AddScoped<GetDocumentDownloadUrlHandler>();
        services.AddScoped<CreatePlanHandler>();
        services.AddScoped<GetPlansHandler>();
        services.AddScoped<ActivateSubscriptionHandler>();
        services.AddScoped<GetSubscriptionsHandler>();
        services.AddScoped<ExpireSubscriptionsHandler>();

        return services;
    }
}
