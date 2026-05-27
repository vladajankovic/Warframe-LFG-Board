using AutoMapper;
using GroupRequests.Database.Entities;
using GroupRequests.Models;

namespace GroupRequests.Utility.Mappers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<GroupRequestEntity, GroupRequestDTO>();

            CreateMap<GroupRequestEntity, GroupRequest>();

            CreateMap<GroupRequestDTO, GroupRequest>();
        }
    }
}
