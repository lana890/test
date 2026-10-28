using EskaDCMSActiveDirectory.Extensions;
using EskaDCMSActiveDirectory.Interfaces;
using EskaDCMSActiveDirectory.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowEskaIntranet", policy =>
    {
        policy.WithOrigins("https://intranet.eskadenia.com", "http://intranet.eskadenia.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddHttpContextAccessor();
// Required so AdUserProvider can resolve IHttpClientFactory (replaces per-call `new HttpClient()`
// which caused socket exhaustion). IHttpClientFactory pools handlers across requests.
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAdUserProvider, AdUserProvider>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Eskadenia DCMS AD API", Version = "v1" });
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
    RequireHeaderSymmetry = false,
    ForwardLimit = null,
    KnownNetworks = { },
    KnownProxies = { }
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowEskaIntranet");

app.UseAuthentication();
app.UseAuthorization();


app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Eskadenia DCMS AD API V1");
    c.RoutePrefix = "swagger"; // Swagger UI at /swagger
});

app.UseJsonApiWrapper("1.0", "API Mapper");

app.MapControllers();



app.Run();
