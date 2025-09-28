// <copyright file="Startup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api;

using Core.Configuration;
using Core.External.Amadeus;
using Core.External.Amadeus.Testing;
using Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

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

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        services.AddControllers();
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
            services.AddHttpClient<IFlightSearchClient, AmadeusFlightSearchClient>();
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
