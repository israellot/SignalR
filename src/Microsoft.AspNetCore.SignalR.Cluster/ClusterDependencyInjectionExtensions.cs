// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Cluster;
using Microsoft.AspNetCore.SignalR.Cluster.Internal;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring Redis-based scale-out for a SignalR Server in an <see cref="ISignalRServerBuilder" />.
    /// </summary>
    public static class ClusterDependencyInjectionExtensions
    {

        public static ISignalRServerBuilder AddCluster(this ISignalRServerBuilder signalrBuilder)
        {
            return AddCluster(signalrBuilder, c => { });
        }


        public static ISignalRServerBuilder AddCluster(this ISignalRServerBuilder signalrBuilder, Action<ClusterOptions> configure)
        {
            
            signalrBuilder.Services.Configure(configure);
            signalrBuilder.Services.AddSingleton<ClusterSignalRMarkerService>();
            signalrBuilder.Services.AddSingleton<ClusterManager>();
            signalrBuilder.Services.AddSingleton<ClusterPeerNodeFactory>();
            signalrBuilder.Services.AddSingleton<ClusterDirectoryFactory>();
            signalrBuilder.Services.AddSingleton<ClusterHub>();
            signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<ClusterHub>), typeof(DefaultHubLifetimeManager<ClusterHub>));            
            signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(ClusterHubLifetimeManager<>));

            return signalrBuilder;
        }

        public static ISignalRServerBuilder AddStaticMembership(this ISignalRServerBuilder signalrBuilder,StaticClusterMember[] clusterMembers)
        {
            signalrBuilder.Services
                .Configure<StaticClusterMembershipOptions>(s =>
                {
                    s.Members = clusterMembers.ToList();
                })
                .AddSingleton<IClusterSignalRMembershipProvider, StaticMembershipProvider>();

            return signalrBuilder;
        }

    }
}
