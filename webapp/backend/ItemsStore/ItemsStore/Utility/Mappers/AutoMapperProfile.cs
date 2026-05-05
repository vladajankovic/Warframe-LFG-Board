using AutoMapper;
using ItemsStore.Commands;
using ItemsStore.Database.Entity;
using ItemsStore.Models;

namespace ItemsStore.Utility.Mappers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<RelicEntity, Relic>()
                .ForMember(dest => dest.RelicType, opt => opt.MapFrom(src => src.RelicType.ToString().ToLower()));

            CreateMap<ResourceEntity, Resource>();
        }
    }
}
