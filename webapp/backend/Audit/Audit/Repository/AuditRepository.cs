using Audit.Commands;
using Audit.Database;
using Audit.Database.Entities;
using Audit.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Audit.Repository
{
    public class AuditRepository : IAuditRepository
    {
        private readonly AuditDbContext _dbContext;
        private readonly IMapper _mapper;

        public AuditRepository(AuditDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }
        public async Task<AuditLogEntity> CreateAuditLog(CreateAuditLogCommand command)
        {
            var logEntity = new AuditLogEntity()
            {
                DateTime = command.DateTime,
                Service = command.Service,
                Action = command.Action,
                User = command.User,
                Details = command.Details,
            };

            await _dbContext.AuditLog.AddAsync(logEntity);
            await _dbContext.SaveChangesAsync();

            return logEntity;
        }

        public async Task<List<AuditLog>> Get(DateTime? from, DateTime? to, string service, string action, string user)
        {
            var query = _dbContext.AuditLog.AsQueryable();

            if (from != null)
            {
                query = query.Where(x => x.DateTime >= from);
            }

            if (to != null)
            {
                query = query.Where(x => x.DateTime <= to);
            }

            if (!string.IsNullOrEmpty(service))
            {
                query = query.Where(x => x.Service.Equals(service));
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(x => x.Action.Equals(action));
            }

            if (!string.IsNullOrEmpty(user))
            {
                query = query.Where(x => x.User.Equals(user));
            }

            query = query.OrderByDescending(x => x.DateTime);

            var result = query.AsNoTracking().ToList();

            return _mapper.Map<List<AuditLog>>(result);
        }
    }
}
