using FluentValidation;
using UsersDirectory.Commands;
using UsersDirectory.Utility;

namespace UsersDirectory.Validators
{
    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage("Email is required.");
            RuleFor(x => x.Email).Must(EmailUtils.IsValidEmail)
                .WithMessage("Email is not valid.");
            RuleFor(x => x.Email).MaximumLength(254)
                .WithMessage("Email must be at most 254 characters long.");
            RuleFor(x => x.Password).NotEmpty()
                .WithMessage("Password is required.");
            RuleFor(x => x.Password)
                .Must(PasswordUtils.IsValidPassword)
                .WithMessage("Password must be between 8 and 256 characters and include at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            RuleFor(x => x.Role).NotEmpty()
                .WithMessage("Role is required.");
            RuleFor(x => x.IsVerified).NotEmpty()
                .WithMessage("IsVerified is required.");
        }
    }
}
