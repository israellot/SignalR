using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.AspNetCore.SignalR.Cluster.Internal
{
       
   

    public class ClusterManager:IDisposable 
    {
        bool _isDisposed;

        ILogger _logger;
        IClusterSignalRMembershipProvider _membershipProvider;
        ClusterDirectoryFactory _directoryFactory;
        ClusterPeerNodeFactory _peerFactory;

        ConcurrentDictionary<string, ClusterPeerNodeHandler> _peers = new ConcurrentDictionary<string, ClusterPeerNodeHandler>();
        
        public ClusterManager(
            ILogger<ClusterManager> logger,
            ClusterDirectoryFactory directoryFactory,
            ClusterPeerNodeFactory peerFactory,
            IClusterSignalRMembershipProvider membershipProvider)
        {
            _logger = logger;
            _membershipProvider = membershipProvider;
            _directoryFactory = directoryFactory;
            _peerFactory = peerFactory;

            var ignore = MonitorTask();
        }

        

        public async Task SendToAll(string[] nodes, string hubName, string methodName, object[] args, string[] excludeConnectionIds = null)
        {

            var tasks = new List<Task>();
            foreach (var node in nodes)
            {
                var nodeProxy = _peers.Values.FirstOrDefault(kv => kv.UniqueId == node);

                if (nodeProxy != null)
                {
                    var t= nodeProxy.Connection.InvokeAsync("SendToAllConnections", hubName, methodName, args, excludeConnectionIds);
                    tasks.Add(t);
                }

            }

            await Task.WhenAll(tasks);
        }

        public async Task DirectoryOperations(DirectoryOperation[] ops)
        {

            var tasks = new List<Task>();
            foreach (var nodeProxy in _peers.Values)
            {
                var t = nodeProxy.Connection.InvokeAsync("DirectoryOperations", ops);
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
        }

        private async Task MonitorTask()
        {
            while (!_isDisposed)
            {
                try
                {
                    var members = await _membershipProvider.GetAllMembers();

                    foreach (var m in members)
                    {

                        var node = _peers.GetOrAdd(m.Name, _ => {
                            _logger.LogInformation($"Adding peer {m.Name} on {m.Address}");
                            return _peerFactory.GetNewPeerNode(m.Name, m.Address);
                        });
                        
                    }

                    await Task.Delay(5000);
                }
                catch { }
            }
        }

        

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
