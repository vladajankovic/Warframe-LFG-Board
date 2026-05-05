using FluentValidation.TestHelper;
using ItemsStore.Commands;
using ItemsStore.Validators;
using Xunit;

namespace ItemsStore.Tests.Validators
{
    public class AddResourceCommandValidatorTests
    {
        private readonly AddResourceCommandValidator _validator = new();

        private static AddResourceCommand Valid() => new()
        {
            Name = "Ferrite",
            Description = "A common resource used in many blueprints."
        };

        // ── Name ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Name_WhenEmpty_FailsWithRequiredError()
        {
            var command = Valid();
            command.Name = string.Empty;

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("required");
        }

        [Fact]
        public void Name_WhenExceeds128Chars_FailsWithMaxLengthError()
        {
            var command = Valid();
            command.Name = new string('a', 129);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorCode("max length");
        }

        [Fact]
        public void Name_WhenValid_Passes()
        {
            var result = _validator.TestValidate(Valid());

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        // ── Description ──────────────────────────────────────────────────────────

        [Fact]
        public void Description_WhenEmpty_FailsWithRequiredError()
        {
            var command = Valid();
            command.Description = string.Empty;

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorCode("required");
        }

        [Fact]
        public void Description_WhenExceeds512Chars_FailsWithMaxLengthError()
        {
            var command = Valid();
            command.Description = new string('a', 513);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorCode("max length");
        }

        [Fact]
        public void Description_WhenValid_Passes()
        {
            var result = _validator.TestValidate(Valid());

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        // ── Valid command ─────────────────────────────────────────────────────────

        [Fact]
        public void ValidCommand_PassesAllRules()
        {
            var result = _validator.TestValidate(Valid());

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
