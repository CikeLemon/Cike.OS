using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using Cike.OS.Application.UserToolConfigurations.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Cike.OS.Tests;

public class SaveUserToolConfigurationsDtoValidatorTests
{
    private readonly SaveUserToolConfigurationsDtoValidator _validator = new();

    private static SaveUserToolConfigurationDto Item(long toolId, string value = "v") => new()
    {
        ToolId = toolId,
        Values = new Dictionary<string, string> { ["ak"] = value },
    };

    private static List<SaveUserToolConfigurationDto> Batch(params SaveUserToolConfigurationDto[] items) => [.. items];

    [Fact]
    public void Validate_ValidBatch_HasNoErrors()
    {
        _validator.TestValidate(Batch(Item(1), Item(2))).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyBatch_HasError()
    {
        Assert.NotEmpty(_validator.TestValidate(Batch()).Errors);
    }

    [Fact]
    public void Validate_DuplicateTool_HasError()
    {
        Assert.NotEmpty(_validator.TestValidate(Batch(Item(1), Item(1))).Errors);
    }

    [Fact]
    public void Validate_ItemFollowsSingleSaveRules()
    {
        // 单项规则沿用单条保存的 Validator：工具 Id 须大于 0，参数值不能为空白
        var toolId = _validator.TestValidate(Batch(Item(0)));
        Assert.Contains(toolId.Errors, x => x.PropertyName.EndsWith("ToolId"));

        var values = _validator.TestValidate(Batch(Item(1, " ")));
        Assert.Contains(values.Errors, x => x.PropertyName.EndsWith("Values"));
    }
}
