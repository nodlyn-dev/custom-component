using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nodlyn.Samples.RuntimeIngressConsumer;

/// <summary>
/// Minimal HTTP client for the Nodlyn runtime agent event ingress API.
/// This is the pattern ERP systems, microservices, and scripts should follow.
/// </summary>
/// <remarks>
/// Endpoints (default port 9090):
/// <list type="bullet">
///   <item>POST /api/agent/event — start a workflow (or resume when eventName is set)</item>
///   <item>POST /api/agent/data  — telemetry alias; default type is "data"</item>
///   <item>POST /api/agent/resume — resume a run waiting on Wait For Event</item>
/// </list>
/// Authentication: header <c>X-Agent-Token</c> (from export manifest).
/// Swagger: {BaseUrl}/swagger
/// </remarks>
public sealed class AgentRuntimeClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly AgentOptions _options;

    public AgentRuntimeClient(AgentOptions options, HttpClient? httpClient = null)
    {
        _options = options;
        _http = httpClient ?? new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };

        if (!string.IsNullOrWhiteSpace(options.AgentToken))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Agent-Token", options.AgentToken);
    }

    /// <summary>
    /// Triggers a workflow via POST /api/agent/event.
    /// Returns the run id when the agent accepts the event (HTTP 202).
    /// </summary>
    /// <param name="payload">Arbitrary JSON object passed into the workflow trigger context.</param>
    /// <param name="correlationId">Optional idempotency key — scoped per workflowId on the agent.</param>
    public Task<IngressResponse> TriggerEventAsync(
        object? payload = null,
        string? correlationId = null,
        string? eventType = null,
        int? workflowId = null,
        CancellationToken ct = default)
    {
        var body = BuildEventBody(payload, correlationId, eventType, workflowId);
        return PostAsync("api/agent/event", body, ct);
    }

    /// <summary>
    /// Sends telemetry via POST /api/agent/data (same as /event but default type is "data").
    /// </summary>
    public Task<IngressResponse> TriggerDataAsync(
        object? payload = null,
        int? workflowId = null,
        CancellationToken ct = default)
    {
        var body = BuildEventBody(payload, correlationId: null, eventType: null, workflowId);
        return PostAsync("api/agent/data", body, ct);
    }

    /// <summary>
    /// Resumes a suspended workflow via POST /api/agent/resume.
    /// The agent validates run state synchronously before returning 202 (or 409 on mismatch).
    /// </summary>
    /// <param name="eventName">Must match the Wait For Event block name in the workflow.</param>
    /// <param name="runId">Optional when exactly one run awaits this event.</param>
    public Task<IngressResponse> ResumeAsync(
        string eventName,
        string? runId = null,
        object? payload = null,
        int? workflowId = null,
        CancellationToken ct = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["eventName"] = eventName,
        };

        if (!string.IsNullOrWhiteSpace(runId))
            body["runId"] = runId;
        if (workflowId is int wfId)
            body["workflowId"] = wfId;
        else if (_options.WorkflowId is int defaultWf)
            body["workflowId"] = defaultWf;
        if (payload is not null)
            body["payload"] = payload;

        return PostAsync("api/agent/resume", body, ct);
    }

    /// <summary>
    /// Demonstrates idempotency: two triggers with the same correlationId return the same runId
    /// (unless the previous run failed or was cancelled).
    /// </summary>
    public async Task DemonstrateIdempotencyAsync(CancellationToken ct = default)
    {
        const string key = "sample-idempotency-001";
        Console.WriteLine($"  correlationId = {key}");

        var first = await TriggerEventAsync(
            new { demo = "idempotency", attempt = 1 },
            correlationId: key,
            ct: ct);
        Console.WriteLine($"  First call  → accepted={first.Accepted}, runId={first.RunId}");

        var second = await TriggerEventAsync(
            new { demo = "idempotency", attempt = 2 },
            correlationId: key,
            ct: ct);
        Console.WriteLine($"  Second call → accepted={second.Accepted}, runId={second.RunId}");

        if (first.RunId == second.RunId)
            Console.WriteLine("  ✓ Same runId — idempotency working.");
        else
            Console.WriteLine("  Note: different runIds (prior run may have completed/failed, or correlationId was omitted on agent).");
    }

    private Dictionary<string, object?> BuildEventBody(
        object? payload,
        string? correlationId,
        string? eventType,
        int? workflowId)
    {
        var body = new Dictionary<string, object?>
        {
            ["type"] = eventType ?? _options.EventType,
        };

        var wf = workflowId ?? _options.WorkflowId;
        if (wf is int id)
            body["workflowId"] = id;

        if (!string.IsNullOrWhiteSpace(correlationId))
            body["correlationId"] = correlationId;

        if (payload is not null)
            body["payload"] = payload;

        return body;
    }

    private async Task<IngressResponse> PostAsync(string path, object body, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(path, body, JsonOpts, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            var ok = JsonSerializer.Deserialize<IngressResponse>(text, JsonOpts);
            return ok ?? new IngressResponse { Accepted = true, RawBody = text };
        }

        throw new AgentIngressException(
            (int)response.StatusCode,
            TryReadError(text) ?? response.ReasonPhrase ?? "Request failed",
            text);
    }

    private static string? TryReadError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
        }
        catch { /* ignore */ }

        return null;
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>202 Accepted body from the runtime agent.</summary>
public sealed class IngressResponse
{
    public bool Accepted { get; set; }
    public string? Action { get; set; }
    public string? RunId { get; set; }
    public string? RawBody { get; set; }
}

/// <summary>Thrown when the agent returns 400/401/404/409.</summary>
public sealed class AgentIngressException : Exception
{
    public int StatusCode { get; }

    public AgentIngressException(int statusCode, string message, string? responseBody)
        : base($"HTTP {statusCode}: {message}")
    {
        StatusCode = statusCode;
        if (!string.IsNullOrWhiteSpace(responseBody))
            Console.WriteLine($"  Response body: {responseBody}");
    }
}
