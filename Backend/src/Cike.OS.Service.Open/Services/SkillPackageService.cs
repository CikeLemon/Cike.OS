using Cike.AspNetCore.MinimalAPIs;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.SkillPackages.Dtos;
using Cike.OS.Application.SkillPackages.Commands;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

public class SkillPackageService : MinimalApiServiceBase
{
    [IgnoreAntiforgeryToken]
    [MinimalApiRoute("/api/v1/SkillPackages/Upload", httpMethods: ["Post"])]
    public async Task<Ok<UploadSkillPackageResultDto>> UploadAsync(
        [FromServices] ILocalEventBus eventBus,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await using var content = file.OpenReadStream();
        var command = new UploadSkillPackageCommand(content);
        await eventBus.PublishAsync(command, cancellationToken);

        return TypedResults.Ok(new UploadSkillPackageResultDto { Directory = command.Directory ?? string.Empty });
    }
}
