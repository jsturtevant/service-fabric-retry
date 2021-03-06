using System;
using System.Collections.Generic;
using System.Fabric.Query;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using ServiceA.Services;

namespace ServiceA
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
            services.AddControllers();
            services.AddApplicationInsightsTelemetry();
            services.AddPollyPolicies();
            services.AddSingleton<IServiceResolver, ServiceResolver>();

            // Created multiple http clients for demonstration purposes.
            services.AddHttpClient<WeatherClientTyped>("noretry", x => { x.BaseAddress = new Uri(Constants.reverseProxy); });
            services.AddHttpClient("retry", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(15); // Overall timeout across all tries
                })
                .AddPolicyHandlerFromRegistry(Constants.backoffpolicy)
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5))); //timeout per request https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory#use-case-applying-timeouts
            services.AddHttpClient("sf", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(5); // Overall timeout for http client
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }




}
