// AdvancedOpcUaMonitor.cs
//using Opc.Ua;
//using Opc.Ua.Client;
//using Opc.Ua.Configuration;
//using System.Collections.Concurrent;


//namespace OpcUaSimpleSubscription
//{
//    public class AdvancedOpcUaMonitor : IDisposable
//    {
//        private Session _session;
//        private Subscription _subscription;
//        private string _serverUrl;
//        private string _nodeId;
//        private ICollection<string> _nodeIds;
//        private Timer _reconnectTimer;
//        private bool _disposed = false;
//        private ConcurrentDictionary<string, MonitoredItem> _monitoredItems;
//        public event Action<string, DataValue, MonitoredItem> OnValueChanged;
//        public event Action<string, StatusCode> OnNodeStatusChanged;
//        public event Action<string> OnStatusChanged;
//        public event Action<bool> OnConnectionStatusChanged;

//        public AdvancedOpcUaMonitor(string serverUrl, string nodeId)
//        {
//            _serverUrl = serverUrl;
//            _nodeId = nodeId;
//        }
//        public AdvancedOpcUaMonitor(string serverUrl, ICollection<string> nodeIds)
//        {
//            _serverUrl = serverUrl;
//            _nodeIds = nodeIds;
//        }

//        public async Task StartMonitoringAsync()
//        {
//            OnStatusChanged?.Invoke("Starting OPC UA monitor...");
//            await ConnectAndSubscribeAsync();
//        }

//        private async Task ConnectAndSubscribeAsync()
//        {
//            try
//            {
//                OnStatusChanged?.Invoke($"Connecting to {_serverUrl}...");

//                var config = new ApplicationConfiguration
//                {
//                    ApplicationName = "OpcConnectionTest",
//                    ApplicationUri = Utils.Format(@"urn:{0}:OpcConnectionTest", System.Net.Dns.GetHostName()),
//                    ApplicationType = ApplicationType.Client,
//                    SecurityConfiguration = new SecurityConfiguration()
//                    {
//                        ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\CertIndifier" },
//                        TrustedIssuerCertificates = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\trustedIssuer" },
//                        TrustedPeerCertificates = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\trustedPeer" },
//                        RejectedCertificateStore = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\rejectStore" },
//                        AutoAcceptUntrustedCertificates = true
//                    },
//                    TransportConfigurations = new TransportConfigurationCollection(),
//                    TransportQuotas = new TransportQuotas() { OperationTimeout = 15000 },
//                    ClientConfiguration = new ClientConfiguration() { DefaultSessionTimeout = 60000 },
//                    TraceConfiguration = new TraceConfiguration()
//                };

//                await config.ValidateAsync(ApplicationType.Client);
//                if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
//                {
//                    config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
//                }
                
//                var edpoint = await CoreClientUtils.SelectEndpointAsync(config, _serverUrl, useSecurity: false);
//                var identity = new UserIdentity();
//                _session = await Session.Create(
//                    config,
//                    new ConfiguredEndpoint(null, edpoint, EndpointConfiguration.Create(config)),
//                    false,
//                    "",
//                    60000,
//                    identity,
//                    null);

//                if (_session.Connected)
//                {
//                    OnStatusChanged?.Invoke("✅ Connected successfully!");
//                    CreateSubscription();
//                }
//            }
//            catch (Exception ex)
//            {
//                OnStatusChanged?.Invoke($"❌ Connection failed: {ex.Message}");
//                ScheduleReconnect();
//            }
//        }
//        private void CreateSubscription(int publishingInterval)
//        {
//            try
//            {
//                _subscription = new Subscription(_session.DefaultSubscription)
//                {
//                    DisplayName = "GroupSubscription",
//                    PublishingInterval = publishingInterval,
//                    KeepAliveCount = 10,
//                    LifetimeCount = 30,
//                    MaxNotificationsPerPublish = 1000,
//                    Priority = 1
//                };

//                // Создаем MonitoredItems для каждой ноды
//                foreach (var nodeId in _nodeIds)
//                {
//                    try
//                    {
//                        var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
//                        {
//                            DisplayName = nodeId,
//                            StartNodeId = nodeId,
//                            AttributeId = Attributes.Value,
//                            MonitoringMode = MonitoringMode.Reporting,
//                            SamplingInterval = publishingInterval,
//                            QueueSize = 1,
//                            DiscardOldest = true,
//                            CacheQueueSize = 1
//                        };

//                        monitoredItem.Notification += OnMonitoredItemNotification;
//                        monitoredItem.Handle = nodeId; // Сохраняем nodeId в handle для идентификации

//                        _subscription.AddItem(monitoredItem);
//                        _monitoredItems[nodeId] = monitoredItem;

//                        OnStatusChanged?.Invoke($"✅ Added node to subscription: {nodeId}");
//                    }
//                    catch (Exception ex)
//                    {
//                        OnStatusChanged?.Invoke($"❌ Failed to add node {nodeId}: {ex.Message}");
//                    }
//                }

//                if (_subscription.MonitoredItemCount > 0)
//                {
//                    _session.AddSubscription(_subscription);
//                    _subscription.Create();
//                    OnStatusChanged?.Invoke($"✅ Subscription created with {_subscription.MonitoredItemCount} nodes");
//                }
//                else
//                {
//                    OnStatusChanged?.Invoke("⚠️ No nodes were added to subscription");
//                }
//            }
//            catch (Exception ex)
//            {
//                OnStatusChanged?.Invoke($"❌ Subscription creation failed: {ex.Message}");
//                throw;
//            }
//        }

//        private void OnMonitoredItemNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
//        {
//            try
//            {
//                if (e.NotificationValue is MonitoredItemNotification notification)
//                {
//                    var nodeId = monitoredItem.Handle as string ?? monitoredItem.DisplayName;

//                    OnValueChanged?.Invoke(nodeId, notification.Value, monitoredItem);

//                    // Отправляем статус ноды
//                    if (StatusCode.IsNotGood(notification.Value.StatusCode))
//                    {
//                        OnNodeStatusChanged?.Invoke(nodeId, notification.Value.StatusCode);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                OnStatusChanged?.Invoke($"❌ Error processing notification: {ex.Message}");
//            }
//        }
//        //private void CreateMultiSubscription(List <string> nodeIds)
//        //{
//        //    foreach (var nodeId in nodeIds) 
//        //    {
//        //        _subscription = new Subscription(_session.DefaultSubscription)
//        //        {
//        //            DisplayName = "AdvancedMonitor",
//        //            PublishingInterval = 500,
//        //            KeepAliveCount = 10
//        //        };
//        //        var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
//        //        {
//        //            DisplayName = _nodeId,
//        //            StartNodeId = _nodeId,
//        //            AttributeId = Attributes.Value,
//        //            SamplingInterval = 500,
//        //            QueueSize = 1,
//        //            DiscardOldest = true
//        //        };

//        //        monitoredItem.Notification += (item, e) =>
//        //        {
//        //            if (e.NotificationValue is MonitoredItemNotification notification)
//        //            {
//        //                OnValueChanged?.Invoke(_nodeId, notification.Value);
//        //            }
//        //        };

//        //        _subscription.AddItem(monitoredItem);
//        //        _session.AddSubscription(_subscription);
//        //        _subscription.CreateAsync();


//        //    }
//        //}
//        private async Task AddNodeToSubscription(string nodeId)
//        {


//                var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
//                {
//                    StartNodeId = new NodeId(nodeId),
//                    AttributeId = Attributes.Value,
//                    DisplayName = nodeId,
//                    MonitoringMode = MonitoringMode.Reporting,
//                    SamplingInterval = 500,
//                    QueueSize = 1,
//                    DiscardOldest = true
//                };


//            _subscription.AddItem(monitoredItem);
//            await _subscription.ApplyChangesAsync();
//        }
//        private void CreateSubscription()
//        {
//            try
//            {
//                _subscription = new Subscription(_session.DefaultSubscription)
//                {
//                    DisplayName = "AdvancedMonitor",
//                    PublishingInterval = 500,
//                    KeepAliveCount = 10
//                };

//                var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
//                {
//                    DisplayName = _nodeId,
//                    StartNodeId = _nodeId,
//                    AttributeId = Attributes.Value,
//                    SamplingInterval = 500,
//                    QueueSize = 1,
//                    DiscardOldest = true
//                };

//                monitoredItem.Notification += (item, e) =>
//                {
//                    if (e.NotificationValue is MonitoredItemNotification notification)
//                    {
//                        OnValueChanged?.Invoke(_nodeId, notification.Value);
//                    }
//                };

//                _subscription.AddItem(monitoredItem);
//                _session.AddSubscription(_subscription);
//                _subscription.CreateAsync();

//                OnStatusChanged?.Invoke($"✅ Monitoring node: {_nodeId}");
//            }
//            catch (Exception ex)
//            {
//                OnStatusChanged?.Invoke($"❌ Subscription failed: {ex.Message}");
//            }
//        }

//        private void ScheduleReconnect()
//        {
//            _reconnectTimer = new Timer(async _ => 
//            {
//                if (!_disposed)
//                {
//                    await ConnectAndSubscribeAsync();
//                }
//            }, null, 5000, Timeout.Infinite);
//        }

//        public void Dispose()
//        {
//            if (!_disposed)
//            {
//                _disposed = true;
//                _reconnectTimer?.Dispose();
//                _subscription?.DeleteAsync(true);
//                _subscription?.Dispose();
//                _session?.CloseAsync();
//                _session?.Dispose();
//                OnStatusChanged?.Invoke("Monitor disposed.");
//            }
//        }
//    }
//}