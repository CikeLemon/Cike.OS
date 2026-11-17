using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Validators;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace Cike.OS.Tests;

public class SaveUserToolConfigurationDtoValidatorTests
{
    private readonly SaveUserToolConfigurationDtoValidator _validator = new();

    [Fact]
    public void Validate_NullValues_DoesNotThrow()
    {
        var dto = new SaveUserToolConfigurationDto
        {
            ToolId = 1,
            Values = null!,
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Values);
    }
}
