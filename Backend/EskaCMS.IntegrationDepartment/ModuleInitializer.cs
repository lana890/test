using EskaCMS.Infrastructure.Modules;
using EskaCMS.IntegrationDepartment.ServerFlows;
using EskaCMS.IntegrationDepartment.ServerFlows.Controllers;
using EskaCMS.IntegrationDepartment.ServerFlows.Execution;
using EskaCMS.IntegrationDepartment.ServerFlows.Services;
using EskaCMS.Integrations.Services.InsuranceInterfaces;
using EskaCommerce.Orders.Events;
using MediatR;
using EskaCMS.Integrations.Services.Interfaces;
using EskaCMS.Integrations.Services.JIF;
using EskaCMS.Integrations.Services.JoClaim;
using EskaCMS.Integrations.Services.JoClaim.Interface;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.IntegrationDepartment
{
	public class ModuleInitializer : IModuleInitializer
	{
		public void ConfigureServices(IServiceCollection services)
		{

            services.AddHttpClient<GeneralApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddTransient<IGeneralApiClient, GeneralApiClient>();

            services.AddHttpClient<MedicalApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddTransient<IMedicalApiClient, MedicalApiClient>();

            services.AddHttpClient<FinancialApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(40);
            });
            services.AddTransient<IFinancialApiClient, FinancialApiClient>();

        
            services.AddScoped<IIntegrationServices, IntegrationService>();

			services.AddScoped<IJIFService, JIFService>();

            services.AddScoped<IJoClaimService, JoClaimService>();

            AddServerFlows(services);
		}

        /// <summary>
        /// Server flows: apiGroupList configs the server runs on its own, started after a payment, by
        /// a page, on a schedule, by another system (webhook) or by an order's status. Off until
        /// ServerFlows:Enabled is true in appsettings.json.
        /// </summary>
        private static void AddServerFlows(IServiceCollection services)
        {
            services.AddOptions<ServerFlowOptions>().BindConfiguration(ServerFlowOptions.SectionName);

            services.AddSingleton<ServerFlowQueue>();
            services.AddSingleton<ServerFlowSignals>();
            services.AddSingleton<ServerFlowCatalog>();
            services.AddSingleton<FlowCaller>();
            services.AddSingleton<ServerFlowAlerts>();
            services.AddSingleton<ServerFlowRunner>();
            services.AddSingleton<ServerFlowTriggers>();
            services.AddSingleton<ServerFlowSchedules>();
            services.AddSingleton<ServerFlowOrderStatuses>();
            services.AddSingleton<ServerFlowRetention>();
            services.AddSingleton<ServerFlowTexts>();
            services.AddSingleton<ServerFlowAnswers>();
            services.AddSingleton<ServerFlowPageRuns>();
            services.AddSingleton<ServerFlowWebhookCalls>();
            services.AddSingleton<ServerFlowWebhooks>();

            services.AddScoped<FlowContextBuilder>();
            services.AddScoped<ServerFlowUserOperations>();
            services.AddScoped<ServerFlowOrderOperations>();
            services.AddScoped<ServerFlowAdminService>();
            services.AddScoped<ServerFlowAdminTools>();
            services.AddScoped<ServerFlowTryIt>();
            services.AddScoped<ServerFlowAdminFilter>();

            services.AddTransient<INotificationHandler<OrderChanged>, OrderPaidServerFlowHandler>();
            services.AddHostedService<ServerFlowWorker>();
        }

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{}
	}
}
