using AutoMapper;

namespace Bl4ckout.MyMasternode.Auth.AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Database.Models.User, DataModels.Auth.V1.DTOs.Users.UserDto>().ReverseMap();
        CreateMap<Database.Models.User, DataModels.Auth.V1.DTOs.Users.UserCreateDto>().ReverseMap();
        CreateMap<Database.Models.User, DataModels.Auth.V1.DTOs.Users.UserUpdateDto>().ReverseMap();
    }
}