using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

public class UpdateModelProviderDtoValidator : AbstractValidator<UpdateModelProviderDto>
{
    public UpdateModelProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("提供商名称不能为空。")
            .MaximumLength(128).WithMessage("提供商名称不能超过 128 个字符。");

        RuleFor(x => x).Custom((dto, context) =>
        {
            ModelProviderSettingRules.Validate(dto.Setting, context.AddFailure);
        });

        // 映射的模型是否存在、同一提供方下是否重复，由 Handler 与聚合裁决
        RuleFor(x => x.Mappings)
            .NotNull().WithMessage("模型映射不能为 null。");

        RuleForEach(x => x.Mappings).ChildRules(mapping =>
        {
            mapping.RuleFor(x => x.ModelId)
                .GreaterThan(0).WithMessage("必须选择一个模型。");

            mapping.RuleFor(x => x.ProviderModelName)
                .MaximumLength(128).WithMessage("提供商侧模型名不能超过 128 个字符。");
        });
    }
}
