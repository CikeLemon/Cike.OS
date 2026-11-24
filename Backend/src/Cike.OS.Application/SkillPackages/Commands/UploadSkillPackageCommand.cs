using Cike.Cqrs.Commands;

namespace Cike.OS.Application.SkillPackages.Commands;

public record UploadSkillPackageCommand(Stream Content) : Command
{
    public string? Directory { get; set; }
}
