using Cike.OS.Application.Contracts.Tools.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Tools.Validators;

/// <summary>
/// 更新只校验可变字段与设置自身；设置与存储形态是否匹配由聚合根在 Update 时断言（形态创建后不可变）。
/// </summary>
public class UpdateToolDtoValidator : AbstractValidator<UpdateToolDto>
{
    public UpdateToolDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("工具编码不能为空。")
            .MaximumLength(64).WithMessage("工具编码不能超过 64 个字符。");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("工具名称不能为空。")
            .MaximumLength(128).WithMessage("工具名称不能超过 128 个字符。");

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage("工具描述不能超过 512 个字符。");

        RuleFor(x => x.ToolPackageId)
            .GreaterThan(0).WithMessage("必须指定工具所属的工具包。");

        RuleFor(x => x.Setting)
            .NotNull().WithMessage("工具设置不能为空。");

        RuleFor(x => x.Parameters)
            .NotNull().WithMessage("参数定义不能为 null。");

        RuleFor(x => x).Custom((dto, context) =>
        {
            ToolSettingRules.ValidateParameters(dto.Parameters, context.AddFailure);
            ToolSettingRules.Validate(dto.Setting, dto.Parameters, expectedType: null, context.AddFailure);
        });
    }
}
