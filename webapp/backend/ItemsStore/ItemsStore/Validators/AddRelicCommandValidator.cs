using FluentValidation;
using ItemsStore.Commands;

namespace ItemsStore.Validators
{
    public class AddRelicCommandValidator : AbstractValidator<AddRelicCommand>
    {
        public AddRelicCommandValidator()
        {
            RuleFor(relic => relic.Name).NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'name' is required");

            RuleFor(relic => relic.Name).MaximumLength(30)
                .WithErrorCode("max length")
                .WithMessage(x => $"Length of '{{PropertyName}}' is {x.Name.Length}, but maximum allowed lenght is {{MaxLength}}");

            RuleFor(relic => relic.Type).IsInEnum()
                .WithErrorCode("enum int value")
                .WithMessage("Bad enum integer value for property '{PropertyName}'");

            RuleFor(relic => relic.Code).NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'code' is required");

            RuleFor(relic => relic.Code).MaximumLength(10)
                .WithErrorCode("max length")
                .WithMessage(x => $"Length of '{{PropertyName}}' is {x.Name.Length}, but maximum allowed lenght is {{MaxLength}}");
        }
    }
}
