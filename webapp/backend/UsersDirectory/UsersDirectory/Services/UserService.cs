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
        private readonly IPasswordHasher<string> _passwordHasher;
        private readonly IConfiguration _configuration;

        public UserService(
            IUsersRepository usersRepository,
            IMapper mapper,
            IPasswordHasher<string> passwordHasher,
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

            command.Password = _passwordHasher.HashPassword(string.Empty, command.Password);

            var newUser = await _userRepository.CreateUser(command);

            return _mapper.Map<User>(newUser);
        }

        public async Task<bool> DeleteUser(string email)
        {
            return await _userRepository.DeleteUser(email);
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

            return _mapper.Map<User>(updatedUser);
        }

        public async Task<LoginResponse> LoginUser(AddUserCommand user)
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

            var token = GenerateJwtToken(foundUser.Email, foundUser.Role);

            return new LoginResponse
            {
                Token = token,
                Email = foundUser.Email,
                Role = foundUser.Role,
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
                    IsVerified = foundUser.IsVerified
                }
            );

            return _mapper.Map<User>(updatedUser);
        }

        public async Task<bool> DeleteAccount(AddUserCommand user)
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

            return await _userRepository.DeleteUser(user.Email);
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
