using FluentValidation;
using UsersDirectory.Commands;
using UsersDirectory.Utility;

namespace UsersDirectory.Validators
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage("Email is required.");
            RuleFor(x => x.Email).Must(EmailUtils.IsValidEmail)
                .WithMessage("Email is not valid.");
            RuleFor(x => x.Email).MaximumLength(254)
                .WithMessage("Email must be at most 254 characters long.");
            RuleFor(x => x.OldPassword).NotEmpty()
                .WithMessage("OldPassword is required.");
            RuleFor(x => x.OldPassword)
                .Must(PasswordUtils.IsValidPassword)
                .WithMessage("OldPassword must be between 8 and 256 characters and include at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            RuleFor(x => x.NewPassword).NotEmpty()
                    .WithMessage("NewPassword is required.");
            RuleFor(x => x.NewPassword)
                .Must(PasswordUtils.IsValidPassword)
                .WithMessage("NewPassword must be between 8 and 256 characters and include at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            RuleFor(x => x)
                .Must(x => x.OldPassword != x.NewPassword)
                .WithMessage("NewPassword must be different from OldPassword.");
        }
    }
}
