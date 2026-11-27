using Cike.OS.Application.Contracts.ModelProviders.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ModelProviders.Validators;

/// <summary>
/// 模型提供方新建输入的校验。名称是否重名、模型引用的标签是否存在，由 Handler 查库裁决；
/// 提交的模型列表里名称是否重复，由聚合对着已加载的集合断言。
/// </summary>
public class CreateModelProviderDtoValidator : AbstractValidator<CreateModelProviderDto>
{
    public CreateModelProviderDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("模型提供方名称不能为空。")
            .MaximumLength(128).WithMessage("模型提供方名称不能超过 128 个字符。");

        RuleFor(x => x.Protocol)
            .IsInEnum().WithMessage("请选择协议。");

        // 供应商的兼容地址写法不统一（有的带 /v1、有的不带），只校验必填、不校验 URL 格式
        RuleFor(x => x.BaseUrl)
            .NotEmpty().WithMessage("接入地址不能为空。");

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API 密钥不能为空。");

        // 协议专属字段（Organization / Version）均可选，不设必填；设置的子类型必须与协议一致
        RuleFor(x => x).Custom((dto, context) =>
        {
            ModelProviderSettingRules.Validate(dto.Protocol, dto.Setting, context.AddFailure);
        });

        // 可以先建连接、后补模型：空列表合法
        RuleFor(x => x.Models)
            .NotNull().WithMessage("模型列表不能为 null。");

        RuleForEach(x => x.Models).SetValidator(new SaveModelProviderModelDtoValidator());
    }
}
