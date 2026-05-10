using AutoMapper;
using Microsoft.AspNetCore.Identity;
using UsersDirectory.Commands;
using UsersDirectory.Models;
using UsersDirectory.Repositories;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UsersDirectory.Services
{
    public class UserService : IUsersService
    {
        private readonly IUsersRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher<AddUserCommand> _passwordHasher;
        public UserService(
            IUsersRepository usersRepository,
            IMapper mapper,
            IPasswordHasher<AddUserCommand> passwordHasher
        )
        {
            _userRepository = usersRepository;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
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

        public async Task<bool> LoginUser(AddUserCommand user)
        {
            var foundUser = await _userRepository.GetUser(user.Email);

            if (foundUser == null)
            {
                throw new KeyNotFoundException($"User '{user.Email}' not found");
            }

            var result = _passwordHasher.VerifyHashedPassword(user, foundUser.Password, user.Password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
