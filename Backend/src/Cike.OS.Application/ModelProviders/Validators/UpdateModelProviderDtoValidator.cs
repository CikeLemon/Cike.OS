using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

public class UpdateModelProviderDtoValidator : AbstractValidator<UpdateModelProviderDto>
{
    public UpdateModelProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("模型提供方名称不能为空。")
            .MaximumLength(128).WithMessage("模型提供方名称不能超过 128 个字符。");

        // 接入点重名、协议不可变、带 Id 的接入点是否存在，由聚合裁决
        RuleFor(x => x.Endpoints)
            .NotNull().WithMessage("接入点不能为 null。");

        RuleForEach(x => x.Endpoints).SetValidator(new SaveModelProviderEndpointDtoValidator());
    }
}
