using Cike.OS.Application.Contracts.Agents.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Agents.Validators;

public class SaveAgentToolDtoValidator : AbstractValidator<SaveAgentToolDto>
{
    public SaveAgentToolDtoValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("请选择工具绑定类型。");

        RuleFor(x => x.TargetId)
            .GreaterThan(0).WithMessage("工具绑定目标必须大于 0。");
    }
}
