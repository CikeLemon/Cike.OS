using Cike.OS.Application.Contracts.Models.Dtos;
using Cike.OS.Application.Models.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Cike.OS.Tests;

public class CreateModelDtoValidatorTests
{
    private readonly CreateModelDtoValidator _validator = new();

    private static CreateModelDto Valid() => new()
    {
        Name = "glm-5.3",
        DisplayName = "GLM 5.3",
        Description = "智谱旗舰模型",
        TagIds = [],
    };

    [Fact]
    public void Validate_ValidModel_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var dto = Valid();
        dto.Name = " ";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_EmptyDisplayName_HasError()
    {
        var dto = Valid();
        dto.DisplayName = "";

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Validate_EmptyDescription_HasNoError()
    {
        // 描述可选：未填写落库为空字符串
        var dto = Valid();
        dto.Description = "";

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TooLongDescription_HasError()
    {
        var dto = Valid();
        dto.Description = new string('长', 2049);

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NullTagIds_HasError()
    {
        var dto = Valid();
        dto.TagIds = null!;

        // RuleForEach 对 null 集合不报错，必须靠 NotNull 拦下，否则 Handler 会 NRE
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.TagIds);
    }

    [Fact]
    public void Validate_NonPositiveTagId_HasError()
    {
        var dto = Valid();
        dto.TagIds = [1, 0];

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("TagIds[1]");
    }
}
