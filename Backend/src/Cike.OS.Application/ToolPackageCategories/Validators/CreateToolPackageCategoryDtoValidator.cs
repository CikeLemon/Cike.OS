using Cike.OS.Application.Contracts.ToolPackageCategories.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ToolPackageCategories.Validators;

public class CreateToolPackageCategoryDtoValidator : AbstractValidator<CreateToolPackageCategoryDto>
{
    public CreateToolPackageCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("分类名称不能为空。")
            .MaximumLength(128).WithMessage("分类名称不能超过 128 个字符。");
    }
}
