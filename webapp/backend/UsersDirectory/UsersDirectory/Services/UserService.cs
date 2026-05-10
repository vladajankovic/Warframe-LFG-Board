using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UsersDirectory.Commands;
using UsersDirectory.Models;
using UsersDirectory.Repositories;

namespace UsersDirectory.Services
{
    public class UserService : IUsersService
    {
        private readonly IUsersRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<AddUserCommand> _passwordHasher;
        private readonly IConfiguration _configuration;

        public UserService(
            IUsersRepository usersRepository,
            IMapper mapper,
            IPasswordHasher<AddUserCommand> passwordHasher,
            IConfiguration configuration
        )
        {
            _userRepository = usersRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<User> CreateUser(AddUserCommand command)
        {
            var userEntity = await _userRepository.GetUser(command.Email);

            if (userEntity != null)
            {
                throw new InvalidOperationException($"User '{command.Email}' already exists");
            }

            command.Password = _passwordHasher.HashPassword(command, command.Password);

            var newUser = await _userRepository.CreateUser(command);

            return _mapper.Map<User>(newUser);
        }

        public Task<bool> DeleteUser(string email)
        {
            return _userRepository.DeleteUser(email);
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

        public async Task<User> UpdateUser(AddUserCommand command)
        {
            var userEntity = await _userRepository.GetUser(command.Email);

            if (userEntity == null)
            {
                throw new KeyNotFoundException($"User '{command.Email}' not found");
            }

            command.Password = _passwordHasher.HashPassword(command, command.Password);

            var updatedUser = await _userRepository.UpdateUser(command);

            return _mapper.Map<User>(updatedUser);
        }

        public async Task<LoginResponse> LoginUser(AddUserCommand user)
        {
            var foundUser = await _userRepository.GetUser(user.Email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{user.Email}' not found");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, foundUser.Password, user.Password);
            if (result != PasswordVerificationResult.Success && result != PasswordVerificationResult.SuccessRehashNeeded)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var token = GenerateJwtToken(foundUser.Email, foundUser.Role);

            return new LoginResponse
            {
                Token = token,
                Email = foundUser.Email,
                Role = foundUser.Role,
            };
        }

        private string GenerateJwtToken(string email, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
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
    }
}
