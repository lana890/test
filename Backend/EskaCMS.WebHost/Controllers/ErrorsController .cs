using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace EskaCMS.WebHost.Controllers
{
    public class ErrorController : ControllerBase
    {
        [HttpGet]
        [Route("/error-local-development")]
        public IActionResult ErrorLocalDevelopment(
            [FromServices] IWebHostEnvironment webHostEnvironment)
        {
            if (webHostEnvironment.EnvironmentName != "Development")
            {
                throw new InvalidOperationException(
                    "This shouldn't be invoked in non-development environments.");
            }

            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return Problem(
                detail: "An unexpected error occurred while processing the request.",
                title: context?.Error?.Message);
        }

        [HttpGet]
        [Route("/error")]
        public IActionResult Error() => Problem();
    }
}
