// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR.Cluster;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using System.Threading.Tasks;


namespace ClusterSample
{
    public class Startup
    {
      

        public void ConfigureServices(IServiceCollection services)
        {
        
            services.AddSignalR()
                .AddCluster()
                .AddMessagePackProtocol()
                .AddStaticMembership(new[]
                {
                    new StaticClusterMember("node_1","http://localhost:5000"),
                    new StaticClusterMember("node_2","http://localhost:5001")
                });         
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseFileServer();
            app.UseClusteredSignalR(options => options.MapHub<Broadcaster>("/broadcast"));

        }

       
    }
}
