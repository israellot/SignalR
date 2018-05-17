using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Cluster.Internal
{
    public class ClusterDirectoryFactory
    {
        private ILogger _logger;
        private IServiceProvider _serviceProvider;

        private ConcurrentDictionary<string, ClusterDirectory> _directories = new ConcurrentDictionary<string, ClusterDirectory>();


        public ClusterDirectoryFactory(IServiceProvider serviceProvider, ILogger<ClusterDirectoryFactory> logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public ClusterDirectory GetOrCreateDirectory<THub>() where THub : Hub
        {
            var hubName = typeof(THub).FullName;

            var directory= _directories.GetOrAdd(hubName, (s) => {
                return new ClusterDirectory(
                    _serviceProvider.GetRequiredService<ILogger<ClusterDirectory>>(),
                    typeof(THub)
                    );
            });

            if (directory.HubType == null)
                directory.AssociateType(typeof(THub));

            return directory;
        }

        public ClusterDirectory GetDirectory(string hubName)
        {
            return _directories.GetOrAdd(hubName, (s) => {
                return new ClusterDirectory(
                    _serviceProvider.GetRequiredService<ILogger<ClusterDirectory>>()
                    );
            });
        }

    }

    public class ClusterDirectory
    {
        private ILogger _logger;


        private ConcurrentDictionary<string, HashSet<string>> _connections = new ConcurrentDictionary<string, HashSet<string>>();
        private ConcurrentDictionary<string, HashSet<string>> _users = new ConcurrentDictionary<string, HashSet<string>>();
        private ConcurrentDictionary<string, HashSet<string>> _groups = new ConcurrentDictionary<string, HashSet<string>>();

        public string HubName { get; private set; }

        public Type HubType { get; private set; }

        public ClusterDirectory(ILogger<ClusterDirectory> logger,Type hubType=null) {
            _logger = logger;

            if(hubType!=null)
                AssociateType(hubType);
        }

        public void AssociateType(Type type)
        {
            HubName = type.FullName;
            HubType = type;
        }

        public void PerformOperations(DirectoryOperation[] ops)
        {
            foreach (var op in ops)
                PerformOperation(op);
        }

        public bool PerformOperation(DirectoryOperation op)
        {
            if (op.Action == DirectoryOperation.DirectoryAction.Add)
            {
                _logger.LogInformation($"Performing Add {op.ItemId} to {op.Element.ToString()} on hub {op.Hub} for node {op.Node} ");
            }
            else
            {
                _logger.LogInformation($"Performing Remove {op.ItemId} from {op.Element.ToString()} on hub {op.Hub} for node {op.Node} ");
            }

            HashSet<string> hashset;
            switch (op.Element)
            {
                case DirectoryOperation.DirectoryElement.User:
                    hashset = _users.GetOrAdd(op.Node, (n) => new HashSet<string>());break;
                case DirectoryOperation.DirectoryElement.Connection:
                    hashset = _connections.GetOrAdd(op.Node, (n) => new HashSet<string>()); break;
                case DirectoryOperation.DirectoryElement.Group:
                    hashset = _groups.GetOrAdd(op.Node, (n) => new HashSet<string>()); break;
                default:
                    throw new NotImplementedException();
            }

            lock (hashset)
            {
                if (op.Action == DirectoryOperation.DirectoryAction.Add)
                    return hashset.Add(op.ItemId);
                else
                    return hashset.Remove(op.ItemId);
            }            
        }

        public string[] NodesWithAnyConnection(string except=null)
        {
            var nodes = _connections.Where(s => s.Value.Any()).Select(x => x.Key);

            if (!string.IsNullOrEmpty(except))
                nodes = nodes.Where(n => n != except);

            return nodes.ToArray();
        }

        public string NodeForConnection(string connectionId)
        {
            return _connections
                .Where(s => s.Value.Contains(connectionId))
                .Select(x => x.Key).FirstOrDefault();
        }

        public string[] NodesForUser(string userId)
        {
            return _users
                .Where(s => s.Value.Contains(userId))
                .Select(x => x.Key)
                .ToArray();
        }
    }

   [DataContract]
    public class DirectoryOperation
    {
        [DataMember(Order =0)]
        public DirectoryAction Action { get; set; }

        [DataMember(Order = 1)]
        public DirectoryElement Element { get; set; }

        [DataMember(Order = 2)]
        public String Node { get; set; }

        [DataMember(Order = 3)]
        public String Hub { get; set; }

        [DataMember(Order = 4)]
        public String ItemId { get; set; }

        public static DirectoryOperation AddConnection<THub>(string node,string connectionId) where THub:Hub
        {
            return new DirectoryOperation()
            {
                Action = DirectoryAction.Add,
                Element = DirectoryElement.Connection,
                Hub = typeof(THub).FullName,
                Node = node,
                ItemId = connectionId
            };
        }

        public static DirectoryOperation AddUser<THub>(string node, string userId) where THub : Hub
        {
            return new DirectoryOperation()
            {
                Action = DirectoryAction.Add,
                Element = DirectoryElement.User,
                Hub = typeof(THub).FullName,
                Node = node,
                ItemId = userId
            };
        }

        public static DirectoryOperation AddGroup<THub>(string node, string groupId) where THub : Hub
        {
            return new DirectoryOperation()
            {
                Action = DirectoryAction.Add,
                Element = DirectoryElement.Group,
                Hub = typeof(THub).FullName,
                Node = node,
                ItemId = groupId
            };
        }

        public static DirectoryOperation RemoveConnection<THub>(string node, string connectionId) where THub : Hub
        {
            return new DirectoryOperation()
            {
                Action = DirectoryAction.Remove,
                Element = DirectoryElement.Connection,
                Hub = typeof(THub).FullName,
                Node = node,
                ItemId = connectionId
            };
        }

        public static DirectoryOperation RemoveUser<THub>(string node, string userId) where THub : Hub
        {
            return new DirectoryOperation()
            {
                Action = DirectoryAction.Remove,
                Element = DirectoryElement.User,
                Hub = typeof(THub).FullName,
                Node = node,
                ItemId = userId
            };
        }

        public static DirectoryOperation RemoveGroup<THub>(string node, string groupId) where THub : Hub
        {
            return new DirectoryOperation()
            {
                Action = DirectoryAction.Remove,
                Element = DirectoryElement.Group,
                Hub = typeof(THub).FullName,
                Node = node,
                ItemId = groupId
            };
        }

        public enum DirectoryAction : byte
        {            
            Add = 1,
            Remove = 2,
        }

        public enum DirectoryElement : byte
        {
            Connection = 1,
            User = 2,
            Group = 3
        }
    }
}
