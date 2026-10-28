using EskaDCMSActiveDirectory.Models;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaDCMSActiveDirectory.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseAdMiddleware(this IApplicationBuilder builder) =>
            builder.UseMiddleware<AdUserMiddleware>();
    }
}

