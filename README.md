## Nodlyn — Extensible Workflow Automation Engine

Nodlyn is a modern, deterministic workflow automation engine designed for local execution, on-prem environments, and fully isolated runtime agents. It provides a clean execution model, a rich library of built-in connectors, and a lightweight plugin system that allows developers to extend the platform with custom components.

This repository contains sample custom components demonstrating how to integrate new devices, protocols, and actions into the Nodlyn ecosystem. These examples show how to:

- define new connectors and capabilities  
- implement custom executors  
- expose settings and input/output schemas  
- register components through the NodeRegistry  
- run custom logic inside the deterministic ExecutionEngine  
- package components for use in both Nodlyn.Api and Nodlyn.Runtime  

Nodlyn is built for clarity, safety, and predictability. The execution engine is fully deterministic, with no AI involvement in runtime decisions. All workflow logic runs locally, while the optional cloud control plane provides management, audit, licensing, and heartbeat monitoring — without influencing execution.

## About Nodlyn (Main Product Website)

Nodlyn is a complete workflow automation platform with:

- a visual workflow editor  
- a deterministic execution engine  
- a local runtime agent export system  
- device health monitoring  
- audit logging  
- safety gates (approval & arm)  
- plugin‑based extensibility  
- rich built‑in connectors (HTTP, MQTT, File System, ZigBee, IoT Hub, Vision/OCR, Databases, Messaging, Automation, and more)

You can explore the full product, documentation, and platform capabilities at:

https://nodlyn.com

The main site includes the full workflow editor, cloud control plane, runtime export tools, and detailed documentation for building production‑grade automation pipelines.

## What’s Inside This Repository

This repository focuses specifically on custom component development.  
It includes:

- minimal, easy‑to‑follow examples  
- recommended folder structure  
- connector definitions  
- capability executors  
- schema builders  
- registration patterns  
- safety level guidelines  
- packaging notes for runtime agents  

These samples are intended as a starting point for building your own integrations tailored to your environment, devices, or business logic.


## Why Custom Components?

Nodlyn’s architecture is intentionally modular.  
Custom components allow you to:

- integrate proprietary devices  
- add new protocols  
- build domain‑specific actions  
- extend the workflow editor with new nodes  
- run custom logic inside the runtime agent  
- keep your automation fully local and secure  

Whether you’re building industrial automation, IoT workflows, business process automation, or local agents for offline environments, custom components give you full control.

## Getting Started

1. Clone this repository  
2. Review the sample connectors  
3. Build your own component following the same structure  
4. Drop your DLL into `plugins/custom/`  
5. Restart Nodlyn.Api or Nodlyn.Runtime  
6. Your new nodes appear automatically in the workflow editor

## License

MIT License — feel free to use, modify, and extend these samples in your own projects.