using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AutoMapper;
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
        private readonly Mock<IPasswordHasher<AddUserCommand>> _hasherMock;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly UserService _sut;

        public UserServiceTests()
        {
            _repositoryMock = new Mock<IUsersRepository>();
            _hasherMock = new Mock<IPasswordHasher<AddUserCommand>>();

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
                _configuration
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
            _hasherMock.Setup(h => h.HashPassword(command, "plain")).Returns("hashed_plain");
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
            _hasherMock.Setup(h => h.HashPassword(command, "plain")).Returns("hashed_plain");
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
            var command  = new AddUserCommand { Email = "user@test.com", Password = "newpass" };
            var existing = MakeEntity("user@test.com", "old_hash");
            var updated  = MakeEntity("user@test.com", "new_hash");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(existing);
            _hasherMock.Setup(h => h.HashPassword(command, "newpass")).Returns("new_hash");
            _repositoryMock.Setup(r => r.UpdateUser(command)).ReturnsAsync(updated);

            var result = await _sut.UpdateUser(command);

            Assert.NotNull(result);
            Assert.Equal("user@test.com", result.Email);
            _repositoryMock.Verify(r => r.UpdateUser(command), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_UnknownUser_ThrowsKeyNotFoundException()
        {
            var command = new AddUserCommand { Email = "missing@test.com", Password = "pass" };
            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync((UserEntity)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.UpdateUser(command));
        }

        [Fact]
        public async Task UpdateUser_HashesNewPasswordBeforeSaving()
        {
            var command  = new AddUserCommand { Email = "user@test.com", Password = "newpass" };
            var existing = MakeEntity("user@test.com");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(existing);
            _hasherMock.Setup(h => h.HashPassword(command, "newpass")).Returns("new_hash");
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
            var command = new AddUserCommand { Email = "user@test.com", Password = "plain" };
            var entity  = MakeEntity("user@test.com", "hashed", "User");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(command, "hashed", "plain"))
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
            var command = new AddUserCommand { Email = "user@test.com", Password = "plain" };
            var entity  = MakeEntity("user@test.com", "old_hash", "Admin");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(command, "old_hash", "plain"))
                       .Returns(PasswordVerificationResult.SuccessRehashNeeded);

            var result = await _sut.LoginUser(command);

            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.Equal("Admin", result.Role);
        }

        [Fact]
        public async Task LoginUser_UnknownEmail_ThrowsKeyNotFoundException()
        {
            var command = new AddUserCommand { Email = "missing@test.com", Password = "plain" };
            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync((UserEntity)null!);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.LoginUser(command));
        }

        [Fact]
        public async Task LoginUser_WrongPassword_ThrowsUnauthorizedAccessException()
        {
            var command = new AddUserCommand { Email = "user@test.com", Password = "wrong" };
            var entity  = MakeEntity("user@test.com", "hashed");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(command, "hashed", "wrong"))
                       .Returns(PasswordVerificationResult.Failed);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.LoginUser(command));
        }

        [Fact]
        public async Task LoginUser_ValidCredentials_TokenContainsEmailAndRoleClaims()
        {
            var command = new AddUserCommand { Email = "admin@test.com", Password = "plain" };
            var entity  = MakeEntity("admin@test.com", "hashed", "Admin");

            _repositoryMock.Setup(r => r.GetUser(command.Email)).ReturnsAsync(entity);
            _hasherMock.Setup(h => h.VerifyHashedPassword(command, "hashed", "plain"))
                       .Returns(PasswordVerificationResult.Success);

            var result = await _sut.LoginUser(command);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(result.Token);

            Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == "admin@test.com");
            Assert.Contains(jwt.Claims, c => c.Value == "Admin");
        }
    }
}
