// <copyright file="Startup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api
{
    using Core.Configuration;
    using Core.External;
    using Core.External.Amadeus;
    using Core.External.Amadeus.Testing;
    using Core.Infrastructure;
    using Core.Interfaces;
    using Core.Validation;
    using FluentValidation;
    using FluentValidation.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Scrutor;
    using Web.Api.Filters;
    using Web.Api.Infrastructure;
    using Web.Api.Middleware;
    using Web.Api.Services;
    using Web.Api.Validators;

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

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
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

            // Add health checks
            services.AddHealthChecks();

            // Add OpenTelemetry
            services.AddOpenTelemetry(Configuration);

            // Add API rate limiting
            services.AddApiRateLimiting(options =>
            {
                Configuration.GetSection("RateLimit").Bind(options);
            });

            // Add controllers with JSON options for problem details
            services.AddControllers(options =>
            {
                // Add region validation filter to all actions
                options.Filters.Add<ValidateRegionFilter>();
                // Add problem details exception filter
                options.Filters.Add<ProblemDetailsExceptionFilter>();
            })
            .AddNewtonsoftJson()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = false;
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetailsFactory = context.HttpContext.RequestServices
                        .GetRequiredService<ProblemDetailsFactory>();

                    var validationProblemDetails = problemDetailsFactory
                        .CreateValidationProblemDetails(
                            context.HttpContext,
                            context.ModelState,
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "One or more validation errors occurred",
                            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                            detail: "See the errors field for details.");

                    return new BadRequestObjectResult(validationProblemDetails)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

            // Override the default problem details factory with our custom one
            services.AddSingleton<ProblemDetailsFactory, ValidationProblemDetailsFactory>();

            // Register validation services
            services.AddSingleton<IRegionValidator, RegionValidator>();

            // Add FluentValidation
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<CheapestDestinationRequestValidator>();

            // Configure resilience options
            var resilienceOptions = new ResilienceOptions();
            Configuration.GetSection("Resilience").Bind(resilienceOptions);
            services.Configure<ResilienceOptions>(Configuration.GetSection("Resilience"));

            // Configure cache options
            services.Configure<CacheOptions>(Configuration.GetSection("Cache"));

            // Add memory cache
            services.AddMemoryCache();

            // Add HTTP context accessor for accessing Cache-Control headers
            services.AddHttpContextAccessor();

            // Register the cache headers service
            services.AddScoped<ICacheHeadersService, HttpContextCacheHeadersService>();

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

                // Include XML comments if they exist
                var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (System.IO.File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Include schema annotations from FluentValidation
                c.SchemaFilter<FluentValidationSchemaFilter>();
            });

            services.Configure<AmadeusOptions>(
                Configuration.GetSection("FlightSearch:Amadeus"));

            if (_env.IsDevelopment() && Configuration.GetValue("UseTestFlightData", false))
            {
                // Register the test flight search client
                services.AddSingleton<IFlightSearchClient, TestFlightSearchClient>();
            }
            else
            {
                // Use resilient HTTP client for Amadeus API
                services.AddResilientAmadeusClient(resilienceOptions);
            }

            // Decorate the IFlightSearchClient with caching functionality
            services.Decorate<IFlightSearchClient>((inner, provider) =>
            {
                return new CachedFlightSearchClient(
                    inner,
                    provider.GetRequiredService<IMemoryCache>(),
                    provider.GetRequiredService<IOptions<CacheOptions>>(),
                    provider.GetRequiredService<ICacheHeadersService>(),
                    provider.GetRequiredService<ILogger<CachedFlightSearchClient>>());
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
            }

            app.UseHttpsRedirection();

            // Add OpenTelemetry middleware early in the pipeline
            app.UseOpenTelemetryMetrics();

            app.UseCorrelationId();

            // Add rate limiting middleware early in the pipeline
            app.UseApiRateLimiting();

            app.UseHttpCacheHeaders();

            app.UseRouting();
            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                // Map health check endpoint
                endpoints.MapHealthChecks("/health");
            });
        }
    }
}
