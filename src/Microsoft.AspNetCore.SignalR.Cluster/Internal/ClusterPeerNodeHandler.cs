using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Cluster.Internal
{
    public class ClusterPeerNodeFactory
    {
        private IClusterSignalRMembershipProvider _membershipProvider;
        private ClusterDirectoryFactory _directoryFactory;
        private IServiceProvider _serviceProvider;

        public ClusterPeerNodeFactory(IServiceProvider serviceProvider,
            ClusterDirectoryFactory directoryFactory,
            IClusterSignalRMembershipProvider membershipProvider)
        {
            _serviceProvider = serviceProvider;
            _directoryFactory = directoryFactory;
            _membershipProvider = membershipProvider;
        }

        public ClusterPeerNodeHandler GetNewPeerNode(string name,string address)
        {
            return new ClusterPeerNodeHandler(_serviceProvider, _directoryFactory, _membershipProvider, name, address);
        }

    }

    public class ClusterPeerNodeHandler : IDisposable
    {
        private bool _isDisposed;

        private IServiceProvider _serviceProvider;
        private IClusterSignalRMembershipProvider _membershipProvider;
        private ClusterDirectoryFactory _directoryFactory;

        public string Name { get; private set; }

        public string UniqueId { get; private set; }

        public string Address { get; private set; }

        public Boolean IsSelf { get; set; }

        public HubConnection Connection { get; private set; }


        public ClusterPeerNodeHandler(
            IServiceProvider serviceProvider,
            ClusterDirectoryFactory directoryFactory,
            IClusterSignalRMembershipProvider membershipProvider,
            string name, string address)
        {
            Name = name;
            Address = address;

            _membershipProvider = membershipProvider;
            _directoryFactory = directoryFactory;
            _serviceProvider = serviceProvider;

            var ignore = ConnectTask();
        }

        

        

        private async Task ConnectTask()
        {            
            if (Connection == null)
            {

                var builder = new HubConnectionBuilder()
                .WithUrl(new Uri(Address + "/_cluster"), HttpTransportType.WebSockets, o => {
                    o.Headers = new Dictionary<string, string>()
                    {
                        ["X-SignalR-Cluster-Token"] = _membershipProvider.ClusterToken,
                        ["X-SignalR-Cluster-Node"] = Name,
                        ["X-SignalR-Cluster-Node-Id"] = _membershipProvider.GetOwnUniqueId()
                    };
                })
                .AddMessagePackProtocol();

                Connection = builder.Build();

                HookCallbacks();

                Connection.Closed += (ex) => {
                    return ConnectTask();
                };
                                
            }

           
            if (Connection.State == HubConnectionState.Disconnected)
            {
                await Task.Delay(1000);
                while (!_isDisposed && Connection != null && !IsSelf)
                {
                    try
                    {
                        await Connection.StartAsync();
                        if (Connection.State == HubConnectionState.Connected)
                            break;
                    }
                    catch
                    {
                        //ignore?
                    }
                    await Task.Delay(1000);
                }
            }

            if(IsSelf)
            {
                if (Connection != null && Connection.State == HubConnectionState.Connected)
                    _ = Connection.StopAsync();

                Connection = null;
            }

            
        }

        private void HookCallbacks()
        {
            var self = this;
            
            if (Connection != null)
            {
                Connection.On<string>("RemoteUniqueId", id => {

                    self.UniqueId = id;

                    if (id == _membershipProvider.GetOwnUniqueId())
                        this.IsSelf = true;

                });

                Connection.On<DirectoryOperation[]>("DirectoryOperations", ops => {

                    foreach(var g in ops.GroupBy(o => o.Hub))
                    {
                        var hubDirectory = _directoryFactory.GetDirectory(g.Key);

                        hubDirectory.PerformOperations(g.ToArray());
                    }

                });

            }
        }
        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
