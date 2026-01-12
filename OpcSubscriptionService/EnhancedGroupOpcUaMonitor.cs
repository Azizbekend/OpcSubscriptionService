// EnhancedGroupOpcUaMonitor.cs
using Opc.Ua;
using Opc.Ua.Client;

namespace OpcSubscriptionService
{
    public class EnhancedGroupOpcUaMonitor : GroupOpcUaMonitor
    {
        private Dictionary<string, NodeStatistics> _nodeStatistics;
        private DateTime _startTime;
        private int _totalNotifications = 0;

        public EnhancedGroupOpcUaMonitor(string serverUrl, List<string> nodeIds) 
            : base(serverUrl, nodeIds)
        {
            _nodeStatistics = new Dictionary<string, NodeStatistics>();
            _startTime = DateTime.UtcNow;

            // Подписываемся на события базового класса
            this.OnValueChanged += OnValueChangedWithStats;
            this.OnStatusChanged += OnStatusChangedWithStats;
        }

        private void OnValueChangedWithStats(string nodeId, DataValue dataValue, MonitoredItem monitoredItem)
        {
            _totalNotifications++;
            
            if (!_nodeStatistics.ContainsKey(nodeId))
            {
                _nodeStatistics[nodeId] = new NodeStatistics();
            }

            var stats = _nodeStatistics[nodeId];
            stats.TotalUpdates++;
            stats.LastUpdate = DateTime.UtcNow;
            stats.LastValue = dataValue.Value;
            stats.LastStatus = dataValue.StatusCode;

            // Обновляем статистику качества
            if (StatusCode.IsGood(dataValue.StatusCode))
            {
                stats.GoodUpdates++;
            }
            else
            {
                stats.BadUpdates++;
            }

            // Периодически выводим статистику (каждые 100 сообщений)
            if (_totalNotifications % 100 == 0)
            {
                PrintStatistics();
            }
        }

        private void OnStatusChangedWithStats(string message)
        {
            // Можно логировать или обрабатывать статус сообщения
        }

        public void PrintStatistics()
        {
            var uptime = DateTime.UtcNow - _startTime;
            Console.WriteLine($"\n=== OPC UA Monitor Statistics ===");
            Console.WriteLine($"Uptime: {uptime:hh\\:mm\\:ss}");
            Console.WriteLine($"Total notifications: {_totalNotifications}");
            Console.WriteLine($"Active nodes: {_nodeStatistics.Count}");
            Console.WriteLine($"Notifications/sec: {_totalNotifications / uptime.TotalSeconds:F2}");
            
            foreach (var (nodeId, stats) in _nodeStatistics)
            {
                var qualityPercentage = stats.TotalUpdates > 0 ? 
                    (double)stats.GoodUpdates / stats.TotalUpdates * 100 : 0;
                
                Console.WriteLine($"\nNode: {nodeId}");
                Console.WriteLine($"  Updates: {stats.TotalUpdates} (Good: {stats.GoodUpdates}, Bad: {stats.BadUpdates})");
                Console.WriteLine($"  Quality: {qualityPercentage:F1}%");
                Console.WriteLine($"  Last Value: {stats.LastValue}");
                Console.WriteLine($"  Last Update: {stats.LastUpdate:HH:mm:ss}");
            }
            Console.WriteLine(new string('=', 50));
        }

        public Dictionary<string, NodeStatistics> GetStatistics()
        {
            return new Dictionary<string, NodeStatistics>(_nodeStatistics);
        }
    }

    public class NodeStatistics
    {
        public int TotalUpdates { get; set; }
        public int GoodUpdates { get; set; }
        public int BadUpdates { get; set; }
        public object LastValue { get; set; }
        public DateTime LastUpdate { get; set; }
        public StatusCode LastStatus { get; set; }

        public NodeStatistics()
        {
            LastUpdate = DateTime.UtcNow;
        }
    }
}