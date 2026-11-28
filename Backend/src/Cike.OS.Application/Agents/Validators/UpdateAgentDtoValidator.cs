using Cike.OS.Application.Contracts.Agents.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Agents.Validators;

/// <summary>Agent 更新输入的校验：没有不可变字段，规则与新建相同。</summary>
public class UpdateAgentDtoValidator : AbstractValidator<UpdateAgentDto>
{
    public UpdateAgentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("请填写名称。")
            .MaximumLength(128).WithMessage("名称不能超过 128 个字符。");

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage("描述不能超过 512 个字符。");

        RuleFor(x => x.Instructions)
            .NotEmpty().WithMessage("请填写指令。");

        RuleFor(x => x.Options)
            .NotNull().WithMessage("模型选项不能为 null。")
            .SetValidator(new AgentOptionsValidator());

        RuleFor(x => x.Tools)
            .NotNull().WithMessage("工具列表不能为 null。");

        RuleForEach(x => x.Tools).SetValidator(new SaveAgentToolDtoValidator());
    }
}
