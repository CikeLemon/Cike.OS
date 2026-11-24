using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Application.Tools.Validators;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation.TestHelper;
using ModelContextProtocol.Client;
using Xunit;

namespace Cike.OS.Tests;

public class CreateToolDtoValidatorTests
{
    private readonly CreateToolDtoValidator _validator = new();

    private static CreateToolDto ValidHttp() => new()
    {
        Code = "http-tool",
        Name = "HTTP tool",
        Type = ToolType.HttpMcp,
        ToolPackageId = 1,
        Setting = new HttpMcpToolSetting { Endpoint = "http://localhost/mcp" },
    };

    [Fact]
    public void Validate_ValidHttpTool_HasNoErrors()
    {
        _validator.TestValidate(ValidHttp()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TypeMismatch_HasError()
    {
        var dto = ValidHttp();
        dto.Type = ToolType.Skill;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_SkillWithoutDirectory_HasError()
    {
        var dto = ValidHttp();
        dto.Type = ToolType.Skill;
        dto.Setting = new SkillToolSetting { Directory = " " };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.Directory");
    }

    [Fact]
    public void Validate_HttpWithoutEndpoint_HasError()
    {
        var dto = ValidHttp();
        dto.Setting = new HttpMcpToolSetting { Endpoint = "" };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.Endpoint");
    }

    [Fact]
    public void Validate_UndefinedTransportMode_HasError()
    {
        var dto = ValidHttp();
        ((HttpMcpToolSetting)dto.Setting).TransportMode = (HttpTransportMode)999;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.TransportMode");
    }

    [Fact]
    public void Validate_NonPositiveTimeout_HasError()
    {
        var dto = ValidHttp();
        ((HttpMcpToolSetting)dto.Setting).ConnectionTimeout = TimeSpan.Zero;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting.ConnectionTimeout");
    }

    [Fact]
    public void Validate_ParameterReferenceToUndefinedCode_HasError()
    {
        var dto = ValidHttp();
        ((HttpMcpToolSetting)dto.Setting).AdditionalHeaders = new Dictionary<string, ToolValue>
        {
            ["X-Key"] = ToolValue.FromParameter("missing"),
        };

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }

    [Fact]
    public void Validate_ParameterReferenceToDefinedCode_HasNoError()
    {
        var dto = ValidHttp();
        dto.Parameters = [new ToolParameterDefinition { Code = "ak", Name = "Key" }];
        ((HttpMcpToolSetting)dto.Setting).AdditionalHeaders = new Dictionary<string, ToolValue>
        {
            ["X-Key"] = ToolValue.FromParameter("ak"),
        };

        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DuplicateParameterCodes_HasError()
    {
        var dto = ValidHttp();
        dto.Parameters =
        [
            new ToolParameterDefinition { Code = "dup", Name = "一" },
            new ToolParameterDefinition { Code = "dup", Name = "二" },
        ];

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Parameters");
    }

    [Fact]
    public void Validate_NullSetting_HasError()
    {
        var dto = ValidHttp();
        dto.Setting = null!;

        _validator.TestValidate(dto).ShouldHaveValidationErrorFor("Setting");
    }
}
