using FluentValidation.TestHelper;
using ItemsStore.Commands;
using ItemsStore.Database.Entity;
using ItemsStore.Validators;
using Xunit;

namespace ItemsStore.Tests.Validators
{
    public class AddRelicCommandValidatorTests
    {
        private readonly AddRelicCommandValidator _validator = new();

        private static AddRelicCommand Valid() => new()
        {
            Name = "Lith A1 Intact",
            Type = RelicType.LITH,
            Code = "A1"
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
        public void Name_WhenExceeds30Chars_FailsWithMaxLengthError()
        {
            var command = Valid();
            command.Name = new string('a', 31);

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

        // ── Type ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Type_WhenInvalidEnumValue_FailsWithEnumError()
        {
            var command = Valid();
            command.Type = (RelicType)999;

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Type)
                .WithErrorCode("enum int value");
        }

        [Theory]
        [InlineData(RelicType.LITH)]
        [InlineData(RelicType.MESO)]
        [InlineData(RelicType.NEO)]
        [InlineData(RelicType.AXI)]
        [InlineData(RelicType.REQUIEM)]
        [InlineData(RelicType.VANGUARD)]
        public void Type_WhenValidEnumValue_Passes(RelicType type)
        {
            var command = Valid();
            command.Type = type;

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Type);
        }

        // ── Code ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Code_WhenEmpty_FailsWithRequiredError()
        {
            var command = Valid();
            command.Code = string.Empty;

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorCode("required");
        }

        [Fact]
        public void Code_WhenExceeds10Chars_FailsWithMaxLengthError()
        {
            var command = Valid();
            command.Code = new string('a', 11);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorCode("max length");
        }

        [Fact]
        public void Code_WhenValid_Passes()
        {
            var result = _validator.TestValidate(Valid());

            result.ShouldNotHaveValidationErrorFor(x => x.Code);
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
