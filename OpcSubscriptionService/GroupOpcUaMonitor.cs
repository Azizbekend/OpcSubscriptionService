// GroupOpcUaMonitor.cs
using Opc.Ua;
using Opc.Ua.Client;
using System.Collections.Concurrent;


namespace OpcSubscriptionService
{
    public class GroupOpcUaMonitor : IDisposable
    {
        private Session _session;
        private Subscription _subscription;
        private string _serverUrl;
        private List<string> _nodeIds;
        private Timer _reconnectTimer;
        private bool _disposed = false;
        private readonly ApplicationConfiguration _config;
        private int _reconnectAttempt = 0;
        private const int MAX_RECONNECT_ATTEMPTS = 10;

        // События
        public event Action<string, DataValue, MonitoredItem> OnValueChanged;
        public event Action<string, StatusCode> OnNodeStatusChanged;
        public event Action<string> OnStatusChanged;
        public event Action<bool> OnConnectionStatusChanged;

        // Словарь для отслеживания MonitoredItems
        private ConcurrentDictionary<string, MonitoredItem> _monitoredItems;

        public GroupOpcUaMonitor(string serverUrl, List<string> nodeIds)
        {
            _serverUrl = serverUrl;
            _nodeIds = nodeIds ?? new List<string>();
            _monitoredItems = new ConcurrentDictionary<string, MonitoredItem>();

            _config = new ApplicationConfiguration()
            {
                ApplicationName = "OpcConnectionTest",
                ApplicationUri = Utils.Format(@"urn:{0}:OpcConnectionTest", System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration()
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\CertIndifier" },
                    TrustedIssuerCertificates = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\trustedIssuer" },
                    TrustedPeerCertificates = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\trustedPeer" },
                    RejectedCertificateStore = new CertificateTrustList() { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPCFoundation\CertificateStores\rejectStore" },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas() { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration() { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };
        }

        public async Task StartMonitoringAsync(int publishingInterval = 1000)
        {
            OnStatusChanged?.Invoke($"Starting OPC UA group monitor for {_nodeIds.Count} nodes...");
            await ConnectAndSubscribeAsync(publishingInterval);
        }

        private async Task ConnectAndSubscribeAsync(int publishingInterval = 1000)
        {
            try
            {
                OnStatusChanged?.Invoke($"Connecting to {_serverUrl}...");

             

                await _config.ValidateAsync(ApplicationType.Client);
                if (_config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    _config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
                }

                var edpoint = await CoreClientUtils.SelectEndpointAsync(_config, _serverUrl, useSecurity: false);
                var identity = new UserIdentity();
                _session = await Session.Create(
                    _config,
                    new ConfiguredEndpoint(null, edpoint, EndpointConfiguration.Create(_config)),
                    false,
                    "",
                    60000,
                    identity,
                    null);

                if (_session.Connected)
                {
                    _reconnectAttempt = 0;
                    OnStatusChanged?.Invoke($"✅ Connected successfully to {_serverUrl}");
                    OnConnectionStatusChanged?.Invoke(true);
                    
                    // Создаем подписку после успешного подключения
                    CreateSubscription(publishingInterval);
                    
                    // Подписываемся на события сессии
                    //_session.KeepAlive += OnKeepAlive;
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"❌ Connection failed: {ex.Message}");
                ScheduleReconnect();
            }
        }

        private void CreateSubscription(int publishingInterval)
        {
            try
            {
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    DisplayName = "GroupSubscription",
                    PublishingInterval = publishingInterval,
                    KeepAliveCount = 10,
                    LifetimeCount = 30,
                    MaxNotificationsPerPublish = 1000,
                    Priority = 1
                };

                // Создаем MonitoredItems для каждой ноды
                foreach (var nodeId in _nodeIds)
                {
                    try
                    {
                        var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
                        {
                            DisplayName = nodeId,
                            StartNodeId = nodeId,
                            AttributeId = Attributes.Value,
                            MonitoringMode = MonitoringMode.Reporting,
                            SamplingInterval = publishingInterval,
                            QueueSize = 1,
                            DiscardOldest = true,
                            CacheQueueSize = 1
                        };

                        monitoredItem.Notification += OnMonitoredItemNotification;
                        monitoredItem.Handle = nodeId; // Сохраняем nodeId в handle для идентификации

                        _subscription.AddItem(monitoredItem);
                        _monitoredItems[nodeId] = monitoredItem;

                        OnStatusChanged?.Invoke($"✅ Added node to subscription: {nodeId}");
                    }
                    catch (Exception ex)
                    {
                        OnStatusChanged?.Invoke($"❌ Failed to add node {nodeId}: {ex.Message}");
                    }
                }

                if (_subscription.MonitoredItemCount > 0)
                {
                    _session.AddSubscription(_subscription);
                    _subscription.Create();
                    OnStatusChanged?.Invoke($"✅ Subscription created with {_subscription.MonitoredItemCount} nodes");
                }
                else
                {
                    OnStatusChanged?.Invoke("⚠️ No nodes were added to subscription");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"❌ Subscription creation failed: {ex.Message}");
                throw;
            }
        }

        private void OnMonitoredItemNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            try
            {
                if (e.NotificationValue is MonitoredItemNotification notification)
                {
                    var nodeId = monitoredItem.Handle as string ?? monitoredItem.DisplayName;
                    
                    OnValueChanged?.Invoke(nodeId, notification.Value, monitoredItem);
                    
                    // Отправляем статус ноды
                    if (StatusCode.IsNotGood(notification.Value.StatusCode))
                    {
                        OnNodeStatusChanged?.Invoke(nodeId, notification.Value.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"❌ Error processing notification: {ex.Message}");
            }
        }

        //private void OnKeepAlive(KeepAliveEventArgs e)
        //{
        //    if (e.CurrentState != ServerState.Running)
        //    {
        //        OnStatusChanged?.Invoke($"⚠️ Server state changed: {e.CurrentState}");
                
        //        if (e.CurrentState == ServerState.Unknown || e.CurrentState == ServerState.NotConnected)
        //        {
        //            OnConnectionStatusChanged?.Invoke(false);
        //            ScheduleReconnect();
        //        }
        //    }
        //}

        private void ScheduleReconnect()
        {
            if (_disposed || _reconnectAttempt >= MAX_RECONNECT_ATTEMPTS) return;

            _reconnectAttempt++;
            var delay = Math.Min(30000, _reconnectAttempt * 2000); // Экспоненциальная задержка до 30 сек

            OnStatusChanged?.Invoke($"🔄 Attempting reconnect {_reconnectAttempt}/{MAX_RECONNECT_ATTEMPTS} in {delay/1000} seconds...");

            _reconnectTimer = new Timer(async _ =>
            {
                if (!_disposed)
                {
                    await ReconnectAsync();
                }
            }, null, delay, Timeout.Infinite);
        }

        private async Task ReconnectAsync()
        {
            try
            {
                CleanupSession();
                await ConnectAndSubscribeAsync(_subscription?.PublishingInterval ?? 1000);
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"❌ Reconnect failed: {ex.Message}");
                ScheduleReconnect();
            }
        }

        private void CleanupSession()
        {
            try
            {
                if (_subscription != null)
                {
                    _subscription?.Delete(true);
                    _subscription?.Dispose();
                    _subscription = null;
                }

                if (_session != null)
                {
                    
                    _session?.Close();
                    _session?.Dispose();
                    _session = null;
                }

                _monitoredItems.Clear();
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"⚠️ Cleanup error: {ex.Message}");
            }
        }

        // Методы для динамического управления нодами
        public async Task<bool> AddNodeAsync(string nodeId, int samplingInterval = 1000)
        {
            if (_session == null || !_session.Connected || _subscription == null)
            {
                OnStatusChanged?.Invoke("❌ Cannot add node - not connected");
                return false;
            }

            try
            {
                if (_monitoredItems.ContainsKey(nodeId))
                {
                    OnStatusChanged?.Invoke($"⚠️ Node {nodeId} is already monitored");
                    return true;
                }

                var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
                {
                    DisplayName = nodeId,
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    SamplingInterval = samplingInterval,
                    QueueSize = 1,
                    DiscardOldest = true
                };

                monitoredItem.Notification += OnMonitoredItemNotification;
                monitoredItem.Handle = nodeId;

                _subscription.AddItem(monitoredItem);
                _monitoredItems[nodeId] = monitoredItem;

                // Применяем изменения
                _subscription.ApplyChanges();

                OnStatusChanged?.Invoke($"✅ Node added: {nodeId}");
                return true;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"❌ Failed to add node {nodeId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveNodeAsync(string nodeId)
        {
            if (_subscription == null || !_monitoredItems.TryRemove(nodeId, out var monitoredItem))
            {
                return false;
            }

            try
            {
                _subscription.RemoveItem(monitoredItem);
                _subscription.ApplyChanges();
                
                OnStatusChanged?.Invoke($"✅ Node removed: {nodeId}");
                return true;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"❌ Failed to remove node {nodeId}: {ex.Message}");
                return false;
            }
        }

        public List<string> GetMonitoredNodes()
        {
            return _monitoredItems.Keys.ToList();
        }

        public void UpdatePublishingInterval(int publishingInterval)
        {
            if (_subscription != null)
            {
                _subscription.PublishingInterval = publishingInterval;
                _subscription.Modify();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _reconnectTimer?.Dispose();
                CleanupSession();
                OnStatusChanged?.Invoke("📴 Group OPC UA Monitor disposed");
            }
        }
    }
}