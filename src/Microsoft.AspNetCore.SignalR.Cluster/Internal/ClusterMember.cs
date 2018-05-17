using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Cluster.Internal
{
    public class ClusterMember
    {

        public string Name { get; set; }

        public string Address { get; set; }

        public DateTimeOffset LastAnnouncement { get; set; }

    }
}
