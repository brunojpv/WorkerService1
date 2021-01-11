using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lib.Repository;
using Lib.Models;
using Lib.Service;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly Sites _sites;
        private readonly IServiceScopeFactory _serviceScoped;

        public Worker(ILogger<Worker> logger, IConfiguration _conf, IServiceScopeFactory serviceScoped)
        {
            _logger = logger;
            _sites = _conf.GetSection("Sites").Get<Sites>();
            _serviceScoped = serviceScoped;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                HttpStatusCode status = await Requesters.GetStatusFromUrl(_sites.Url);
                if(status != HttpStatusCode.OK)
                {
                    _logger.LogError("API fora do ar!!!");
                }

                var users = await Requesters.GetUsersAsync(_sites.Url);                

                if (users != null)
                {
                    using (var scope = _serviceScoped.CreateScope())
                    {
                        var _repository = scope.ServiceProvider.GetRequiredService<IRepository>();
                        foreach (var user in users)
                        {
                            _repository.BaixarDados(user);

                            if (user.Address.Suite.Contains("Suite"))
                                _repository.SalvarDados(user);
                        }
                    }
                }

                Console.ReadLine();
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
