using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace ValueCards
{
  public class Program
  {
    static IConfigurationRoot configuration;

    public static int Main(string[] args)
    {
      var path = AppDomain.CurrentDomain.BaseDirectory;

      configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

      var logFileTemplate = Path.Combine(path, "Logs", "log.txt");
      var outputTemplate = @"===> [{Timestamp:HH:mm:ss} {Level} {SourceContext}] {Message:lj}{NewLine}{Exception}";
      var logConfiguration = new LoggerConfiguration().ReadFrom.Configuration(configuration);
      logConfiguration.WriteTo.File(logFileTemplate, outputTemplate: outputTemplate, rollingInterval: RollingInterval.Day);
      Log.Logger = logConfiguration.CreateLogger();
      var directoryPath = @"C:/ValueCardReports";
      if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
      try
      {
        Log.Information("Starting web host");
        CreateHostBuilder(args).Build().Run();
        return 0;
      }
      catch (Exception ex)
      {
        Log.Fatal(ex, "Host terminated unexpectedly");
        return 1;
      }
      finally
      {
        Log.CloseAndFlush();
      }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
      var urls = new List<string>();
      urls.Add($"http://*:{configuration.GetValue<int>("Host:Port")}");
      if ((Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production") == "Produttion")
      {
        urls.Add($"https://*:{configuration.GetValue<int>("Host:Port") + 1}");
      }

      return Host.CreateDefaultBuilder(args)
          .ConfigureAppConfiguration((hostingContext, config) =>
          {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            config.AddJsonFile(path + "sitecodes.json", optional: true, reloadOnChange: true);
          })
          .ConfigureWebHostDefaults(webBuilder =>
          {
            webBuilder.UseStartup<Startup>();
            webBuilder.UseSerilog();
            webBuilder.UseUrls(urls.ToArray());
          }).UseWindowsService();
    }
  }
}

