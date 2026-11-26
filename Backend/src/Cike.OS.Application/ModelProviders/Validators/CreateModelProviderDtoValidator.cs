using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

public class CreateModelProviderDtoValidator : AbstractValidator<CreateModelProviderDto>
{
    public CreateModelProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("模型提供方名称不能为空。")
            .MaximumLength(128).WithMessage("模型提供方名称不能超过 128 个字符。");
    }
}
