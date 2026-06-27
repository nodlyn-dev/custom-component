using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Nodlyn.Samples.RuntimeIngressConsumer;

/// <summary>
/// Interactive sample — demonstrates all three runtime ingress channels from an external consumer.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "NODLYN_")
            .Build();

        var options = configuration.Get<SampleOptions>() ?? new SampleOptions();

        PrintBanner(options);

        // Non-interactive CLI: dotnet run -- http trigger
        if (args.Length >= 2)
            return await RunCliAsync(options, args);

        return await RunMenuAsync(options);
    }

    private static void PrintBanner(SampleOptions options)
    {
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  Nodlyn Runtime Ingress Consumer — reference sample          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("  This app simulates an external system (ERP / IoT / script)");
        Console.WriteLine("  sending events to your exported Nodlyn runtime agent.");
        Console.WriteLine();
        Console.WriteLine($"  Agent URL  : {options.Agent.BaseUrl}");
        Console.WriteLine($"  WorkflowId : {options.Agent.WorkflowId?.ToString() ?? "(single-workflow export — optional)"}");
        Console.WriteLine($"  Swagger    : {options.Agent.BaseUrl.TrimEnd('/')}/swagger");
        Console.WriteLine();
    }

    private static async Task<int> RunCliAsync(SampleOptions options, string[] args)
    {
        var channel = args[0].ToLowerInvariant();
        var action = args[1].ToLowerInvariant();

        try
        {
            switch (channel)
            {
                case "http":
                    await RunHttpActionAsync(options, action);
                    break;
                case "mqtt":
                    await RunMqttActionAsync(options, action);
                    break;
                case "file":
                    await RunFileActionAsync(options, action);
                    break;
                default:
                    PrintCliUsage();
                    return 1;
            }

            return 0;
        }
        catch (AgentIngressException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return (int)(ex.StatusCode > 0 ? Math.Min(ex.StatusCode, 255) : 1);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static async Task<int> RunMenuAsync(SampleOptions options)
    {
        string? lastDroppedFile = null;

        while (true)
        {
            Console.WriteLine("Choose a test:");
            Console.WriteLine("  [HTTP]");
            Console.WriteLine("    1  Trigger workflow      POST /api/agent/event");
            Console.WriteLine("    2  Send telemetry        POST /api/agent/data");
            Console.WriteLine("    3  Resume Wait For Event POST /api/agent/resume");
            Console.WriteLine("    4  Idempotency demo      same correlationId twice");
            Console.WriteLine("  [MQTT]");
            Console.WriteLine("    5  Publish event         nodlyn/{agentId}/event");
            Console.WriteLine("    6  Publish to /in        nodlyn/{agentId}/in");
            Console.WriteLine("    7  Resume via MQTT");
            Console.WriteLine("    8  Local command         nodlyn/{agentId}/command (pause/run)");
            Console.WriteLine("  [File watch]");
            Console.WriteLine("    9  Drop JSON file        agent must watch FileWatch:InboxPath");
            Console.WriteLine("   10  Touch dropped file    fire Changed event");
            Console.WriteLine("    0  Exit");
            Console.Write("> ");

            var input = Console.ReadLine()?.Trim();
            Console.WriteLine();

            try
            {
                switch (input)
                {
                    case "1":
                        await RunHttpActionAsync(options, "trigger");
                        break;
                    case "2":
                        await RunHttpActionAsync(options, "data");
                        break;
                    case "3":
                        await RunHttpActionAsync(options, "resume");
                        break;
                    case "4":
                        await RunHttpActionAsync(options, "idempotency");
                        break;
                    case "5":
                        await RunMqttActionAsync(options, "event");
                        break;
                    case "6":
                        await RunMqttActionAsync(options, "in");
                        break;
                    case "7":
                        await RunMqttActionAsync(options, "resume");
                        break;
                    case "8":
                        await RunMqttActionAsync(options, "command");
                        break;
                    case "9":
                        lastDroppedFile = await FileWatchSimulator.DropOrderFileAsync(options.FileWatch, options.Agent);
                        break;
                    case "10":
                        if (lastDroppedFile is null)
                            Console.WriteLine("  Drop a file first (option 9).");
                        else
                            await FileWatchSimulator.TouchFileAsync(lastDroppedFile);
                        break;
                    case "0":
                        return 0;
                    default:
                        Console.WriteLine("  Unknown option.");
                        break;
                }
            }
            catch (AgentIngressException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  HTTP error: {ex.Message}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }

    private static async Task RunHttpActionAsync(SampleOptions options, string action)
    {
        using var client = new AgentRuntimeClient(options.Agent);

        switch (action)
        {
            case "trigger":
                Console.WriteLine("[HTTP] Trigger workflow …");
                var trigger = await client.TriggerEventAsync(new
                {
                    orderId = "SAMPLE-1001",
                    channel = "http",
                });
                PrintAccepted(trigger);
                break;

            case "data":
                Console.WriteLine("[HTTP] Telemetry /api/agent/data …");
                var data = await client.TriggerDataAsync(new
                {
                    metric = "temperature",
                    value = 21.8,
                    unit = "C",
                });
                PrintAccepted(data);
                break;

            case "resume":
                Console.WriteLine("[HTTP] Resume Wait For Event …");
                Console.WriteLine($"  eventName = {options.Agent.ResumeEventName}");
                Console.WriteLine("  Tip: start a workflow that waits on this event, then run this option.");
                var resume = await client.ResumeAsync(options.Agent.ResumeEventName, payload: new
                {
                    paymentRef = "PAY-999",
                    status = "captured",
                });
                PrintAccepted(resume);
                break;

            case "idempotency":
                Console.WriteLine("[HTTP] Idempotency demo …");
                await client.DemonstrateIdempotencyAsync();
                break;

            default:
                PrintCliUsage();
                break;
        }
    }

    private static async Task RunMqttActionAsync(SampleOptions options, string action)
    {
        await using var mqtt = new MqttIngressPublisher(options.Mqtt, options.Agent);
        await mqtt.ConnectAsync();

        switch (action)
        {
            case "event":
                Console.WriteLine("[MQTT] Publish workflow trigger …");
                await mqtt.PublishEventAsync();
                break;

            case "in":
                Console.WriteLine("[MQTT] Publish to /in alias …");
                await mqtt.PublishInAsync();
                break;

            case "resume":
                Console.WriteLine("[MQTT] Publish resume event …");
                await mqtt.PublishResumeAsync();
                break;

            case "command":
                Console.WriteLine("[MQTT] Publish local command 'run' …");
                await mqtt.PublishLocalCommandAsync("run", options.Agent.WorkflowId);
                break;

            default:
                PrintCliUsage();
                break;
        }

        Console.WriteLine("  Check the runtime dashboard for the new or resumed run.");
    }

    private static async Task RunFileActionAsync(SampleOptions options, string action)
    {
        switch (action)
        {
            case "drop":
                Console.WriteLine("[File] Drop JSON into watched inbox …");
                await FileWatchSimulator.DropOrderFileAsync(options.FileWatch, options.Agent);
                break;

            case "touch":
                Console.WriteLine("[File] Touch requires a prior drop in interactive mode.");
                break;

            default:
                PrintCliUsage();
                break;
        }
    }

    private static void PrintAccepted(IngressResponse response)
    {
        Console.WriteLine($"  ✓ Accepted  action={response.Action ?? "n/a"}  runId={response.RunId ?? "n/a"}");
        Console.WriteLine("  Open the runtime dashboard to inspect the run.");
    }

    private static void PrintCliUsage()
    {
        Console.WriteLine("""
          Usage:
            dotnet run -- http trigger|data|resume|idempotency
            dotnet run -- mqtt event|in|resume|command
            dotnet run -- file drop

          Configure appsettings.json first (agent token, workflow id, MQTT topic base).
          Environment overrides: NODLYN_Agent__AgentToken, NODLYN_Agent__BaseUrl, etc.
          """);
    }
}
