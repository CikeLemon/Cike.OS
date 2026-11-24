using Cike.AspNetCore.MinimalAPIs;
using Cike.Core.Exceptions;
using Cike.EventBus.Local;
using Cike.OS.Application.Contracts.SkillPackages.Dtos;
using Cike.OS.Application.SkillPackages.Commands;
using Cike.OS.Service.Open.Swagger;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cike.OS.Service.Open.Services;

/// <summary>
/// 技能包 zip 上传：独立于工具表单提交，只负责把压缩包落到存储并返回相对目录路径。
/// 表单在宿主层手动读取（<see cref="IFormFile"/> 参数绑定会要求防伪造中间件，框架约定路由下无法可靠关闭），
/// Swagger 契约由 <see cref="SwaggerFormFileOperationFilter"/> 补回。
/// </summary>
public class SkillPackageService : MinimalApiServiceBase
{
    public const string FileFieldName = "file";

    [SwaggerFormFile(FileFieldName, "技能包 zip 压缩包")]
    public async Task<Ok<UploadSkillPackageResultDto>> UploadAsync(
        [FromServices] ILocalEventBus eventBus,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken = default)
    {
        var request = httpContextAccessor.HttpContext?.Request
            ?? throw new UserFriendlyException("无效的上传请求。");

        if (!request.HasFormContentType)
        {
            throw new UserFriendlyException("请使用 multipart/form-data 上传技能包。");
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            // 超过 multipart 上限等表单读取失败，转成可读的业务提示
            throw new UserFriendlyException($"读取上传文件失败：{exception.Message}");
        }

        var file = form.Files.GetFile(FileFieldName)
            ?? throw new UserFriendlyException($"未收到技能包文件（字段名 {FileFieldName}）。");

        await using var content = file.OpenReadStream();
        var command = new UploadSkillPackageCommand(content);
        await eventBus.PublishAsync(command, cancellationToken);

        return TypedResults.Ok(new UploadSkillPackageResultDto { Directory = command.Directory ?? string.Empty });
    }
}
