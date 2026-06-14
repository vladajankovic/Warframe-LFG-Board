using Audit.Database.Entities;
using Audit.Models;
using AutoMapper;

namespace Audit.Utility.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<AuditLogEntity, AuditLog>();
        }
    }
}
