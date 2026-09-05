using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenCMIS.App.Core;
using OpenCMIS.CDB.Core;
using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Protocol.Core;
using OpenCMIS.Shared;
using OpenCMIS.Transport.Abstractions;
using OpenCMIS.Transport.I2C.Serial;
using Serilog;

try
{
    await RunAsync(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

static async Task RunAsync(string[] args)
{
    if (args.Length == 0)
    {
        PrintUsage();
        return;
    }

    var command = args[0].ToLowerInvariant();

    // Commands that don't require a device connection
    if (command == "list")
    {
        await ListDevicesAsync();
        return;
    }

    if (command == "help")
    {
        PrintUsage();
        return;
    }

    // Commands that require a port argument
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Error: Device port is required for this command.");
        PrintUsage();
        return;
    }

    var portName = args[1];
    var host     = CreateHost();

    switch (command)
    {
        case "info":    await ShowModuleInfoAsync(host, portName); break;
        case "status":  await ShowStatusAsync(host, portName); break;
        case "monitor": await MonitorDeviceAsync(host, portName); break;
        case "set-state":
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Error: State parameter required.");
                return;
            }

            await SetStateAsync(host, portName, args[2]);
            break;
        case "read":
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Error: Page and address required.");
                return;
            }

            await ReadRegisterAsync(host, portName, byte.Parse(args[2]), byte.Parse(args[3]));
            break;
        case "write":
            if (args.Length < 5)
            {
                Console.Error.WriteLine("Error: Page, address, and value required.");
                return;
            }

            await WriteRegisterAsync(host, portName, byte.Parse(args[2]), byte.Parse(args[3]), byte.Parse(args[4]));
            break;
        case "cdb": await HandleCdbAsync(host, portName, args.Skip(2).ToArray()); break;
        case "app": await HandleAppAsync(host, portName, args.Skip(2).ToArray()); break;
        case "vdm": await HandleVdmAsync(host, portName, args.Skip(2).ToArray()); break;
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            break;
    }
}

static IHost CreateHost()
{
    return Host.CreateDefaultBuilder()
               .UseSerilog((context, config) =>
                               {
                                   config.MinimumLevel.Warning()
                                         .WriteTo.Console();
                               })
               .ConfigureServices(services =>
                                      {
                                          services.AddOpenCmisCore();
                                          services.AddOpenCmisSerialAdapters();
                                      })
               .Build();
}

static async Task ListDevicesAsync()
{
    using var host    = CreateHost();
    var       manager = host.Services.GetRequiredService<IDeviceManager>();
    Console.WriteLine("Scanning for CMIS devices...");
    var devices = await manager.EnumerateDevicesAsync();

    if (!devices.Any())
    {
        Console.WriteLine("No CMIS devices found.");
        return;
    }

    Console.WriteLine($"\nFound {devices.Count()} device(s):");
    foreach (var device in devices)
    {
        Console.WriteLine($"  [{device.ConnectionType}] {device.Name}");
        foreach (var param in device.ConnectionParameters)
            Console.WriteLine($"    {param.Key}: {param.Value}");
    }
}

static async Task<ICmisDevice> ConnectDeviceAsync(IHost host, string portName)
{
    var manager = host.Services.GetRequiredService<IDeviceManager>();
    var deviceInfo = new DeviceInfo
                     {
                         Id             = portName,
                         Name           = $"CMIS Module on {portName}",
                         ConnectionType = ConnectionType.I2C,
                         ConnectionParameters = new()
                                                {
                                                    ["PortName"]     = portName,
                                                    ["BaudRate"]     = "115200",
                                                    ["SlaveAddress"] = "0xA0"
                                                }
                     };

    Console.WriteLine($"Connecting to {portName}...");
    var device = await manager.OpenDeviceAsync(deviceInfo);
    Console.WriteLine("Connected.");
    return device;
}

static async Task ShowModuleInfoAsync(IHost host, string portName)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var info = await device.GetModuleInfoAsync();
        Console.WriteLine("\nModule Information:");
        Console.WriteLine($"  Vendor:       {info.VendorName}");
        Console.WriteLine($"  Part Number:  {info.PartNumber}");
        Console.WriteLine($"  Serial Number:{info.SerialNumber}");
        Console.WriteLine($"  Module Type:  {info.ModuleType}");
        Console.WriteLine($"  CMIS Version: {info.CmisVersion}");
        Console.WriteLine($"  CDB Support:  {info.Capabilities.SupportsCdb}");
        Console.WriteLine($"  Diagnostics:  {info.Capabilities.SupportsDiagnosticMonitoring}");
        Console.WriteLine($"  State Control:{info.Capabilities.SupportsStateControl}");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task ShowStatusAsync(IHost host, string portName)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var status = await device.GetStatusAsync();
        Console.WriteLine("\nModule Status:");
        Console.WriteLine($"  State:    {status.CurrentState}");
        Console.WriteLine($"  Is Ready: {status.IsReady}");
        Console.WriteLine($"  Alerts:   {(status.HasAlerts ? "YES" : "None")}");
        foreach (var alert in status.ActiveAlerts)
            Console.WriteLine($"    - {alert}");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task MonitorDeviceAsync(IHost host, string portName)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var monitor = new DeviceMonitor(device);
        monitor.StatusChanged += (_, args) =>
                                     {
                                         Console.WriteLine(
                                                 $"[{DateTime.Now:HH:mm:ss}] State: {args.OldStatus.CurrentState} -> {args.NewStatus.CurrentState}");
                                     };
        monitor.Alert += (_, args) =>
                             {
                                 Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ALERT [{args.AlertType}]: {args.Message}");
                             };

        Console.WriteLine("Monitoring started. Press Ctrl+C to stop.");
        await monitor.StartMonitoringAsync(TimeSpan.FromSeconds(1));

        // Wait for Ctrl+C
        var tcs = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
                                      {
                                          e.Cancel = true;
                                          tcs.TrySetResult();
                                      };
        await tcs.Task;

        await monitor.StopMonitoringAsync();
        Console.WriteLine("Monitoring stopped.");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task SetStateAsync(IHost host, string portName, string stateName)
{
    if (!Enum.TryParse<ModuleState>(stateName, true, out var targetState))
    {
        Console.Error.WriteLine($"Invalid state: {stateName}. Valid values: {string.Join(", ", Enum.GetNames<ModuleState>())}");
        return;
    }

    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var before = await device.GetStatusAsync();
        Console.WriteLine($"Current state: {before.CurrentState}");

        await device.SetStateAsync(targetState);

        var after = await device.GetStatusAsync();
        Console.WriteLine($"New state: {after.CurrentState}");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task ReadRegisterAsync(IHost host, string portName, byte page, byte address)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var value = await device.RegisterAccess.ReadByteAsync(page, address);
        Console.WriteLine($"Page 0x{page:X2}, Reg 0x{address:X2} = 0x{value:X2} ({value})");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task WriteRegisterAsync(IHost host, string portName, byte page, byte address, byte value)
{
    if (page >= CmisConstants.VdmDescriptorPageStart && page <= CmisConstants.VdmDescriptorPageEnd)
    {
        Console.Error.WriteLine("Writes to CMIS VDM descriptor pages 20h-23h are not supported.");
        return;
    }

    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        await device.RegisterAccess.WriteByteAsync(page, address, value);
        var verify = await device.RegisterAccess.ReadByteAsync(page, address);
        Console.WriteLine($"Written and verified: Page 0x{page:X2}, Reg 0x{address:X2} = 0x{verify:X2} ({verify})");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task HandleCdbAsync(IHost host, string portName, string[] args)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var cdbManager = new CdbManager(new CdbReader(), new CdbWriter(), new CdbValidator());
        var subCommand = args.Length > 0 ? args[0].ToLowerInvariant() : "read";

        if (subCommand == "read")
        {
            Console.WriteLine("Reading CDB...");
            var cdb = await cdbManager.ReadCdbAsync(device);
            Console.WriteLine($"CDB: {cdb.Fields.Count} fields, Checksum=0x{cdb.Checksum:X4}");
            foreach (var field in cdb.Fields)
                Console.WriteLine($"  [{field.Type}] {field.Id} = {field.Value}");
        }
        else
            Console.WriteLine($"Unknown CDB sub-command: {subCommand}");
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task HandleAppAsync(IHost host, string portName, string[] args)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var factory    = new CmisApplicationFactory(device.RegisterAccess);
        var subCommand = args.Length > 0 ? args[0].ToLowerInvariant() : "list";

        switch (subCommand)
        {
            case "list":
                var apps    = await factory.GetSupportedApplicationsAsync();
                var current = await factory.GetCurrentApplicationAsync();
                Console.WriteLine($"Current: {current?.ToString() ?? "Unknown"}");
                Console.WriteLine("Supported:");
                foreach (var app in apps)
                    Console.WriteLine($"  {app}");
                break;

            case "switch":
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("App code required.");
                    return;
                }

                var code = Convert.ToByte(args[1], args[1].StartsWith("0x") ? 16 : 10);
                Console.WriteLine($"Switching to application 0x{code:X2}...");
                await factory.SwitchApplicationAsync(code);
                Console.WriteLine("Switch successful.");
                break;

            default: Console.WriteLine($"Unknown app sub-command: {subCommand}"); break;
        }
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task HandleVdmAsync(IHost host, string portName, string[] args)
{
    var device = await ConnectDeviceAsync(host, portName);
    try
    {
        var subCommand = args.Length > 0 ? args[0].ToLowerInvariant() : "read";

        switch (subCommand)
        {
            case "monitor":
                await MonitorVdmAsync(device);
                break;
            case "read":
            {
                var diag = await device.ReadVdmDiagnosticsAsync();
                PrintVdmDiagnostics(diag);
                break;
            }
            default:
                Console.Error.WriteLine($"Unknown VDM sub-command: {subCommand}. Valid: read, monitor");
                break;
        }
    }
    finally
    {
        await device.CloseAsync();
    }
}

static async Task MonitorVdmAsync(ICmisDevice device)
{
    Console.WriteLine("VDM monitoring started. Press Ctrl+C to stop.");
    var cts = new CancellationTokenSource();
    ConsoleCancelEventHandler cancelHandler = (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };
    Console.CancelKeyPress += cancelHandler;

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var diag = await device.ReadVdmDiagnosticsAsync();
            Console.Clear();
            Console.WriteLine($"VDM Live Monitor — {DateTime.Now:HH:mm:ss}");
            Console.WriteLine(new string('=', 60));
            PrintVdmDiagnostics(diag);
            await Task.Delay(2000, cts.Token);
        }
    }
    catch (TaskCanceledException) { }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
        cts.Dispose();
        Console.WriteLine("\nVDM monitoring stopped.");
    }
}

static void PrintVdmDiagnostics(VdmDiagnostics diag)
{
    if (!diag.IsSupported)
    {
        Console.WriteLine("VDM is not supported by this module or it has no advertised observables.");
        return;
    }

    Console.WriteLine("\nVDM Diagnostics (descriptor-driven, read-only)");
    Console.WriteLine(new string('=', 60));
    Console.WriteLine($"  {"Instance",-8} {"Descriptor",-12} {"Sample",-8} {"High alarm",-12} {"High warning",-13} {"Low warning",-12} {"Low alarm",-10}");
    foreach (var observable in diag.ObservableInstances)
    {
        Console.WriteLine($"  {observable.Instance,-8} {Convert.ToHexString(observable.Descriptor),-12} 0x{observable.Sample:X4}   {ToFlagText(observable.Flags.HighAlarm),-12} {ToFlagText(observable.Flags.HighWarning),-13} {ToFlagText(observable.Flags.LowWarning),-12} {ToFlagText(observable.Flags.LowAlarm),-10}");
    }
}

static string ToFlagText(bool isSet) => isSet ? "set" : "clear";

static void PrintUsage()
{
    Console.WriteLine("OpenCMIS CLI - CMIS 5.2/5.3 Optical Module Control Tool");
    Console.WriteLine("\nUsage:");
    Console.WriteLine("  OpenCMIS.UI.CLI <command> [port] [options]");
    Console.WriteLine("\nCommands:");
    Console.WriteLine("  list                          List available devices");
    Console.WriteLine("  info    <port>                Show module information");
    Console.WriteLine("  status  <port>                Show module status");
    Console.WriteLine("  monitor <port>                Real-time status monitoring");
    Console.WriteLine("  set-state <port> <state>      Set module state (LowPwr/PwrUp/Ready/PwrDn)");
    Console.WriteLine("  read    <port> <page> <addr>  Read a register");
    Console.WriteLine("  write   <port> <page> <addr> <value>  Write a register");
    Console.WriteLine("  cdb     <port> read           Read CDB");
    Console.WriteLine("  app     <port> list           List supported applications");
    Console.WriteLine("  app     <port> switch <code>  Switch to application");
    Console.WriteLine("  vdm     <port> [sub]          Read-only descriptor-driven VDM diagnostics");
    Console.WriteLine("          Sub-commands:");
    Console.WriteLine("            read                 Show generic observable instances (default)");
    Console.WriteLine("            monitor              Live VDM monitoring");
}
