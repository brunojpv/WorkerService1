using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Data;
using System.Data.SqlClient;
using Lib.Repository;
using System.IO;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

namespace WorkerService
{
    public class Program
    {
        private static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            IConfigurationRoot configuration = GetConfiguration();
            ConfiguraLog(configuration);

            try
            {
                Log.Information("Iniciando worker!!!");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Erro no worker!!!");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfiguraLog(IConfigurationRoot configuration)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

            Log.Logger = new LoggerConfiguration()                            
                            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
                            {
                                AutoRegisterTemplate = true,
                                IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower()}-{DateTime.UtcNow:yyyy-MM}"
                            })
                            .Enrich.WithProperty("Environment", environment)
                            .ReadFrom.Configuration(configuration)
                            .CreateLogger();
        }

        private static IConfigurationRoot GetConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .Build();
            
            return configuration;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureAppConfiguration((hostContext, config) => {
                    var env = hostContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    Configuration = hostContext.Configuration;
                    services.AddSingleton<IDbConnection>(conn => new SqlConnection(Configuration.GetConnectionString("connectionSQL")));
                    services.AddScoped<IUsersRepository, UsersRepository>();
                    services.AddScoped<IAdressRepository, AdressRepository>();
                    services.AddScoped<ICompanyRepository, CompanyRepository>();
                    services.AddScoped<IGeoRepository, GeoRepository>();
                    services.AddScoped<IRepository, Repository>();
                    services.AddHostedService<Worker>();
                });
    }
}
