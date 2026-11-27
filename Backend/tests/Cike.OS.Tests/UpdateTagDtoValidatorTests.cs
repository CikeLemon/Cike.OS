using Cike.OS.Application.Contracts.Tags.Dtos;
using Cike.OS.Application.Tags.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Cike.OS.Tests;

public class UpdateTagDtoValidatorTests
{
    private readonly UpdateTagDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidName_HasNoErrors()
    {
        _validator.TestValidate(new UpdateTagDto { Name = "视觉" }).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyName_HasError(string name)
    {
        _validator.TestValidate(new UpdateTagDto { Name = name }).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_TooLongName_HasError()
    {
        _validator.TestValidate(new UpdateTagDto { Name = new string('长', 129) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }
}
