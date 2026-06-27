using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MQTTnet;
using MQTTnet.Client;

namespace Nodlyn.Samples.RuntimeIngressConsumer;

/// <summary>
/// Publishes JSON events to the Nodlyn runtime agent MQTT ingress topics.
/// The runtime subscribes to nodlyn/{agentId}/event, /in, and /command.
/// </summary>
/// <remarks>
/// MQTT ingress is independent from per-workflow MQTT triggers inside Nodlyn Studio.
/// Broker credentials are configured on the agent (Runtime:MqttIngress in appsettings.json),
/// not the X-Agent-Token used for HTTP.
/// </remarks>
public sealed class MqttIngressPublisher : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly MqttOptions _options;
    private readonly AgentOptions _agent;
    private IMqttClient? _client;

    public MqttIngressPublisher(MqttOptions mqtt, AgentOptions agent)
    {
        _options = mqtt;
        _agent = agent;
    }

    /// <summary>Connects to the broker (call once before publishing).</summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("MQTT is disabled in appsettings.json (Mqtt:Enabled = false).");

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.Host, _options.Port)
            .WithClientId(_options.ClientId)
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(_options.Username))
            builder = builder.WithCredentials(_options.Username, _options.Password);

        await _client.ConnectAsync(builder.Build(), ct);
        Console.WriteLine($"  Connected to MQTT {_options.Host}:{_options.Port} as {_options.ClientId}");
    }

    /// <summary>
    /// Publishes a workflow trigger to nodlyn/{agentId}/event.
    /// Payload shape matches HTTP POST /api/agent/event.
    /// </summary>
    public Task PublishEventAsync(object? payload = null, string? eventType = null, CancellationToken ct = default)
    {
        var topic = $"{_options.TopicBase.Trim('/')}/event";
        var message = new
        {
            type = eventType ?? _agent.EventType,
            workflowId = _agent.WorkflowId,
            correlationId = $"mqtt-{Guid.NewGuid():N}",
            payload = payload ?? new { source = "mqtt-sample", temp = 22.5 },
        };

        return PublishJsonAsync(topic, message, ct);
    }

    /// <summary>
    /// Publishes to nodlyn/{agentId}/in — same routing as /event (alias topic).
    /// </summary>
    public Task PublishInAsync(object? payload = null, CancellationToken ct = default)
    {
        var topic = $"{_options.TopicBase.Trim('/')}/in";
        var message = new
        {
            type = "sensor.reading",
            workflowId = _agent.WorkflowId,
            payload = payload ?? new { sensorId = "S-01", value = 42 },
        };

        return PublishJsonAsync(topic, message, ct);
    }

    /// <summary>
    /// Publishes a resume event (Wait For Event) over MQTT — same fields as HTTP /resume.
    /// </summary>
    public Task PublishResumeAsync(string? eventName = null, object? payload = null, CancellationToken ct = default)
    {
        var topic = $"{_options.TopicBase.Trim('/')}/event";
        var message = new
        {
            eventName = eventName ?? _agent.ResumeEventName,
            workflowId = _agent.WorkflowId,
            payload = payload ?? new { amount = 100, currency = "EUR" },
        };

        return PublishJsonAsync(topic, message, ct);
    }

    /// <summary>
    /// Publishes a local operator command to nodlyn/{agentId}/command.
    /// These are handled on the agent machine only (pause/resume/run) — not cloud heartbeat.
    /// </summary>
    public Task PublishLocalCommandAsync(string command, int? workflowId = null, CancellationToken ct = default)
    {
        var topic = $"{_options.TopicBase.Trim('/')}/command";
        object message = workflowId is int wf
            ? new { command, workflowId = wf }
            : new { command };

        return PublishJsonAsync(topic, message, ct);
    }

    private async Task PublishJsonAsync(string topic, object payload, CancellationToken ct)
    {
        if (_client is null || !_client.IsConnected)
            throw new InvalidOperationException("Call ConnectAsync() first.");

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var msg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(msg, ct);
        Console.WriteLine($"  Published to {topic}");
        Console.WriteLine($"  Payload: {json}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is null)
            return;

        if (_client.IsConnected)
        {
            try { await _client.DisconnectAsync(); }
            catch { /* ignore on shutdown */ }
        }

        _client.Dispose();
    }
}
