using Cike.OS.Domain.Shared.ValueObjects;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cike.OS.Service.Open.Swagger;

/// <summary>
/// Swashbuckle 为多态基类合成的判别键属性默认是 string，而 [JsonDerivedType] 用的是 ToolType 数值；
/// 这里把 <see cref="ToolSetting"/> 的 <c>type</c> 改成 integer，使生成的 TS 类型是数字字面量而非字串。
/// </summary>
public class ToolSettingDiscriminatorSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(ToolSetting)
            && schema.Properties.TryGetValue("type", out var discriminator))
        {
            discriminator.Type = "integer";
            discriminator.Format = "int32";
            discriminator.Enum = [new OpenApiInteger(1), new OpenApiInteger(2), new OpenApiInteger(3)];
        }
    }
}
