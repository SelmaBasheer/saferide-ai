using AutoMapper;
using DriverService.Domain;
using DriverService.Features.CreateDriver;
using DriverService.Features.GetDrivers;

namespace DriverService.Mapping;

public class DriverMappingProfile : Profile
{
    public DriverMappingProfile()
    {
        CreateMap<Driver, CreateDriverResponse>()
            .ForCtorParam("Status", o => o.MapFrom(d => d.Status.ToString()));

        CreateMap<Driver, DriverListItem>()
            .ForCtorParam("Status", o => o.MapFrom(d => d.Status.ToString()));
    }
}
