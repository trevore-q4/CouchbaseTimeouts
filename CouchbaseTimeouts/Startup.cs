using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CouchbaseTimeouts
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            LoggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }
        public ILoggerFactory LoggerFactory { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddSingleton<ICluster>(
                new Cluster(new ClientConfiguration
                {
                    LoggerFactory = LoggerFactory,
                    BucketConfigs = new Dictionary<string, BucketConfiguration>
                    {
                        {
                            "DataBucket",
                            new BucketConfiguration
                            {
                                BucketName = "DataBucket",
                                Password = "testpassword",
                                UseSsl = false,
                                PoolConfiguration = new PoolConfiguration
                                {
                                    MinSize = 5,
                                    MaxSize = 20
                                }
                            }
                        }
                    },
                    Servers = new List<Uri>(new[]
                    {
                        new Uri("http://192.168.0.12:8091"),
                        new Uri("http://192.168.0.19:8091"),
                        new Uri("http://192.168.0.20:8091")
                    })
                }
            ));

            services.AddSingleton<IBucket>(sp =>
            {
                var cluster = sp.GetService<ICluster>();
                return cluster.OpenBucket("DataBucket");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
