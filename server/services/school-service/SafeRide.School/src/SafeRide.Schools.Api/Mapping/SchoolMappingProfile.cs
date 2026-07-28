using AutoMapper;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Domain.Entities;

namespace SafeRide.Schools.Api.Mapping;

public sealed class SchoolMappingProfile : Profile
{
    public SchoolMappingProfile()
    {
        CreateMap<School, SchoolDto>()
            .ForCtorParam(
                "AdminName",
                o => o.MapFrom(s => s.AdminFirstName + " " + s.AdminLastName)
            )
            .ForCtorParam("Status", o => o.MapFrom(s => s.Status.ToString()));
    }
}
