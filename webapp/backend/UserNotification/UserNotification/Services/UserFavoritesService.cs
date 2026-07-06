using UserNotification.Commands;
using UserNotification.Database.Entities;
using UserNotification.Models;
using UserNotification.Repositories;
using UsersDirectory.Broker.RabbitMQ;

namespace UserNotification.Services
{
    public class UserFavoritesService : IUserFavoritesService
    {
        private readonly IUserFavoritesRepository _repository;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly ILogger<UserFavoritesService> _logger;

        public UserFavoritesService(
            IUserFavoritesRepository repository, 
            IRabbitMQPublisher rabbitMQPublisher, 
            ILogger<UserFavoritesService> logger
        )
        {
            _repository = repository;
            _rabbitMQPublisher = rabbitMQPublisher;
            _logger = logger;
        }

        public async Task<UserFavoriteEntity> Create(AddOrRemoveUserFavoritesCommand command)
        {
            var result = await _repository.Get(command.UserId, command.ItemName);

            if (result != null)
            {
                return result;
            }

            var entity = _repository.Create(command);

            try
            {
                var message = new AuditEventMessage(
                    "Added",
                    command.UserId,
                    $"User {command.UserId} has added {command.ItemName} to favorites.");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Id}", command.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Id}", command.UserId);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return result;
        }

        public async Task<bool> Delete(AddOrRemoveUserFavoritesCommand command)
        {
            var result = await _repository.Delete(command);

            if (result)
            {
                try
                {
                    var message = new AuditEventMessage(
                        "Removed",
                        command.UserId,
                        $"User {command.UserId} has removed {command.ItemName} from favorites.");

                    await _rabbitMQPublisher.PublishAsync("audit", message);
                    _logger.LogInformation("Published AuditEventMessage for user {Id}", command.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AuditEventMessage user {Id}", command.UserId);
                    // Don't rethrow - the group request was created successfully, we just failed to notify
                }
            }

            return result;
        }

        public async Task<List<UserFavorites>> GetAll()
        {
            var result = await _repository.GetAll();
            var users = result.GroupBy(x => x.UserId).Select(x => x.Key).ToList();

            List<UserFavorites> list = new ();

            users.ForEach(u =>
            {
                list.Add(new UserFavorites
                {
                    UserId = u,
                    FavoriteItems = result.Where(x => x.UserId.Equals(u)).Select(x => x.ItemName).ToList()
                });
            });

            return list;
        }

        public async Task<UserFavorites> GetAllFromUser(string userId)
        {
            var result = await _repository.GetAllFromUser(userId);

            var userFavorites = new UserFavorites
            {
                UserId = userId,
                FavoriteItems = result.Select(x => x.ItemName).ToList()
            };

            return userFavorites;
        }
    }
}
