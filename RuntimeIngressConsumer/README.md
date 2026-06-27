# Runtime Ingress Consumer Sample

Reference **external consumer** for a Nodlyn exported runtime agent. Use this project as a starting point when integrating ERP systems, IoT gateways, scripts, or microservices with your deployed agent.

This sample covers all unified ingress channels:

| Channel | What this sample does | Runtime endpoint / mechanism |
|---------|----------------------|------------------------------|
| **HTTP** | `AgentRuntimeClient` | `POST /api/agent/event`, `/data`, `/resume` |
| **MQTT** | `MqttIngressPublisher` | Publish to `nodlyn/{agentId}/event`, `/in`, `/command` |
| **File** | `FileWatchSimulator` | Drop JSON files into a folder the agent watches |

> **Not Studio / Cloud** — heartbeat and design-time APIs are separate. This sample only talks to the **runtime agent** on the customer machine.

---

## Prerequisites

1. Export an **ingress-only** workflow (Manual Start) or any workflow with `eventRoutes` / `fileWatchRoutes` in the manifest.
2. Install and start the runtime agent from the export ZIP.
3. Note from the package:
   - **Agent token** → `AgentToken` in manifest or `README-quickstart.txt`
   - **Dashboard port** → default `9090`
   - **Workflow ID** → manifest / dashboard
   - **Agent ID** → `agent-identity.json` next to the runtime (for MQTT topic base)

Optional for MQTT tests: Mosquitto or any MQTT broker configured on the agent (`Runtime:MqttIngress:Host`).

Optional for file tests: configure file watch on the agent:

```json
{
  "Runtime": {
    "FileWatchIngressEnabled": true,
    "FileWatch": {
      "Routes": [
        {
          "path": "C:/data/inbox",
          "filter": "*.json",
          "workflowId": 1,
          "includeSubdirectories": false
        }
      ]
    }
  }
}
```

---

## Configuration

Edit `appsettings.json`:

```json
{
  "Agent": {
    "BaseUrl": "http://localhost:9090",
    "AgentToken": "your-token-from-export",
    "WorkflowId": 1,
    "EventType": "order.received",
    "ResumeEventName": "payment.received"
  },
  "Mqtt": {
    "Enabled": true,
    "Host": "localhost",
    "Port": 1883,
    "TopicBase": "nodlyn/your-agent-id-from-agent-identity.json"
  },
  "FileWatch": {
    "InboxPath": "C:/data/inbox"
  }
}
```

Environment variable overrides (double underscore for nesting):

```bash
set NODLYN_Agent__AgentToken=your-token
set NODLYN_Agent__BaseUrl=http://localhost:9090
dotnet run
```

---

## Run

From this folder:

```bash
dotnet run
```

Or open **`../Samples.sln`** in Visual Studio and set **RuntimeIngressConsumer** as startup project.

From the `Samples` folder root:

```bash
dotnet build Samples.sln
dotnet run --project RuntimeIngressConsumer
```

Interactive menu — pick HTTP, MQTT, or file-watch scenarios.

### Non-interactive (CI / scripts)

```bash
dotnet run -- http trigger
dotnet run -- http data
dotnet run -- http resume
dotnet run -- http idempotency
dotnet run -- mqtt event
dotnet run -- mqtt in
dotnet run -- mqtt resume
dotnet run -- mqtt command
dotnet run -- file drop
```

---

## HTTP integration (copy into your app)

```csharp
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:9090/") };
client.DefaultRequestHeaders.Add("X-Agent-Token", "YOUR_AGENT_TOKEN");

var body = new
{
    type = "order.received",
    workflowId = 1,
    correlationId = "ext-001",   // optional idempotency key
    payload = new { orderId = "A-100" }
};

var response = await client.PostAsJsonAsync("api/agent/event", body);
// 202 Accepted → { "accepted": true, "action": "trigger", "runId": "..." }
```

Swagger UI: `http://localhost:9090/swagger`

---

## MQTT integration

Publish JSON to `nodlyn/{agentId}/event`:

```json
{
  "type": "sensor.reading",
  "workflowId": 1,
  "payload": { "temp": 22.1 }
}
```

Resume (Wait For Event):

```json
{
  "eventName": "payment.received",
  "workflowId": 1,
  "payload": { "amount": 100 }
}
```

Local operator command (agent machine only):

```json
{ "command": "run", "workflowId": 1 }
```

---

## File watch integration

Your integration writes files; the **agent** watches the folder. This sample writes a JSON order file — no HTTP call required.

Match `FileWatch:InboxPath` in this sample with `Runtime:FileWatch:Routes[].path` on the agent.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| HTTP 401 | `X-Agent-Token` matches export manifest |
| HTTP 404 | Ingress enabled; workflow id / eventRoutes type correct |
| HTTP 409 on resume | `eventName` matches Wait For Event block; run still waiting |
| MQTT no run | Broker host on agent; `TopicBase` matches `agent-identity.json` |
| File no run | Path exists on **agent machine**; filter matches extension |

Full reference: `Nodlyn.Runtime/README.md`

---

## Project structure

```
AgentRuntimeClient.cs    — HTTP ingress (event, data, resume, idempotency)
MqttIngressPublisher.cs  — MQTT publish helpers
FileWatchSimulator.cs    — drop / touch files for file-watch ingress
SampleOptions.cs         — appsettings binding
Program.cs                 — interactive menu + CLI
```

All types are self-contained — copy the `.cs` files into your solution or reference patterns directly.
