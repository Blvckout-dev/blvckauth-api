using AutoMapper;

namespace Bl4ckout.MyMasternode.Auth.AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Database.Entities.User, DataModels.Auth.V1.DTOs.Users.UserDto>()
            .ForMember(
                dest => dest.ScopeIds,
                opt => opt.MapFrom(
                    src => src.Scopes != null ?
                    src.Scopes.Select(s => s.Id) :
                    Enumerable.Empty<int>()
                )
            )
            .ReverseMap();
        CreateMap<Database.Entities.User, DataModels.Auth.V1.DTOs.Users.UserMinimalDto>().ReverseMap();
        CreateMap<Database.Entities.User, DataModels.Auth.V1.DTOs.Users.UserDetailDto>().ReverseMap();
        CreateMap<Database.Entities.User, DataModels.Auth.V1.DTOs.Users.UserCreateDto>().ReverseMap();
        CreateMap<Database.Entities.User, DataModels.Auth.V1.DTOs.Users.UserUpdateDto>().ReverseMap();

        CreateMap<Database.Entities.Role, DataModels.Auth.V1.DTOs.Roles.RoleDto>().ReverseMap();

        CreateMap<Database.Entities.Scope, DataModels.Auth.V1.DTOs.Scopes.ScopeDto>().ReverseMap();
    }
}