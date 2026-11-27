using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>模型提供方下一条模型的校验。标签引用是否存在，由 Handler 查库裁决。</summary>
public class SaveModelProviderModelDtoValidator : AbstractValidator<SaveModelProviderModelDto>
{
    public SaveModelProviderModelDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("请填写模型名称。")
            .MaximumLength(128).WithMessage("模型名称不能超过 128 个字符。");

        // 描述可选：未填写落库为空字符串
        RuleFor(x => x.Description)
            .MaximumLength(2048).WithMessage("描述不能超过 2048 个字符。");

        RuleFor(x => x.TagIds)
            .NotNull().WithMessage("标签引用不能为 null。");

        RuleForEach(x => x.TagIds)
            .GreaterThan(0).WithMessage("标签引用不合法。");
    }
}
