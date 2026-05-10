using AutoMapper;
using UsersDirectory.Database.Entities;
using UsersDirectory.Models;

namespace UsersDirectory.Utility.Mappers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserEntity, User>();
        }
    }
}
