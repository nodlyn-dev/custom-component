using System.Globalization;
using Nodlyn.Core.Connectors;
using Microsoft.Extensions.Logging;
using CoreCtx = Nodlyn.Core.Engine.ExecutionContext;

namespace Nodlyn.CustomComponent;

/// <summary>
/// Executor for the "Number Check" capability.
///
/// Demonstrates how to build a CONDITION node — a node that evaluates
/// to true or false, controlling which branch of the workflow continues.
///
/// KEY DIFFERENCE FROM ACTION NODES:
///   - Condition nodes return a boolean "result" in their output.
///   - The workflow engine uses this result to decide which outbound
///     edge to follow (true branch vs. false branch).
///   - Condition nodes should always be side-effect-free.
///
/// EXAMPLE USAGE IN A WORKFLOW:
///   [Sensor Read] → [Number Check (temp > 30)] → true  → [Send Alert]
///                                               → false → [Log Normal]
/// </summary>
public sealed class NumberCheckExecutor : CapabilityExecutor
{
    public override string ConnectorId  => "custom.example";
    public override string CapabilityId => "numberCheck";

    public override Task<StepOutcome> ExecuteAsync(
        NodeInstance node,
        DeviceInstance device,
        CoreCtx context,
        CancellationToken ct)
    {
        var settings   = node.Settings;
        var inputField = settings.GetValueOrDefault("inputField")?.ToString() ?? "value";
        var op         = settings.GetValueOrDefault("operator")?.ToString() ?? "greaterThan";

        if (!double.TryParse(settings.GetValueOrDefault("threshold")?.ToString(),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold))
        {
            return Task.FromResult(
                StepOutcome.Fail(node.NodeId, node.NodeType, node.Label,
                    "Threshold must be a valid number."));
        }

        // ── Extract the numeric value from pipeline input ───────────────
        var rawInput = ResolveInput(node, context);
        var valueStr = PipelineHelper.ExtractField(rawInput, inputField);

        if (valueStr is null || !double.TryParse(valueStr,
                NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return Task.FromResult(
                StepOutcome.Fail(node.NodeId, node.NodeType, node.Label,
                    $"Field '{inputField}' is missing or not a valid number."));
        }

        // ── Evaluate the condition ──────────────────────────────────────
        var result = op switch
        {
            "greaterThan"    => value > threshold,
            "lessThan"       => value < threshold,
            "equals"         => Math.Abs(value - threshold) < 0.0001,
            "greaterOrEqual" => value >= threshold,
            "lessOrEqual"    => value <= threshold,
            _                => false,
        };

        Logger.LogInformation("NumberCheck: {Value} {Op} {Threshold} = {Result}",
            value, op, threshold, result);

        // For condition nodes, the "result" field drives branch selection.
        return Task.FromResult(
            StepOutcome.Ok(node.NodeId, node.NodeType, node.Label, new
            {
                result,
                value,
                threshold,
                @operator = op,
            }));
    }

}
