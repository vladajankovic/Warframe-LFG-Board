using FluentValidation;
using UsersDirectory.Commands;
using UsersDirectory.Utility;

namespace UsersDirectory.Validators
{
    public class UpdateInGameNameCommandValidator : AbstractValidator<UpdateInGameNameCommand>
    {
        public UpdateInGameNameCommandValidator()
        {
            RuleFor(x => x.Email).NotEmpty()
                .WithMessage("Email is required.");
            RuleFor(x => x.Email).Must(EmailUtils.IsValidEmail)
                .WithMessage("Email is not valid.");
            RuleFor(x => x.Email).MaximumLength(254)
                .WithMessage("Email must be at most 254 characters long.");
            RuleFor(x => x.InGameName).NotEmpty()
                .WithMessage("InGameName is required.");
            RuleFor(x => x.InGameName).MaximumLength(50)
                .WithMessage("InGameName must be at most 50 characters long.");
        }
    }
}
