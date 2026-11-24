using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cike.OS.Service.Open.Swagger;

/// <summary>
/// 标记端点方法接收一个 multipart/form-data 文件字段；由 <see cref="SwaggerFormFileOperationFilter"/> 生成为 requestBody。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SwaggerFormFileAttribute(string fieldName, string description) : Attribute
{
    public string FieldName { get; } = fieldName;

    public string Description { get; } = description;
}

/// <summary>
/// 为手动读取表单的端点补回 multipart/form-data 的 Swagger 契约，使 gen:api 能生成带文件 body 的函数。
/// </summary>
public class SwaggerFormFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var attribute = context.MethodInfo?.GetCustomAttributes(typeof(SwaggerFormFileAttribute), false)
            .OfType<SwaggerFormFileAttribute>()
            .FirstOrDefault();
        if (attribute == null)
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Description = attribute.Description,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Required = new HashSet<string> { attribute.FieldName },
                        Properties =
                        {
                            [attribute.FieldName] = new OpenApiSchema { Type = "string", Format = "binary" },
                        },
                    },
                },
            },
        };
    }
}
