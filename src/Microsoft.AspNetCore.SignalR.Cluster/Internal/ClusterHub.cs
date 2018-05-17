using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Cluster.Internal
{
    public class ClusterHub:Hub<IClusterHub>
    {

        ClusterManager _clusterManager;
        IClusterSignalRMembershipProvider _membershipProvider;
        ClusterDirectoryFactory _directoryFactory;
        IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();


        public ClusterHub(
            IClusterSignalRMembershipProvider membershipProvider,
            ClusterManager clusterManager,
            IServiceProvider serviceProvider,
            ClusterDirectoryFactory directoryFactory)
        {
            _clusterManager = clusterManager;
            _membershipProvider = membershipProvider;
            _directoryFactory = directoryFactory;
            _serviceProvider = serviceProvider;
        }

        public override Task OnConnectedAsync()
        {
            var headers = this.Context.GetHttpContext().Request.Headers;

            if (headers.TryGetValue("X-SignalR-Cluster-Node", out var nodeName))
            {
                if (headers.TryGetValue("X-SignalR-Cluster-Node-Id", out var nodeUniqueId))
                {
                    Context.Items["Cluster-Node-Id"] = nodeUniqueId;
                    Context.Items["Cluster-Node"] = nodeName;

                    //anounce unique id back                    
                    Clients.Clients(Context.ConnectionId).RemoteUniqueId(_membershipProvider.GetOwnUniqueId());
                }
                else
                {
                    
                }
            }
                

            return Task.CompletedTask;

        }

         

        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                _connections.TryRemove((string)Context.Items["Cluster-Node-Id"], out _);
            }
            catch { }

            return Task.CompletedTask;
        }
        
        public Task DirectoryOperations(DirectoryOperation[] ops)
        {
            foreach (var g in ops.GroupBy(o => o.Hub))
            {
                var hubDirectory = _directoryFactory.GetDirectory(g.Key);

                hubDirectory.PerformOperations(g.ToArray());
            }

            return Task.CompletedTask;
        }

        public  Task SendToAllConnections(string hubName, string methodName, object[] args, string[] excludeConnectionIds = null)
        {
            var hubDirectory = _directoryFactory.GetDirectory(hubName);

            if (hubDirectory != null)
            {
                var hubLifetimeManagerType = typeof(HubLifetimeManager<>).MakeGenericType(hubDirectory.HubType);

                var hubLifetimeManager = (IClusterHubLifetimeManager)_serviceProvider.GetRequiredService(hubLifetimeManagerType);

                hubLifetimeManager.SendToAllLocalConnection(methodName, args, excludeConnectionIds);
            }


            return Task.CompletedTask;
        }

    }

    public interface IClusterHub
    {
    

        Task RemoteUniqueId(string id);
    }
}
