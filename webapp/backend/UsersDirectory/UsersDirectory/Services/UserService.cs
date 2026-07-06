using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UsersDirectory.Broker.RabbitMQ;
using UsersDirectory.Commands;
using UsersDirectory.Enums;
using UsersDirectory.Models;
using UsersDirectory.Repositories;

namespace UsersDirectory.Services
{
    public class UserService : IUsersService
    {
        private readonly IUsersRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<string> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;

        public UserService(
            IUsersRepository usersRepository,
            IMapper mapper,
            IPasswordHasher<string> passwordHasher,
            IConfiguration configuration,
            ILogger<UserService> logger,
            IRabbitMQPublisher rabbitMQPublisher
        )
        {
            _userRepository = usersRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _logger = logger;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        public async Task<User> CreateUser(AddUserCommand command)
        {
            var userEntity = await _userRepository.GetUser(command.Email);

            if (userEntity != null)
            {
                throw new InvalidOperationException($"User '{command.Email}' already exists");
            }

            command.Password = _passwordHasher.HashPassword(string.Empty, command.Password);

            var newUser = await _userRepository.CreateUser(command);

            try
            {
                var message = new AuditEventMessage(
                    "Create",
                    newUser.Id,
                    $"User created: { newUser.Id }");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Id}", newUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Id}", newUser.Id);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return _mapper.Map<User>(newUser);
        }

        public async Task<bool> DeleteUser(string email)
        {
            var user = await _userRepository.GetUser(email);
            var result = await _userRepository.DeleteUser(email);

            if (result)
            {
                try
                {
                    var message = new AuditEventMessage(
                        "Delete",
                        user.Id,
                        $"User deleted: {user.Id}");

                    await _rabbitMQPublisher.PublishAsync("audit", message);
                    _logger.LogInformation("Published AuditEventMessage for user {Email}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", email);
                    // Don't rethrow - the group request was created successfully, we just failed to notify
                }
            }

            return result;
        }

        public async Task<List<User>> GetAllUsers()
        {
            var userList = await _userRepository.GetAllUsers();
            return _mapper.Map<List<User>>(userList);
        }

        public async Task<User> GetUser(string email)
        {
            var userEntity = await _userRepository.GetUser(email);

            if (userEntity == null)
            {
                return null;
            }

            return _mapper.Map<User>(userEntity);
        }

        public async Task<User> UpdateUser(UpdateUserCommand command)
        {
            var userEntity = await _userRepository.GetUser(command.Email);

            if (userEntity == null)
            {
                throw new KeyNotFoundException($"User '{command.Email}' not found");
            }

            if (command.Password != null)
            {
                command.Password = _passwordHasher.HashPassword(string.Empty, command.Password);
            }

            var updatedUser = await _userRepository.UpdateUser(command);

            try
            {
                var message = new AuditEventMessage(
                    "Update",
                    command.Email,
                    $"User updated: {command.Email}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Email}", command.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", command.Email);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return _mapper.Map<User>(updatedUser);
        }

        public async Task<LoginResponse> LoginUser(LoginCommand user)
        {
            var foundUser = await _userRepository.GetUser(user.Email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{user.Email}' not found");
            }

            var result = _passwordHasher.VerifyHashedPassword(string.Empty, foundUser.Password, user.Password);

            if (result != PasswordVerificationResult.Success && result != PasswordVerificationResult.SuccessRehashNeeded)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var token = GenerateJwtToken(foundUser.Id, foundUser.Email, foundUser.Role, foundUser.SubscriptionStatus);

            try
            {
                var message = new AuditEventMessage(
                    "Login",
                    user.Email,
                    $"User login: {user.Email}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", user.Email);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return new LoginResponse
            {
                UserId = foundUser.Id,
                Token = token,
                Email = foundUser.Email,
                Role = foundUser.Role,
                InGameName = foundUser.InGameName,
                SubscriptionStatus = foundUser.SubscriptionStatus,
                SubscriptionExpiryDate = foundUser.SubscriptionExpiryDate,
            };
        }

        public async Task<bool> IsUserVerified(string email)
        {
            var foundUser = await _userRepository.GetUser(email);

            if (foundUser == null)
            {
                return false;
            }

            return foundUser.IsVerified;
        }

        public async Task<User> ChangePassword(ChangePasswordCommand user)
        {
            var foundUser = await _userRepository.GetUser(user.Email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{user.Email}' not found");
            }


            var result = _passwordHasher.VerifyHashedPassword(string.Empty, foundUser.Password, user.OldPassword);

            if (result != PasswordVerificationResult.Success && result != PasswordVerificationResult.SuccessRehashNeeded)
            {
                throw new InvalidOperationException("Incorect value of old password.");
            }

            if (user.OldPassword == user.NewPassword)
            {
                throw new InvalidOperationException("New password must be different from the old password.");
            }

            user.NewPassword = _passwordHasher.HashPassword(string.Empty, user.NewPassword);

            var updatedUser = await _userRepository.UpdateUser(
                new UpdateUserCommand
                {
                    Email = user.Email,
                    Password = user.NewPassword,
                    Role = foundUser.Role,
                    InGameName = foundUser.InGameName,
                    IsVerified = foundUser.IsVerified
                }
            );

            try
            {
                var message = new AuditEventMessage(
                    "Update",
                    user.Email,
                    $"Password change: {user.Email}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", user.Email);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return _mapper.Map<User>(updatedUser);
        }

        public async Task<User> UpdateInGameName(UpdateInGameNameCommand command)
        {
            var foundUser = await _userRepository.GetUser(command.Email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{command.Email}' not found");
            }

            var updatedUser = await _userRepository.UpdateUser(
                new UpdateUserCommand
                {
                    Email = foundUser.Email,
                    Password = foundUser.Password,
                    Role = foundUser.Role,
                    InGameName = command.InGameName,
                    IsVerified = foundUser.IsVerified
                }
            );

            try
            {
                var message = new AuditEventMessage(
                    "Update",
                    foundUser.Email,
                    $"In-game name changed to {command.InGameName}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Email}", foundUser.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", foundUser.Email);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return _mapper.Map<User>(updatedUser);
        }

        public async Task<bool> DeleteAccount(AddUserCommand user)
        {
            var foundUser = await _userRepository.GetUser(user.Email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{user.Email}' not found");
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(string.Empty, foundUser.Password, user.Password);

            if (verificationResult != PasswordVerificationResult.Success && verificationResult != PasswordVerificationResult.SuccessRehashNeeded)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var result = await _userRepository.DeleteUser(user.Email);

            if (result)
            {
                try
                {
                    var message = new AuditEventMessage(
                        "Delete",
                        user.Email,
                        $"User deleted: {user.Email}");

                    await _rabbitMQPublisher.PublishAsync("audit", message);
                    _logger.LogInformation("Published AuditEventMessage for user {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", user.Email);
                    // Don't rethrow - the group request was created successfully, we just failed to notify
                }
            }

            return result;
        }

        private string GenerateJwtToken(string id, string email, string role, SubscriptionStatus subscriptionStatus)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, id),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("subscription_status", subscriptionStatus.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<SubscriptionInfo> ActivateSubscription(string email)
        {
            var foundUser = await _userRepository.GetUser(email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{email}' not found");
            }

            if (foundUser.SubscriptionStatus == SubscriptionStatus.Active)
            {
                throw new InvalidOperationException("Subscription is already active.");
            }

            var expiryDate = DateTime.UtcNow.AddMonths(1);
            var updatedEntity = await _userRepository.UpdateSubscription(email, SubscriptionStatus.Active, expiryDate);

            try
            {
                var message = new AuditEventMessage(
                    "Subsctiption_Activated",
                    email,
                    $"Subscription activated for user {email}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", email);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return new SubscriptionInfo
            {
                SubscriptionStatus = updatedEntity.SubscriptionStatus,
                SubscriptionExpiryDate = updatedEntity.SubscriptionExpiryDate,
            };
        }

        public async Task<SubscriptionInfo> CancelSubscription(string email)
        {
            var foundUser = await _userRepository.GetUser(email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{email}' not found");
            }

            if (foundUser.SubscriptionStatus == SubscriptionStatus.Inactive)
            {
                throw new InvalidOperationException("Subscription is already inactive.");
            }

            var updatedEntity = await _userRepository.UpdateSubscription(email, SubscriptionStatus.Inactive, foundUser.SubscriptionExpiryDate);

            try
            {
                var message = new AuditEventMessage(
                    "Subscription_Canceled",
                    email,
                    $"Subscription canceled for user {email}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for user {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage user {Email}", email);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return new SubscriptionInfo
            {
                SubscriptionStatus = updatedEntity.SubscriptionStatus,
                SubscriptionExpiryDate = updatedEntity.SubscriptionExpiryDate,
            };
        }

        public async Task<SubscriptionInfo> GetSubscription(string email)
        {
            var foundUser = await _userRepository.GetUser(email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{email}' not found");
            }

            return new SubscriptionInfo
            {
                SubscriptionStatus = foundUser.SubscriptionStatus,
                SubscriptionExpiryDate = foundUser.SubscriptionExpiryDate,
            };
        }
    }
}
