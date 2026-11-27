using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>提供方只管自己：更新输入只有名称。</summary>
public class UpdateModelProviderDtoValidator : AbstractValidator<UpdateModelProviderDto>
{
    public UpdateModelProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("模型提供方名称不能为空。")
            .MaximumLength(128).WithMessage("模型提供方名称不能超过 128 个字符。");
    }
}
