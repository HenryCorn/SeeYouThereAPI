// <copyright file="Startup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api;

using Core.Configuration;
using Core.External.Amadeus;
using Core.External.Amadeus.Testing;
using Core.Infrastructure;
using Core.Interfaces;
using Core.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Web.Api.Filters;

/// <summary>
/// The startup class for configuring services and the app's request pipeline.
/// </summary>
public class Startup
{
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    /// <param name="configuration">The congifuration.</param>
    /// <param name="env">The environment.</param>
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        _env = env;
    }

    private IConfiguration Configuration { get; }

    // Configure services here
    private void ConfigureServices(IServiceCollection services)
    {
        // Add CORS support
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // Add controllers (no route prefix needed as controllers already have the /api/v1 prefix)
        services.AddControllers(options =>
        {
            // Add region validation filter to all actions
            options.Filters.Add<ValidateRegionFilter>();
        });

        // Register validation services
        services.AddSingleton<IRegionValidator, RegionValidator>();

        // Configure resilience options
        var resilienceOptions = new ResilienceOptions();
        Configuration.GetSection("Resilience").Bind(resilienceOptions);
        services.Configure<ResilienceOptions>(Configuration.GetSection("Resilience"));

        // Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "SeeYouThere API",
                Version = "v1",
                Description = "API to find the cheapest common destination for multiple travelers.",
            });
        });

        services.Configure<AmadeusOptions>(
            Configuration.GetSection("FlightSearch:Amadeus"));

        if (_env.IsDevelopment() && Configuration.GetValue("UseTestFlightData", false))
        {
            services.AddSingleton<IFlightSearchClient, TestFlightSearchClient>();
        }
        else
        {
            // Use resilient HTTP client for Amadeus API
            services.AddResilientAmadeusClient(resilienceOptions);
        }
    }

    private void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api/v1/swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api/v1/swagger/v1/swagger.json", "SeeYouThere API v1");
                c.RoutePrefix = "api/v1/swagger";
            });

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SeeYouThere API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseCors();

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
