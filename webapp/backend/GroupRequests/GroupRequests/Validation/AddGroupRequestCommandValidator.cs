using FluentValidation;
using GroupRequests.Commands;

namespace GroupRequests.Validation
{
    public class AddGroupRequestCommandValidator : AbstractValidator<AddGroupRequestCommand>
    {
        public AddGroupRequestCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'user-id' is required");
            RuleFor(x => x.InGameName)
                .NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'in-game-name' is required");
            RuleFor(x => x.ItemName)
                .NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'item-name' is required");
        }
    }
}
