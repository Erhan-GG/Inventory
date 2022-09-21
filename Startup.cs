using Common.MassTransit;
using Common.MongoDB;
using Inventory.Clients;
using Inventory.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Inventory
{
    public class Startup
    {
        private const string AllowedOriginSetting = "AllowedOrigin";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMongo()
                    .AddMongoRepository<InventoryItem>("inventoryitems")
                    .AddMongoRepository<CatalogItem>("catalogitems")
                    .AddMassTransitWithRabbitMQ();

            //AddCatalogClient(services);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Inventory", Version = "v1" });
            });
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory v1"));
                app.UseCors(builder =>
                {
                    builder.WithOrigins(Configuration[AllowedOriginSetting])
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static void AddCatalogClient(IServiceCollection services)
        {
            Random jitter = new Random();

            services.AddHttpClient<CatalogClient>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001");
            }
            )
            .AddTransientHttpErrorPolicy(builder =>
                builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                    3,
                    retryAttemp => TimeSpan.FromSeconds(Math.Pow(2, retryAttemp))
                        + TimeSpan.FromMilliseconds(jitter.Next(0, 1000)),
                    onRetry: (outcome, timespan, retryAttemp) =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        serviceProvider.GetService<ILogger<CatalogClient>>()?
                        .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttemp}.");
                    }
                )
            )
            .AddTransientHttpErrorPolicy(builder =>
                builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                        3,
                        TimeSpan.FromSeconds(15),
                        onBreak: (outcome, timespan) =>
                        {
                            var serviceProvider = services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?
                            .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds ...");
                        },
                        onReset: () =>
                        {
                            var serviceProvider = services.BuildServiceProvider();
                            serviceProvider.GetService<ILogger<CatalogClient>>()?
                            .LogWarning($"Closing the circuit ...");
                        }
                    )
                )
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
        }
    }
}
