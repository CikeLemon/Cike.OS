using Cike.OS.Application.Contracts.Models.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Models.Validators;

public class CreateModelDtoValidator : AbstractValidator<CreateModelDto>
{
    public CreateModelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("请填写模型名称。")
            .MaximumLength(128).WithMessage("模型名称不能超过 128 个字符。");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("请填写显示名称。")
            .MaximumLength(128).WithMessage("显示名称不能超过 128 个字符。");

        // 描述可选：未填写落库为空字符串
        RuleFor(x => x.Description)
            .MaximumLength(2048).WithMessage("描述不能超过 2048 个字符。");

        RuleFor(x => x.TagIds)
            .NotNull().WithMessage("标签引用不能为 null。");

        // 提前给出字段级文案；聚合根工厂仍会兜底去重
        RuleForEach(x => x.TagIds)
            .GreaterThan(0).WithMessage("标签引用不合法。");
    }
}
