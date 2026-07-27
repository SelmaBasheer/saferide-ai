using AutoMapper;
using SafeRide.Identity.Api.Contracts;
using SafeRide.Identity.Application.Auth.Login;
using SafeRide.Identity.Application.Auth.Refresh;

namespace SafeRide.Identity.Api.Mapping;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        // AccessToken + RefreshToken match by name; ExpiresIn needs a rule.
        CreateMap<LoginResult, AuthResponse>()
            .ForCtorParam("ExpiresIn", o => o.MapFrom(s => s.AccessTokenExpiresAtUtc));

        CreateMap<RefreshTokenResult, AuthResponse>()
            .ForCtorParam("ExpiresIn", o => o.MapFrom(s => s.AccessTokenExpiresAtUtc));
    }
}
