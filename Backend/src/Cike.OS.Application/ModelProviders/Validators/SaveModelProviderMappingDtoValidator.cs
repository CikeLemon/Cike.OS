using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>提供商映射的输入项：以 ModelId 为键随接入点整体提交，没有独立接口。模型是否存在、同一接入点下是否重复，由 Handler 与聚合裁决。</summary>
public class SaveModelProviderMappingDtoValidator : AbstractValidator<SaveModelProviderMappingDto>
{
    public SaveModelProviderMappingDtoValidator()
    {
        RuleFor(x => x.ModelId)
            .GreaterThan(0).WithMessage("必须选择一个模型。");

        RuleFor(x => x.ProviderModelName)
            .MaximumLength(128).WithMessage("提供商侧模型名不能超过 128 个字符。");
    }
}
