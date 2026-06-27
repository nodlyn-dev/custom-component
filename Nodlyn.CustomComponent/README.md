# Nodlyn Custom Component Plugin

A template project for building custom workflow components for the [Nodlyn](https://nodlyn.com) automation platform.

## Overview

This project demonstrates how to create a custom plugin that adds new workflow nodes to the Nodlyn platform. Once built, the plugin DLL is placed in the `plugins/custom/` directory and automatically loaded at startup.

### What's Included

| Node | Type | Description |
|------|------|-------------|
| **Hello World** 👋 | Action | A minimal example that builds a greeting message from settings |
| **Text Transform** ✏️ | Transform | Reads pipeline input, applies text operations (uppercase, lowercase, reverse, trim, length) |
| **Number Check** 🔢 | Condition | Evaluates a numeric value against a threshold to control workflow branching |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A running instance of the Nodlyn platform

## Quick Start

### 1. Clone this repository

Clone this repository **inside** the Nodlyn source tree (as a sibling of `Nodlyn.Api/`):

```bash
cd <your-nodlyn-source-root>
git clone https://github.com/nodlyn/nodlyn-custom-component.git Nodlyn.CustomComponent
cd Nodlyn.CustomComponent
```

> **Note:** The project references `Nodlyn.Core` via a relative path.
> If you prefer to develop outside the source tree, replace the `ProjectReference`
> in the `.csproj` with a NuGet `PackageReference` once the Nodlyn SDK package is available.

### 2. Build the plugin

```bash
dotnet build -c Release
```

The compiled DLL will be at:
```
src/Nodlyn.CustomComponent/bin/Release/net8.0/Nodlyn.CustomComponent.dll
```

### 3. Deploy to Nodlyn

Copy the DLL to your Nodlyn platform's plugin directory:

```bash
# Windows
copy src\Nodlyn.CustomComponent\bin\Release\net8.0\Nodlyn.CustomComponent.dll ^
     <your-nodlyn-path>\plugins\custom\

# Linux / macOS
cp src/Nodlyn.CustomComponent/bin/Release/net8.0/Nodlyn.CustomComponent.dll \
   <your-nodlyn-path>/plugins/custom/
```

### 4. Restart the platform

Restart the Nodlyn server. Your custom nodes will appear in the **Custom** section of the workflow editor sidebar.

## Creating Your Own Plugin

### Project Structure

```
src/Nodlyn.CustomComponent/
├── CustomConnectorDefinition.cs    # Registers the plugin and declares capabilities
├── Executors/
│   ├── HelloWorldExecutor.cs       # Simplest possible executor
│   ├── TextTransformExecutor.cs    # Demonstrates pipeline data flow
│   └── NumberCheckExecutor.cs      # Demonstrates condition nodes
└── Nodlyn.CustomComponent.csproj   # Project file (references Nodlyn.Core)
```

### Step-by-Step Guide

#### 1. Define Your Connector

Create a class that extends `DeviceConnectorDefinition`. This registers your plugin and declares all its capabilities:

```csharp
public class MyConnectorDefinition : DeviceConnectorDefinition
{
    public override string ConnectorId  => "custom.mycompany";   // Must be globally unique
    public override string DisplayName  => "My Custom Nodes";
    public override string Icon         => "🔧";
    public override string Color        => "#3b82f6";
    public override string Description  => "Description shown in the UI.";

    public override IReadOnlyList<CapabilityContract> Capabilities =>
    [
        new()
        {
            CapabilityId   = "myAction",        // Must match executor's CapabilityId
            DisplayName    = "My Action",
            Icon           = "⚡",
            CapabilityType = CapabilityType.Action,
            SecurityLevel  = SecurityLevel.Safe,
            HasInput       = true,
            HasOutput      = true,
            Settings =
            [
                new() { Name = "param1", Label = "Parameter", Type = "text", DefaultValue = "default" },
            ],
        },
    ];
}
```

#### 2. Create an Executor

Create a class that extends `CapabilityExecutor`. The `ConnectorId` + `CapabilityId` must match:

```csharp
public sealed class MyActionExecutor : CapabilityExecutor
{
    public override string ConnectorId  => "custom.mycompany";
    public override string CapabilityId => "myAction";

    public override Task<StepOutcome> ExecuteAsync(
        NodeInstance node, DeviceInstance device,
        ExecutionContext context, CancellationToken ct)
    {
        var param1 = node.Settings.GetValueOrDefault("param1")?.ToString() ?? "default";

        // Your logic here...

        return Task.FromResult(
            StepOutcome.Ok(node.NodeId, node.NodeType, node.Label,
                new { result = "done", param1 }));
    }
}
```

#### 3. Build & Deploy

```bash
dotnet build -c Release
# Copy DLL to plugins/custom/ and restart the platform
```

### Setting Types

| Type | C# Value | UI Control |
|------|----------|------------|
| `text` | `string` | Text input |
| `number` | `string` (parse with `int.Parse` / `double.Parse`) | Number input |
| `toggle` | `"true"` / `"false"` | Toggle switch |
| `select` | `string` (selected option value) | Dropdown |
| `textarea` | `string` | Multi-line text area |

### Capability Types

| Type | Use For |
|------|---------|
| `Action` | Performing operations (send email, call API, write file) |
| `Condition` | Evaluating true/false to control branching |
| `Transform` | Reshaping data between nodes |
| `Trigger` | Starting workflow execution from external events |
| `Stateful` | Maintaining state across executions (debounce, latch) |
| `FlowControl` | Controlling execution paths (fork, merge, loop) |
| `Integration` | External system integration (REST, webhooks) |

### Security Levels

| Level | Behavior |
|-------|----------|
| `Safe` | No special permission needed |
| `Sensitive` | Requires credential or confirmation |
| `Dangerous` | Requires explicit operator approval before execution |
| `ArmedRequired` | Requires approval AND an arm signal (two-person rule) |

### Reading Pipeline Input

Use the inherited `ResolveInput()` method to access data from upstream nodes:

```csharp
var input = ResolveInput(node, context);

// Use the shared PipelineHelper to extract a field from the pipeline:
var value = PipelineHelper.ExtractField(input, "fieldName");

// Or manually, if the input is a dictionary:
if (input is Dictionary<string, object?> dict)
{
    dict.TryGetValue("fieldName", out var val);
    var text = val?.ToString();
}
```

### Dry Run Support

If your executor performs side effects (sending emails, calling APIs, controlling hardware), check for dry run mode:

```csharp
if (context.ExecutionMode == ExecutionMode.DryRun)
{
    return Task.FromResult(
        StepOutcome.Ok(node.NodeId, node.NodeType, node.Label,
            new { result = "simulated", __dryRun = true }));
}
```

## Plugin Rules

- **Only reference Nodlyn.Core.** Do not reference Nodlyn.Api or Nodlyn.Connectors.
- **Parameterless constructors required.** The plugin loader creates instances using `Activator.CreateInstance()`.
- **ConnectorId must be unique.** Use a namespace like `custom.yourcompany` to avoid collisions.
- **No hot reload.** Plugins are loaded at startup only. Restart the platform after deploying changes.
- **All or nothing.** If any type in your DLL fails validation, the entire plugin is rejected.

## License

MIT License — see [LICENSE](LICENSE) for details.

## Links

- [Nodlyn Platform](https://nodlyn.com)
- [Plugin API Reference](https://nodlyn.com/docs/plugins)
