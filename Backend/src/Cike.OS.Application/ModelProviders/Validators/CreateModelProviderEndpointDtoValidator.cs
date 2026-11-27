using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>
/// 接入点新建输入的校验。接入点重名、接入点 Id 是否在提供方名下、映射的模型是否存在，由 Handler 与聚合裁决；
/// 设置子类型是否与该接入点现有协议一致（更新时没有协议可比），由聚合裁决。
/// </summary>
public class CreateModelProviderEndpointDtoValidator : AbstractValidator<CreateModelProviderEndpointDto>
{
    public CreateModelProviderEndpointDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("接入点名称不能为空。")
            .MaximumLength(128).WithMessage("接入点名称不能超过 128 个字符。");

        RuleFor(x => x.Protocol)
            .IsInEnum().WithMessage("请选择协议。");

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("接入地址不能为空。");

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API 密钥不能为空。");

        // 协议专属字段（Organization / Version）均可选，不设必填；设置的子类型必须与协议一致
        RuleFor(x => x).Custom((dto, context) =>
        {
            ModelProviderEndpointSettingRules.Validate(dto.Protocol, dto.Setting, context.AddFailure);
        });

        RuleFor(x => x.Mappings)
            .NotNull().WithMessage("模型映射不能为 null。");

        RuleForEach(x => x.Mappings).SetValidator(new SaveModelProviderMappingDtoValidator());
    }
}
