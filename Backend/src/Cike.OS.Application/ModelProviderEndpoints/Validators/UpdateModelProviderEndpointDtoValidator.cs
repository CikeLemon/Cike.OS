using Cike.OS.Application.Contracts.ModelProviderEndpoints.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviderEndpoints.Validators;

/// <summary>
/// 接入点更新输入的校验：与新建相同的连接字段规则，但没有协议；设置子类型是否与现有协议一致由聚合裁决。
/// </summary>
public class UpdateModelProviderEndpointDtoValidator : AbstractValidator<UpdateModelProviderEndpointDto>
{
    public UpdateModelProviderEndpointDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("接入点名称不能为空。")
            .MaximumLength(128).WithMessage("接入点名称不能超过 128 个字符。");

        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("接入地址不能为空。");

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API 密钥不能为空。");

        RuleFor(x => x.Setting)
            .NotNull().WithMessage("协议专属设置不能为空。");

        RuleFor(x => x.Mappings)
            .NotNull().WithMessage("模型映射不能为 null。");

        RuleForEach(x => x.Mappings).SetValidator(new SaveModelProviderMappingDtoValidator());
    }
}
