using Cike.OS.Application.Contracts.Tags.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Tags.Validators;

public class UpdateTagDtoValidator : AbstractValidator<UpdateTagDto>
{
    public UpdateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("请填写名称。")
            .MaximumLength(128).WithMessage("名称不能超过 128 个字符。");
    }
}
