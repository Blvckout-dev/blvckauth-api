using AutoMapper;

namespace Blvckout.BlvckAuth.API.AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Database.Entities.User, DataModels.API.V1.DTOs.Users.UserDto>()
            .ForMember(
                dest => dest.ScopeIds,
                opt => opt.MapFrom(
                    src => src.Scopes != null ?
                    src.Scopes.Select(s => s.Id) :
                    Enumerable.Empty<int>()
                )
            )
            .ReverseMap();
        CreateMap<Database.Entities.User, DataModels.API.V1.DTOs.Users.UserMinimalDto>().ReverseMap();
        CreateMap<Database.Entities.User, DataModels.API.V1.DTOs.Users.UserDetailDto>().ReverseMap();
        CreateMap<Database.Entities.User, DataModels.API.V1.DTOs.Users.UserCreateDto>().ReverseMap();
        CreateMap<Database.Entities.User, DataModels.API.V1.DTOs.Users.UserUpdateDto>().ReverseMap();

        CreateMap<Database.Entities.Role, DataModels.API.V1.DTOs.Roles.RoleDto>().ReverseMap();

        CreateMap<Database.Entities.Scope, DataModels.API.V1.DTOs.Scopes.ScopeDto>().ReverseMap();
    }
}