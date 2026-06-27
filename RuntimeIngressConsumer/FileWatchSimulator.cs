using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nodlyn.Samples.RuntimeIngressConsumer;

/// <summary>
/// Simulates an external system that delivers files into a folder watched by the runtime agent.
/// </summary>
/// <remarks>
/// File ingress is configured on the <b>agent</b>, not in this sample:
/// <list type="bullet">
///   <item>Export manifest <c>fileWatchRoutes</c>, or</item>
///   <item>Runtime:FileWatch:Routes in the agent's appsettings.json</item>
/// </list>
/// The agent debounces rapid Created/Changed events (400 ms) before starting a workflow.
/// Route <c>type</c> in the manifest (e.g. file.created) can be matched via eventRoutes.
/// </remarks>
public static class FileWatchSimulator
{
    /// <summary>
    /// Writes a JSON file into the inbox folder. If the runtime watches this path with filter *.json,
    /// it will route a file ingress event and start the configured workflow.
    /// </summary>
    public static async Task<string> DropOrderFileAsync(
        FileWatchOptions options,
        AgentOptions agent,
        CancellationToken ct = default)
    {
        var directory = options.InboxPath.Trim();
        Directory.CreateDirectory(directory);

        var fileName = $"{options.FileNamePrefix}{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}{options.FileExtension}";
        var fullPath = Path.Combine(directory, fileName);

        // The file content becomes workflow payload context on the agent side.
        var order = new
        {
            orderId = $"ORD-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            customer = "Sample Customer Ltd.",
            lines = new[]
            {
                new { sku = "SKU-100", qty = 2 },
                new { sku = "SKU-200", qty = 1 },
            },
            source = "RuntimeIngressConsumer sample",
            workflowId = agent.WorkflowId,
        };

        var json = JsonSerializer.Serialize(order, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fullPath, json, ct);

        Console.WriteLine($"  Dropped file: {fullPath}");
        Console.WriteLine("  Ensure the runtime agent watches this folder, e.g.:");
        Console.WriteLine("""
          "Runtime": {
            "FileWatchIngressEnabled": true,
            "FileWatch": {
              "Routes": [
                { "path": "C:/data/inbox", "filter": "*.json", "workflowId": 1 }
              ]
            }
          }
          """);

        return fullPath;
    }

    /// <summary>
    /// Updates an existing file to demonstrate Changed events (also debounced on the agent).
    /// </summary>
    public static async Task TouchFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Drop a file first.", filePath);

        var json = await File.ReadAllTextAsync(filePath, ct);
        var node = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("Expected JSON object in dropped file.");

        node["touchedAt"] = DateTime.UtcNow.ToString("O");
        await File.WriteAllTextAsync(filePath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), ct);
        Console.WriteLine($"  Updated file: {filePath}");
    }
}
