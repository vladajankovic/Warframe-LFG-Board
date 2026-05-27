namespace UsersDirectory.Commands
{
    public class UpdateUserCommand
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string InGameName { get; set; }
        public bool IsVerified { get; set; }
    }
}
