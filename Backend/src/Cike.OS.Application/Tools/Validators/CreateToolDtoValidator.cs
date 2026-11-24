using Cike.OS.Application.Contracts.Tools.Dtos;
using Cike.OS.Domain.Shared.Enums;
using FluentValidation;

namespace Cike.OS.Application.Tools.Validators;

public class CreateToolDtoValidator : AbstractValidator<CreateToolDto>
{
    public CreateToolDtoValidator()
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

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("工具形态不合法。");

        RuleFor(x => x.Setting)
            .NotNull().WithMessage("工具设置不能为空。");

        RuleFor(x => x.Parameters)
            .NotNull().WithMessage("参数定义不能为 null。");

        // 形态与设置的多态类型必须一一对应，并校验各形态的必填字段与参数引用
        RuleFor(x => x).Custom((dto, context) =>
        {
            ToolSettingRules.ValidateParameters(dto.Parameters, context.AddFailure);
            ToolSettingRules.Validate(dto.Setting, dto.Parameters, dto.Type, context.AddFailure);
        });
    }
}
