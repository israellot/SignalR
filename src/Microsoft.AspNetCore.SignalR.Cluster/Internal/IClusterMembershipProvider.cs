using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Cluster.Internal
{
    public interface IClusterSignalRMembershipProvider
    {
        Task Announce(string nomeName, string address);

        Task<IList<ClusterMember>> GetAllMembers();

        string GetOwnUniqueId();

        string ClusterToken { get; }
    }
}
