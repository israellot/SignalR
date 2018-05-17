// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Cluster.Internal;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.Cluster
{
    /// <summary>
    /// Options used to configure <see cref="ClusterHubLifetimeManager{THub}"/>.
    /// </summary>
    public class ClusterOptions
    {
        public String ClusterToken { get; set; } = "secrettoken";

    }
}
