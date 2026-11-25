using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cike.OS.Service.Open.Swagger;

/// <summary>
/// Swashbuckle 合成的多态判别键属性固定是 string，而 int 判别值（如 ToolSetting 的 ToolType 数值）
/// 实际序列化为 JSON 数字；这里把判别键属性改成 integer + 枚举值，使 gen:api 生成数字字面量判别联合。
/// </summary>
public class JsonPolymorphicDiscriminatorSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var baseType = JsonPolymorphicHelper.GetBaseType(context.Type, out var attr);
        var intValues = baseType?.GetCustomAttributes<JsonDerivedTypeAttribute>(false)
            .Where(a => a.TypeDiscriminator is int)
            .Select(a => (int)a.TypeDiscriminator!)
            .ToList();

        if (attr == null || intValues is not { Count: > 0 })
        {
            return;
        }

        var propertyName = attr.TypeDiscriminatorPropertyName ?? "$type";
        if (schema.Properties?.TryGetValue(propertyName, out var discriminator) == true
            && discriminator is OpenApiSchema property)
        {
            property.Type = JsonSchemaType.Integer;
            property.Format = "int32";
            property.Enum = intValues.Select(v => (JsonNode)JsonValue.Create(v)).ToList();
        }
    }
}

public static class JsonPolymorphicHelper
{
    public static Type? GetBaseType(Type type, out JsonPolymorphicAttribute? polymorphicAttr)
    {
        var baseType = type;

        while (baseType != null && baseType != typeof(object))
        {
            var attr = baseType.GetCustomAttribute<JsonPolymorphicAttribute>(false);
            if (attr != null)
            {
                polymorphicAttr = attr;
                return baseType;
            }

            baseType = baseType.BaseType!;
        }

        polymorphicAttr = null;
        return null;
    }
}
