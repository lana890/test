using EskaDCMSActiveDirectory.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EskaDCMSActiveDirectory.Models
{
    public class AdUserMiddleware
    {
        private readonly RequestDelegate next;

        public AdUserMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, IAdUserProvider userProvider, IConfiguration config)
        {
            try
            {
                if (!(userProvider.Initialized))
                {
                    await userProvider.Create(context, config);
                }
                await next(context);
            }

            catch (Exception)
            {

                await next(context);

            }




        }
    }
}
