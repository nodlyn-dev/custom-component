# Nodlyn Samples

Reference implementations you can share with customers and integrators.

**Open in Visual Studio:** `Samples.sln` (this folder) — standalone solution for client handoff.  
**Full repo:** `RuntimeIngressConsumer` is also included under `Nodlyn.sln` → folder **Samples**.

| Sample | Description |
|--------|-------------|
| [RuntimeIngressConsumer](./RuntimeIngressConsumer/) | Console app that sends **HTTP**, **MQTT**, and **file-drop** events to an exported Nodlyn runtime agent |

These samples are **external consumers** — they talk to the runtime agent the same way an ERP, script, or IoT gateway would. They do **not** use Nodlyn Studio or Cloud APIs.

### Quick start

```bash
cd Samples
dotnet build Samples.sln
dotnet run --project RuntimeIngressConsumer
```

See also: `Nodlyn.Runtime/README.md` (Event Ingress Guide) in the solution root.
