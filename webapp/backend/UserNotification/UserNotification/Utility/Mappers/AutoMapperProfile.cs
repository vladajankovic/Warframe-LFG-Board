using AutoMapper;
using UserNotification.Database.Entities;
using UserNotification.Models;

namespace UserNotification.Utility.Mappers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<UserNotificationEntity, GroupRequestNotification>();
        }
    }
}
