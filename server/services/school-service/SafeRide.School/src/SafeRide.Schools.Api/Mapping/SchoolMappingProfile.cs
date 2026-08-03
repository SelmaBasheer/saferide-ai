using AutoMapper;
using SafeRide.Schools.Api.Contracts;
using SafeRide.Schools.Application.Schools.Command;
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

        CreateMap<SchoolDocument, SchoolDocumentDto>()
            .ForCtorParam("Type", o => o.MapFrom(d => d.Type.ToString()));

        CreateMap<School, SchoolDetailResponse>()
            .ForCtorParam("Board", o => o.MapFrom(s => s.Board == null ? null : s.Board.ToString()))
            .ForCtorParam(
                "BusCount",
                o => o.MapFrom(s => s.BusCount == null ? null : s.BusCount.ToString())
            )
            .ForCtorParam(
                "StudentCount",
                o => o.MapFrom(s => s.StudentCount == null ? null : s.StudentCount.ToString())
            )
            .ForCtorParam("Status", o => o.MapFrom(s => s.Status.ToString()))
            .ForCtorParam("MissingRequirements", o => o.MapFrom(s => s.GetMissingRequirements()));

        CreateMap<UpdateProfileRequest, UpdateSchoolProfileCommand>();
    }
}
