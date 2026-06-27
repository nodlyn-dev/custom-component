using Nodlyn.Core.Connectors;
using Microsoft.Extensions.Logging;
using CoreCtx = Nodlyn.Core.Engine.ExecutionContext;

namespace Nodlyn.CustomComponent;

/// <summary>
/// Executor for the "Text Transform" capability.
///
/// Demonstrates how to:
///   - Read input from upstream nodes using ResolveInput()
///   - Extract a specific field from the pipeline data
///   - Transform data and pass results downstream
///
/// PIPELINE DATA FLOW:
///   Upstream node output → ResolveInput() → extract field → transform → StepOutcome.Ok(output)
///                                                                          ↓
///                                                                    downstream nodes read this
/// </summary>
public sealed class TextTransformExecutor : CapabilityExecutor
{
    public override string ConnectorId  => "custom.example";
    public override string CapabilityId => "textTransform";

    public override Task<StepOutcome> ExecuteAsync(
        NodeInstance node,
        DeviceInstance device,
        CoreCtx context,
        CancellationToken ct)
    {
        var settings = node.Settings;
        var inputField = settings.GetValueOrDefault("inputField")?.ToString() ?? "message";
        var operation  = settings.GetValueOrDefault("operation")?.ToString() ?? "uppercase";

        // ── 1. Read pipeline input from upstream nodes ──────────────────
        // ResolveInput() is a helper from CapabilityExecutor that merges
        // outputs from all connected upstream nodes.
        var rawInput = ResolveInput(node, context);

        // ── 2. Extract the target field from the input ──────────────────
        var inputText = PipelineHelper.ExtractField(rawInput, inputField);

        if (inputText is null)
        {
            return Task.FromResult(
                StepOutcome.Fail(node.NodeId, node.NodeType, node.Label,
                    $"Field '{inputField}' not found in pipeline input. "
                  + "Check that the upstream node outputs this field."));
        }

        // ── 3. Apply the selected transformation ────────────────────────
        object result = operation switch
        {
            "uppercase" => inputText.ToUpperInvariant(),
            "lowercase" => inputText.ToLowerInvariant(),
            "reverse"   => new string(inputText.Reverse().ToArray()),
            "trim"      => inputText.Trim(),
            "length"    => inputText.Length,
            _           => inputText,
        };

        Logger.LogInformation("TextTransform: {Operation} on '{Input}' → '{Result}'",
            operation, inputText, result);

        return Task.FromResult(
            StepOutcome.Ok(node.NodeId, node.NodeType, node.Label, new
            {
                result,
                operation,
                original = inputText,
            }));
    }

}
