using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace HouseRentalAPI.Extensions;

/// <summary>
/// Swagger UI chỉ hiện nút chọn file khi trường multipart có schema inline
/// <c>type: string, format: binary</c>. Mặc định IFormFile được sinh dưới dạng $ref
/// nên phải thay bằng schema inline.
/// </summary>
internal sealed class FormFileOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.RequestBody?.Content is not { } content)
            return Task.CompletedTask;

        foreach (var media in content.Values)
        {
            if (media.Schema is not OpenApiSchema { Properties: { } properties } schema)
                continue;

            foreach (var name in properties.Keys.ToList())
            {
                if (properties[name] is not OpenApiSchemaReference { Reference.Id: "IFormFile" })
                    continue;

                properties[name] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(name);
            }
        }

        return Task.CompletedTask;
    }
}
