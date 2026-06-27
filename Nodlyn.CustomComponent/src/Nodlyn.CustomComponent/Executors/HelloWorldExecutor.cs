using Nodlyn.Core.Connectors;
using Microsoft.Extensions.Logging;
using CoreCtx = Nodlyn.Core.Engine.ExecutionContext;

namespace Nodlyn.CustomComponent;

/// <summary>
/// Executor for the "Hello World" capability.
///
/// This is the simplest possible executor — it reads settings from the node,
/// builds a greeting message, and returns it as output.
///
/// KEY CONCEPTS:
///   - ConnectorId + CapabilityId must match the CapabilityContract in your connector definition.
///   - ExecuteAsync receives the node's settings, the bound device (if any), and the execution context.
///   - Return StepOutcome.Ok() with output data on success, or StepOutcome.Fail() on error.
///   - Always check ExecutionMode.DryRun if your executor has side effects (this one doesn't).
///   - Use ResolveInput() to read data from upstream nodes in the pipeline.
///   - Use Logger (inherited from CapabilityExecutor) for structured logging.
/// </summary>
public sealed class HelloWorldExecutor : CapabilityExecutor
{
    // These two properties link this executor to the matching CapabilityContract.
    // The platform uses ConnectorId + CapabilityId to find the right executor for each node.
    public override string ConnectorId  => "custom.example";
    public override string CapabilityId => "helloWorld";

    public override Task<StepOutcome> ExecuteAsync(
        NodeInstance node,
        DeviceInstance device,
        CoreCtx context,
        CancellationToken ct)
    {
        // ── 1. Read settings from the node ──────────────────────────────
        // Settings come from the inspector panel in the workflow editor.
        // They are stored as Dictionary<string, object?> on the node.
        var settings = node.Settings;

        var name = settings.GetValueOrDefault("name")?.ToString() ?? "World";
        var style = settings.GetValueOrDefault("greeting")?.ToString() ?? "friendly";
        var includeTimestamp = settings.GetValueOrDefault("includeTimestamp")?.ToString() != "false";

        // ── 2. Build the greeting based on the selected style ───────────
        var greeting = style switch
        {
            "formal" => $"Good day, {name}.",
            "casual" => $"Hey, {name}!",
            _        => $"Hello, {name}!",
        };

        Logger.LogInformation("HelloWorld executed: {Greeting}", greeting);

        // ── 3. Return success with output data ──────────────────────────
        // The output object is serialized to JSON and becomes available
        // to downstream nodes via the pipeline.
        var output = new Dictionary<string, object?>
        {
            ["message"] = greeting,
        };

        if (includeTimestamp)
            output["greetedAt"] = DateTime.UtcNow.ToString("o");

        return Task.FromResult(
            StepOutcome.Ok(node.NodeId, node.NodeType, node.Label, output));
    }
}
