using FluentValidation;
using GroupRequests.Commands;

namespace GroupRequests.Validation
{
    public class DeleteGroupRequestCommandValidator : AbstractValidator<DeleteGroupRequestCommand>
    {
        public DeleteGroupRequestCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'user-id' is required");
            RuleFor(x => x.ItemName)
                .NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'item-name' is required");
        }
    }
}
