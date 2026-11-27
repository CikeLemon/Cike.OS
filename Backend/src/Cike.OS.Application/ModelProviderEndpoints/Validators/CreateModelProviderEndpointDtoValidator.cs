using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using Cike.OS.Domain.Shared.Enums;
using Cike.OS.Domain.Shared.ValueObjects;
using FluentValidation;

namespace Cike.OS.Application.ModelProviderEndpoints.Validators;

/// <summary>
/// 接入点新建输入的校验。所属提供方是否存在、接入点重名、映射的模型是否存在，由 Handler 查库裁决。
/// </summary>
public class CreateModelProviderEndpointDtoValidator : AbstractValidator<CreateModelProviderEndpointDto>
{
    public CreateModelProviderEndpointDtoValidator()
    {
        RuleFor(x => x.ModelProviderId)
            .GreaterThan(0).WithMessage("必须选择所属的模型提供方。");

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
