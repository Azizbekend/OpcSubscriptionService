using Microsoft.EntityFrameworkCore;
using OpcSubscriptionService;
using OpcSubscriptionService.Models;
internal class Program
{
    private static async Task Main(string[] args)
    {
        //private static Session session;
        //private static Subscription subscription;
        //private static bool isConnected = true;
        AppDbContext appDbContext = new AppDbContext();
        AppDbContext appDbContext2 = new AppDbContext();
        IncidentRepo incidentRepo = new IncidentRepo(appDbContext2);
        //appDbContext.NodesIndicates.Add(new NodeIndicates
        //{
        //    Indicates = "sdfsdf",
        //    PlcNodeId = "dsfsdf",
        //    TimeStamp = DateTime.Now.ToUniversalTime()

        //});
        //appDbContext.SaveChanges();
        await incidentRepo.CreateIncidentAsync("ns=4;s=|var|PLC210 (Arm32bit + Linux ).Application.BOS_Shapshi.HMI_Motor_20M1.hAlmCom");
        Console.WriteLine("=== OPC UA Group Nodes Monitor ===");

        List<string> nodeIds = new List<string>();
        var hardwares = appDbContext.Hardwares.Where(x => x.ControlBlockId == 6).ToList();
        foreach (var hardware in hardwares)
        {
            var list = appDbContext.Nodes.Where(x => x.PlcNodeId.StartsWith("ns=4") && x.IsCommand == false && x.HardwareId == hardware.Id).ToList();
            foreach (var node in list)
            {
                nodeIds.Add(node.PlcNodeId.Trim());
            }
        }
        //nodeIds.Add("ns=4;s=|var|PLC210 (Arm32bit + Linux ).Application.BOS_Shapshi.HMI_Motor_20M1.hAlmCom");
        string serverUrl = "opc.tcp://192.168.50.2:4840";
        //string serverUrl = "opc.tcp://192.168.50.202:4840";
        //string serverUrl = "opc.tcp://85.141.81.53:4841";
       // string serverUrl = "opc.tcp://85.141.81.53:4842";

        // Создаем монитор
        var monitor = new EnhancedGroupOpcUaMonitor(serverUrl, nodeIds);
        //var monitor2 = new EnhancedGroupOpcUaMonitor(serverUrl1, nodeIds);
        // Подписываемся на события
        monitor.OnValueChanged += (nodeId, value, item) =>
        {

            if (nodeId.EndsWith("hAlmCom") && value.ToString() == "True")
            {

                _ = incidentRepo.CreateIncidentAsync(nodeId);
            }
            if (nodeId.EndsWith("hMode"))
            {
                _ = incidentRepo.CreateHardwareModeEvent(nodeId, value.ToString());
            }
            appDbContext.NodesIndicates.Add(new NodeIndicates
            {
                Indicates = value.ToString(),
                PlcNodeId = nodeId,
                TimeStamp = DateTime.Now.ToUniversalTime()

            });
            appDbContext.SaveChanges();

        };


        //monitor2.OnConnectionStatusChanged += isConnected =>
        //{
        //    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {(isConnected ? "✅ Connected" : "❌ Disconnected")}");
        //};

        //monitor2.OnNodeStatusChanged += (nodeId, status) =>
        //{
        //    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️ Node {nodeId} has bad status: {status}");
        //};

        monitor.OnStatusChanged += message =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ℹ️ {message}");
        };

        monitor.OnConnectionStatusChanged += isConnected =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {(isConnected ? "✅ Connected" : "❌ Disconnected")}");
        };

        monitor.OnNodeStatusChanged += (nodeId, status) =>
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️ Node {nodeId} has bad status: {status}");
        };

        try
        {
            // Запускаем мониторинг
            //monitor2.StartMonitoringAsync(1000);
            await monitor.StartMonitoringAsync(3000);
            //monitor2.StartMonitoringAsync(1000);


            //Console.WriteLine("Press:");
            //Console.WriteLine("  'a' - add random node");
            //Console.WriteLine("  'r' - remove last node");
            //Console.WriteLine("  's' - show statistics");
            //Console.WriteLine("  'q' - quit");

            while (true)
            {
                //var key = Console.ReadKey(true);

                //switch (key.Key)
                //{
                //    case ConsoleKey.A:
                //        var newNode = $"ns=2;s=HelloWorld/Dynamic/Scalar/Int32_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
                //        await monitor.AddNodeAsync(newNode);
                //        break;

                //    case ConsoleKey.R:
                //        var nodes = monitor.GetMonitoredNodes();
                //        if (nodes.Count > 0)
                //        {
                //            await monitor.RemoveNodeAsync(nodes.Last());
                //        }
                //        break;

                //    case ConsoleKey.S:
                //        if (monitor is EnhancedGroupOpcUaMonitor enhancedMonitor)
                //        {
                //            enhancedMonitor.PrintStatistics();
                //        }
                //        break;

                //    case ConsoleKey.Q:
                //        monitor.Dispose();
                //        return;
                //}

                //await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Application error: {ex.Message}");
        }
        finally
        {
            monitor.Dispose();
        }

    }
}