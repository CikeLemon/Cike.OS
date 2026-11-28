using Cike.OS.Application.Contracts.Agents.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Agents.Validators;

/// <summary>
/// Agent 新建输入的校验（规则已逐条经用户确认）。名称是否重名、引用的工具包 / 工具是否存在由 Handler 查库裁决；
/// 提交的工具条目是否重复，由聚合对着已加载的集合断言。
/// </summary>
public class CreateAgentDtoValidator : AbstractValidator<CreateAgentDto>
{
    public CreateAgentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("请填写名称。")
            .MaximumLength(128).WithMessage("名称不能超过 128 个字符。");

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage("描述不能超过 512 个字符。");

        // 指令是 Agent 的自我介绍与提示词，必填、不限长度
        RuleFor(x => x.Instructions)
            .NotEmpty().WithMessage("请填写指令。");

        RuleFor(x => x.Options)
            .NotNull().WithMessage("模型选项不能为 null。")
            .SetValidator(new AgentOptionsValidator());

        // 可以先建一个只会聊天的 Agent：空列表合法
        RuleFor(x => x.Tools)
            .NotNull().WithMessage("工具列表不能为 null。");

        RuleForEach(x => x.Tools).SetValidator(new SaveAgentToolDtoValidator());
    }
}
