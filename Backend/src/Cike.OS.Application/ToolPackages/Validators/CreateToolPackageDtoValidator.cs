using Cike.OS.Application.Contracts.ToolPackages.Dtos;
using FluentValidation;

namespace Cike.OS.Application.ToolPackages.Validators;

public class CreateToolPackageDtoValidator : AbstractValidator<CreateToolPackageDto>
{
    public CreateToolPackageDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("工具包编码不能为空。")
            .MaximumLength(64).WithMessage("工具包编码不能超过 64 个字符。");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("工具包名称不能为空。")
            .MaximumLength(128).WithMessage("工具包名称不能超过 128 个字符。");

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage("工具包描述不能超过 512 个字符。");

        RuleFor(x => x.Icon)
            .MaximumLength(256).WithMessage("工具包图标不能超过 256 个字符。");

        RuleFor(x => x.ToolPackageCategoryId)
            .GreaterThan(0).WithMessage("必须选择一个工具包分类。");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("工具包状态不合法。");
    }
}
