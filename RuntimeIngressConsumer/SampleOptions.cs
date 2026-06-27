namespace Nodlyn.Samples.RuntimeIngressConsumer;

/// <summary>
/// Strongly-typed view of <c>appsettings.json</c>.
/// Copy values from your exported runtime package (manifest token, workflow id, MQTT broker).
/// </summary>
public sealed class SampleOptions
{
    public AgentOptions Agent { get; init; } = new();
    public MqttOptions Mqtt { get; init; } = new();
    public FileWatchOptions FileWatch { get; init; } = new();
}

/// <summary>HTTP ingress settings — maps to POST /api/agent/* on the runtime dashboard port.</summary>
public sealed class AgentOptions
{
    /// <summary>Runtime dashboard URL, e.g. http://localhost:9090 (see export manifest DashboardPort).</summary>
    public string BaseUrl { get; init; } = "http://localhost:9090";

    /// <summary>Agent token from export manifest or README-quickstart.txt in the ZIP.</summary>
    public string AgentToken { get; init; } = "";

    /// <summary>Target workflow id. Required for multi-workflow bundles; optional for single-workflow exports.</summary>
    public int? WorkflowId { get; init; }

    /// <summary>Event type used with <c>eventRoutes</c> in the manifest (e.g. order.received, workflow.42).</summary>
    public string EventType { get; init; } = "order.received";

    /// <summary>Event name for Wait For Event resume demos (must match the block name in the workflow).</summary>
    public string ResumeEventName { get; init; } = "payment.received";
}

/// <summary>MQTT publish settings — runtime agent subscribes; this sample publishes as an external system.</summary>
public sealed class MqttOptions
{
    public bool Enabled { get; init; } = true;
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 1883;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";

    /// <summary>Topic prefix, default nodlyn/{agentId} (see agent-identity.json next to the runtime).</summary>
    public string TopicBase { get; init; } = "nodlyn/your-agent-id";

    public string ClientId { get; init; } = "nodlyn-ingress-consumer-sample";
}

/// <summary>File-watch demo — drop files into a folder the runtime watches (configure on the agent).</summary>
public sealed class FileWatchOptions
{
    /// <summary>Same path as Runtime:FileWatch:Routes[].path on the agent machine.</summary>
    public string InboxPath { get; init; } = "C:/data/inbox";

    public string FileNamePrefix { get; init; } = "order-";
    public string FileExtension { get; init; } = ".json";
}
