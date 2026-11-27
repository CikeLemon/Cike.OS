using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Tags.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Cike.OS.Tests;

public class CreateTagDtoValidatorTests
{
    private readonly CreateTagDtoValidator _validator = new();

    private static CreateTagDto Valid(string key = "vision") => new()
    {
        Key = key,
        Name = "视觉",
    };

    [Theory]
    [InlineData("vision")]
    [InlineData("vision-2")]
    [InlineData("vision_v2")]
    [InlineData("a")]
    public void Validate_ValidKey_HasNoErrors(string key)
    {
        _validator.TestValidate(Valid(key)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Vision")]
    [InlineData("2vision")]
    [InlineData("vis ion")]
    [InlineData("视觉")]
    public void Validate_InvalidKeyFormat_HasError(string key)
    {
        _validator.TestValidate(Valid(key)).ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Fact]
    public void Validate_TooLongKey_HasError()
    {
        _validator.TestValidate(Valid(new string('a', 65))).ShouldHaveValidationErrorFor(x => x.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyName_HasError(string name)
    {
        var dto = Valid();
        dto.Name = name;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_TooLongName_HasError()
    {
        var dto = Valid();
        dto.Name = new string('长', 129);

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }
}
