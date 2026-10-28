using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCommerce.HangfireJobs.Models
{
    public abstract class BackgroundJob<TArgs> : IBackgroundJob<TArgs>
    {
        public ILogger<BackgroundJob<TArgs>> Logger { get; set; }

        protected BackgroundJob()
        {
            Logger = NullLogger<BackgroundJob<TArgs>>.Instance;
        }

        public abstract void Execute(TArgs args);
    }
}
