using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EskaCMS.WebHost.Configurations
{
    public class CustomHeaderOperationFilter : IOperationFilter 
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            var siteIdParameter = operation.Parameters.FirstOrDefault(p => p.Name.Equals("SiteId", StringComparison.OrdinalIgnoreCase));
            if (siteIdParameter != null)
                operation.Parameters.Remove(siteIdParameter);

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "siteId",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "Long",

                }
            });
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "cultureId",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "Long",

                }
            });
        }
    }
}
