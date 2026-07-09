using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.Design;
using UsersDirectory.Broker.RabbitMQ;
using UsersDirectory.Commands;
using UsersDirectory.Database.Entities;
using UsersDirectory.Repositories;
using UsersDirectory.Services;
using UsersDirectory.Utility.Mappers;

namespace UsersDirectory.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUsersRepository> _repositoryMock;
        private readonly Mock<IPasswordHasher<string>> _hasherMock;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<IRabbitMQPublisher> _brokerMock;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _repositoryMock = new Mock<IUsersRepository>();
            _hasherMock = new Mock<IPasswordHasher<string>>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _brokerMock = new Mock<IRabbitMQPublisher>();

            // AutoMapper 14+ requires DI-based configuration
            var services = new ServiceCollection();
            services.AddAutoMapper(typeof(AutoMapperProfile));
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"]      = "TestSecretKey-MustBe-AtLeast-32-Chars!",
                    ["Jwt:Issuer"]   = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                })
                .Build();

            _sut = new UserService(
                _repositoryMock.Object,
                _mapper,
                _hasherMock.Object,
                _configuration,
                _loggerMock.Object,
                _brokerMock.Object
            );
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static UserEntity MakeEntity(string email, string password = "hashed", string role = "User") =>
            new() { Id = Guid.NewGuid().ToString(), Email = email, Password = password, Role = role };

        // ══════════════════════════════════════════════════════════════════════
        // CreateUser
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task CreateUser_NewEmail_ReturnsCreatedUser()
        {
            var command = new AddUserCommand { Email = "new@test.com", Password = "plain" };
            var entity  = MakeEntity(command.Email, "hashed_plain");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync((UserEntity)null!);
            _hasherMock.Setup(h => h.HashPassword(string.Empty, "plain")).Returns("hashed_plain");
            _repositoryMock.Setup(r => r.CreateUser(command)).ReturnsAsync(entity);

            var result = await _sut.CreateUser(command);

            Assert.NotNull(result);
            Assert.Equal("new@test.com", result.Email);
            _repositoryMock.Verify(r => r.CreateUser(command), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ExistingEmail_ThrowsInvalidOperationException()
        {
            var command = new AddUserCommand { Email = "exists@test.com", Password = "plain" };
            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(MakeEntity(command.Email));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateUser(command));
        }

        [Fact]
        public async Task CreateUser_HashesPasswordBeforeSaving()
        {
            var command = new AddUserCommand { Email = "new@test.com", Password = "plain" };
            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync((UserEntity)null!);
            _hasherMock.Setup(h => h.HashPassword(string.Empty, "plain")).Returns("hashed_plain");
            _repositoryMock.Setup(r => r.CreateUser(command)).ReturnsAsync(MakeEntity(command.Email));

            await _sut.CreateUser(command);

            Assert.Equal("hashed_plain", command.Password);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetUser
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetUser_ExistingEmail_ReturnsUser()
        {
            var entity = MakeEntity("user@test.com");
            _repositoryMock.Setup(r => r.GetUser("user@test.com")).ReturnsAsync(entity);

            var result = await _sut.GetUser("user@test.com");

            Assert.NotNull(result);
            Assert.Equal("user@test.com", result.Email);
        }

        [Fact]
        public async Task GetUser_UnknownEmail_ReturnsNull()
        {
            _repositoryMock.Setup(r => r.GetUser("missing@test.com")).ReturnsAsync((UserEntity)null!);

            var result = await _sut.GetUser("missing@test.com");

            Assert.Null(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // GetAllUsers
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetAllUsers_ReturnsAllMappedUsers()
        {
            var entities = new List<UserEntity>
            {
                MakeEntity("a@test.com"),
                MakeEntity("b@test.com"),
            };
            _repositoryMock.Setup(r => r.GetAllUsers()).ReturnsAsync(entities);

            var result = await _sut.GetAllUsers();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.Email == "a@test.com");
            Assert.Contains(result, u => u.Email == "b@test.com");
        }

        [Fact]
        public async Task GetAllUsers_EmptyRepository_ReturnsEmptyList()
        {
            _repositoryMock.Setup(r => r.GetAllUsers()).ReturnsAsync(new List<UserEntity>());

            var result = await _sut.GetAllUsers();

            Assert.Empty(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DeleteUser
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteUser_ExistingUser_ReturnsTrue()
        {
            _repositoryMock.Setup(r => r.DeleteUser("user@test.com")).ReturnsAsync(true);

            var result = await _sut.DeleteUser("user@test.com");

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteUser_UnknownUser_ReturnsFalse()
        {
            _repositoryMock.Setup(r => r.DeleteUser("missing@test.com")).ReturnsAsync(false);

            var result = await _sut.DeleteUser("missing@test.com");

            Assert.False(result);
        }

        // ══════════════════════════════════════════════════════════════════════
        // UpdateUser
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateUser_ExistingUser_ReturnsUpdatedUser()
        {
            var command  = new UpdateUserCommand { Email = "user@test.com", Password = "newpass" };
            var existing = MakeEntity("user@test.com", "old_hash");
            var updated  = MakeEntity("user@test.com", "new_hash");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(existing);
            _hasherMock.Setup(h => h.HashPassword(string.Empty, "newpass")).Returns("new_hash");
            _repositoryMock.Setup(r => r.UpdateUser(command)).ReturnsAsync(updated);

            var result = await _sut.UpdateUser(command);

            Assert.NotNull(result);
            Assert.Equal("user@test.com", result.Email);
            _repositoryMock.Verify(r => r.UpdateUser(command), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_UnknownUser_ThrowsKeyNotFoundException()
        {
            var command = new UpdateUserCommand { Email = "missing@test.com", Password = "pass" };
            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync((UserEntity)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.UpdateUser(command));
        }

        [Fact]
        public async Task UpdateUser_HashesNewPasswordBeforeSaving()
        {
            var command  = new UpdateUserCommand { Email = "user@test.com", Password = "newpass" };
            var existing = MakeEntity("user@test.com");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(existing);
            _hasherMock.Setup(h => h.HashPassword(string.Empty, "newpass")).Returns("new_hash");
            _repositoryMock.Setup(r => r.UpdateUser(command)).ReturnsAsync(existing);

            await _sut.UpdateUser(command);

            Assert.Equal("new_hash", command.Password);
        }

        // ══════════════════════════════════════════════════════════════════════
        // LoginUser
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task LoginUser_ValidCredentials_ReturnsLoginResponseWithToken()
        {
            var command = new LoginCommand { Email = "user@test.com", Password = "plain" };
            var entity  = MakeEntity("user@test.com", "hashed", "User");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(string.Empty, "hashed", "plain"))
                       .Returns(PasswordVerificationResult.Success);

            var result = await _sut.LoginUser(command);

            Assert.NotNull(result);
            Assert.Equal("user@test.com", result.Email);
            Assert.Equal("User", result.Role);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
        }

        [Fact]
        public async Task LoginUser_SuccessRehashNeeded_ReturnsTokenWithCorrectRole()
        {
            var command = new LoginCommand { Email = "user@test.com", Password = "plain" };
            var entity  = MakeEntity("user@test.com", "old_hash", "Admin");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(string.Empty, "old_hash", "plain"))
                       .Returns(PasswordVerificationResult.SuccessRehashNeeded);

            var result = await _sut.LoginUser(command);

            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task LoginUser_UnknownEmail_ThrowsKeyNotFoundException()
        {
            var command = new LoginCommand { Email = "missing@test.com", Password = "plain" };
            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync((UserEntity)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.LoginUser(command));
        }

        [Fact]
        public async Task LoginUser_WrongPassword_ThrowsUnauthorizedAccessException()
        {
            var command = new LoginCommand { Email = "user@test.com", Password = "wrong" };
            var entity  = MakeEntity("user@test.com", "hashed");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(string.Empty, "hashed", "wrong"))
                       .Returns(PasswordVerificationResult.Failed);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginUser(command));
        }

        [Fact]
        public async Task LoginUser_ValidCredentials_TokenContainsEmailAndRoleClaims()
        {
            var command = new LoginCommand { Email = "admin@test.com", Password = "plain" };
            var entity  = MakeEntity("admin@test.com", "hashed", "Admin");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(string.Empty, "hashed", "plain"))
                       .Returns(PasswordVerificationResult.Success);

            var result = await _sut.LoginUser(command);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);

            Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == "admin@test.com");
            Assert.Contains(jwt.Claims, c => c.Value == "Admin");
        }

        [Fact]
        public async Task ActivateSubscription_ReturnsTokenWithActiveSubscriptionClaim()
        {
            var email = "user@test.com";
            var entity = MakeEntity(email);
            entity.SubscriptionStatus = UsersDirectory.Enums.SubscriptionStatus.Inactive;

            var updated = MakeEntity(email);
            updated.Id = entity.Id;
            updated.SubscriptionStatus = UsersDirectory.Enums.SubscriptionStatus.Active;
            updated.SubscriptionExpiryDate = DateTime.UtcNow.AddMonths(1);

            _repositoryMock.Setup(r => r.GetUser(email)).ReturnsAsync(entity);
            _repositoryMock
                .Setup(r => r.UpdateSubscription(
                    email,
                    UsersDirectory.Enums.SubscriptionStatus.Active,
                    It.IsAny<DateTime>()))
                .ReturnsAsync(updated);

            var result = await _sut.ActivateSubscription(email);

            Assert.Equal(UsersDirectory.Enums.SubscriptionStatus.Active, result.SubscriptionStatus);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);
            Assert.Contains(jwt.Claims, c => c.Type == "subscription_status" && c.Value == "Active");
        }

        [Fact]
        public async Task CancelSubscription_ReturnsTokenWithInactiveSubscriptionClaim()
        {
            var email = "user@test.com";
            var expiry = DateTime.UtcNow.AddMonths(1);
            var entity = MakeEntity(email);
            entity.SubscriptionStatus = UsersDirectory.Enums.SubscriptionStatus.Active;
            entity.SubscriptionExpiryDate = expiry;

            var updated = MakeEntity(email);
            updated.Id = entity.Id;
            updated.SubscriptionStatus = UsersDirectory.Enums.SubscriptionStatus.Inactive;
            updated.SubscriptionExpiryDate = expiry;

            _repositoryMock.Setup(r => r.GetUser(email)).ReturnsAsync(entity);
            _repositoryMock
                .Setup(r => r.UpdateSubscription(email, UsersDirectory.Enums.SubscriptionStatus.Inactive, expiry))
                .ReturnsAsync(updated);

            var result = await _sut.CancelSubscription(email);

            Assert.Equal(UsersDirectory.Enums.SubscriptionStatus.Inactive, result.SubscriptionStatus);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);
            Assert.Contains(jwt.Claims, c => c.Type == "subscription_status" && c.Value == "Inactive");
        }
    }
}
