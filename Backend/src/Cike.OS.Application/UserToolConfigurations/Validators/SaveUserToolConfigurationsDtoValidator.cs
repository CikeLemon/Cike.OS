using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using FluentValidation;

namespace Cike.OS.Application.UserToolConfigurations.Validators;

/// <summary>批量保存的校验：列表不能为空、工具不能重复；每一项沿用单条保存的规则。必填参数是否齐全由解析器在用例里裁决。</summary>
public class SaveUserToolConfigurationsDtoValidator : AbstractValidator<List<SaveUserToolConfigurationDto>>
{
    public SaveUserToolConfigurationsDtoValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("至少提交一个工具的配置。");

        RuleFor(x => x)
            .Must(x => x == null || x.Select(item => item.ToolId).Distinct().Count() == x.Count)
            .WithMessage("同一个工具在列表里只能出现一次。");

        RuleForEach(x => x).SetValidator(new SaveUserToolConfigurationDtoValidator());
    }
}
