using Cike.OS.Application.Contracts.UserToolConfigurations.Dtos;
using FluentValidation;

namespace Cike.OS.Application.UserToolConfigurations.Validators;

public class SaveUserToolConfigurationDtoValidator : AbstractValidator<SaveUserToolConfigurationDto>
{
    public SaveUserToolConfigurationDtoValidator()
    {
        RuleFor(x => x.ToolId)
            .GreaterThan(0).WithMessage("工具Id必须大于0。");

        RuleFor(x => x.Values)
            .NotNull().WithMessage("参数值不能为空。");

        RuleFor(x => x.Values.Keys)
            .Must(keys => keys.All(key => !string.IsNullOrWhiteSpace(key)))
            .When(x => x.Values != null)
            .WithMessage("参数名不能为空白。");

        RuleFor(x => x.Values.Values)
            .Must(values => values.All(value => !string.IsNullOrWhiteSpace(value)))
            .When(x => x.Values != null)
            .WithMessage("参数值不能为空白。");
    }
}
