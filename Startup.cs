using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
//using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using System.Collections.Generic;
using Binance.API.Csharp.Client.Models.Market;
using Binance.API.Csharp.Client.Models.Enums;

namespace Binance_Bot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });

            services.AddHttpClient();
            services.AddHostedService<ApiCaller>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }

    public class ApiCaller : IHostedService, IDisposable
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<ApiCaller> _logger;
        private Timer _timer;
        BinanceClient _client;
        ApiClient _apiClient;
        string _listenKey;
        bool bidPlaced = false;

        public ApiCaller(ILogger<ApiCaller> logger, IHttpClientFactory httpClientFactory)
        {
            _httpFactory = httpClientFactory;
            _logger = logger;
            _apiClient = new ApiClient("", "");
            _client = new BinanceClient(_apiClient);
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            //_logger.LogInformation("Timed Hosted Service running.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromMinutes(3));

            _listenKey = _client.StartUserStream().Result.ListenKey;
            _apiClient.ConnectToWebSocket("",);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            /*_logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);*/

            _client.CloseUserStream(_listenKey);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            if (!bidPlaced)
            {
                string symbol = "BTCUSDT";
                decimal qnt = 10;
                decimal buyProcent = 0.995M;
                decimal sellProcent = 1.005M;
                List<SymbolPrice> prices = new List<SymbolPrice>(_client.GetAllPrices().Result);
                var marketprc = prices.Find(price => price.Symbol == symbol).Price;
                decimal buyprc = marketprc * buyProcent;
                var res = _client.PostNewOrderTest(symbol, qnt, buyprc, OrderSide.BUY).Result;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
