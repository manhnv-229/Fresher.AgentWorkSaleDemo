using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Demo.Api.Filters;

public sealed class FormFileUploadFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormFileCollection))
            .ToList();

        if (formFileParameters.Count == 0)
        {
            return;
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>()
                    },
                    Encoding = new Dictionary<string, OpenApiEncoding>()
                }
            }
        };

        var schema = operation.RequestBody.Content["multipart/form-data"].Schema!;

        foreach (var parameter in context.ApiDescription.ParameterDescriptions.Where(p => p.Source == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Form))
        {
            var parameterName = parameter.Name ?? parameter.ModelMetadata?.Name;
            if (string.IsNullOrEmpty(parameterName))
            {
                continue;
            }

            var isFile = parameter.Type == typeof(IFormFile) || parameter.Type == typeof(IFormFileCollection);
            schema.Properties[parameterName] = new OpenApiSchema
            {
                Type = isFile ? "string" : "string",
                Format = isFile ? "binary" : null
            };
        }
    }
}