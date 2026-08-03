using Microsoft.Extensions.DependencyInjection;
using SafeRide.Schools.Application.Schools.Command;
using SafeRide.Schools.Application.Schools.Query;

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

        return services;
    }
}
