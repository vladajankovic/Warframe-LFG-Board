using FluentValidation;
using ItemsStore.Commands;

namespace ItemsStore.Validators
{
    public class AddResourceCommandValidator : AbstractValidator<AddResourceCommand>
    {
        public AddResourceCommandValidator()
        {
            RuleFor(resource => resource.Name).NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'name' is required");

            RuleFor(resource => resource.Name).MaximumLength(128)
                .WithErrorCode("max length")
                .WithMessage(x => $"Length of '{{PropertyName}}' is {x.Name.Length}, but maximum allowed lenght is {{MaxLength}}");

            RuleFor(resource => resource.Description).NotEmpty()
                .WithErrorCode("required")
                .WithMessage("'name' is required");

            RuleFor(resource => resource.Description).MaximumLength(512)
                .WithErrorCode("max length")
                .WithMessage(x => $"Length of '{{PropertyName}}' is {x.Name.Length}, but maximum allowed lenght is {{MaxLength}}");
        }
    }
}
