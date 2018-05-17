// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClusterSample
{
    public class Broadcaster : Hub
    {
        public Task Broadcast(string sender, string message) =>
            Clients.Others.SendAsync("Message", sender, message);
    }
}
