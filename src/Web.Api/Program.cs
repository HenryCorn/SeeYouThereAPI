// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Web.Api;

using Microsoft.AspNetCore.Server.Kestrel.Core;

/// <summary>
/// The main entry point for the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args"> The arguments.</param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(5087, o => o.Protocols = HttpProtocols.Http1AndHttp2);
                    options.ListenLocalhost(7180, o =>
                    {
                        o.Protocols = HttpProtocols.Http1AndHttp2;
                        o.UseHttps();
                    });
                });
            });
}
