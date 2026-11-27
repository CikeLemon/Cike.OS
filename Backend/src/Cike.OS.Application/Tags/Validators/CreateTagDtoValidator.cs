using Cike.OS.Application.Contracts.Tags.Dtos;
using FluentValidation;

namespace Cike.OS.Application.Tags.Validators;

public class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    // 小写字母开头，其后只允许小写字母、数字、- 和 _；不收大写，省掉大小写归一
    private const string KeyPattern = @"^[a-z][a-z0-9_-]*$";

    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("请填写键。")
            .Matches(KeyPattern).WithMessage("键只能由小写字母、数字、- 和 _ 组成，且以字母开头。")
            .MaximumLength(64).WithMessage("键不能超过 64 个字符。");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("请填写名称。")
            .MaximumLength(128).WithMessage("名称不能超过 128 个字符。");
    }
}
