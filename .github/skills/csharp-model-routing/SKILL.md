---
name: csharp-model-routing
description: 'Route to the most cost-effective OpenRouter model for C# agent delegation. Use when selecting a model for Planner, Implementer, or Debugging work based on task complexity. Returns a deterministic model recommendation as JSON.'
---

# C# Model Routing

## Purpose

Deterministic routing logic that recommends the single most cost-effective (cheapest) model based on two inputs: **Category** and **Complexity**. Used by orchestrator agents before delegating planning or implementation work to sub-agents.

## Input Parameters

| Parameter | Values | Description |
|-----------|--------|-------------|
| **Category** | `Planner`, `Implementer`, `Debugging`, `Testing` | The type of work being delegated |
| **Complexity** | `Low`, `Medium`, `High` | The difficulty/scope of the task |

## Allowed Models

These are the models available via the agent tool. The `model` field is the EXACT string the orchestrator must pass to sub-agents.

1. **DeepSeek V4 Flash (deepseek)** — Ultra-lightweight, 1M context, ~$0.14/$0.28 per 1M.
2. **Xiaomi: MiMo-V2.5 (openrouter)** — Lightweight generalist, 1M context, ~$0.14/$0.28 per 1M.
3. **Xiaomi: MiMo-V2.5-Pro (openrouter)** — Mid-tier production engine, 1M context, ~$0.435/$0.87 per 1M.
4. **DeepSeek V4 Pro (deepseek)** — Advanced coding & reasoning flagship, 1M context, ~$0.435/$0.87 per 1M.
5. **Z.ai: GLM 5.2 (openrouter)** — Mid-tier reasoning & tool agent, 1M context, ~$0.60/$1.20 per 1M.
6. **MiniMax: MiniMax M3 (openrouter)** — Heavy multi-step automation, 1M context, ~$0.80/$1.60 per 1M.

## Routing Algorithm

### Step 1: Apply Category Preferences

- **Implementer / Debugging**: Prefers raw throughput / coding accuracy → Flash → Pro → M3
- **Planner**: Prefers structured reasoning / planning depth → Pro → M3 → GLM 5.2
- **Testing**: Prefers cost efficiency for test writing → Flash → Pro (Flash for most unit tests, Pro only for complex mocking or integration tests)

### Step 2: Apply Complexity Scaling

- **Low**: Cheapest tier in the category's preference order
- **Medium**: Middle tier
- **High**: Most capable tier

### Step 3: Select Best-for-Buck

Evaluate in hierarchical order: **Category → Complexity**. Select the lowest-priced model matching the category preference at the given complexity.

## Routing Table

| Category | Complexity | Model (exact agent string) | Short Name |
|:---------|:-----------|:---------------------------|:-----------|
| **Implementer / Debugging** | Low | `DeepSeek V4 Flash (deepseek)` | DeepSeek V4 Flash |
| **Implementer / Debugging** | Medium | `DeepSeek V4 Pro (deepseek)` | DeepSeek V4 Pro |
| **Implementer / Debugging** | High | `MiniMax: MiniMax M3 (openrouter)` | MiniMax M3 |
| **Planner** | Low | `DeepSeek V4 Pro (deepseek)` | DeepSeek V4 Pro |
| **Planner** | Medium | `MiniMax: MiniMax M3 (openrouter)` | MiniMax M3 |
| **Planner** | High | `Z.ai: GLM 5.2 (openrouter)` | GLM 5.2 |
| **Testing** | Low | `DeepSeek V4 Flash (deepseek)` | DeepSeek V4 Flash |
| **Testing** | Medium | `DeepSeek V4 Pro (deepseek)` | DeepSeek V4 Pro |
| **Testing** | High | `DeepSeek V4 Pro (deepseek)` | DeepSeek V4 Pro |

Lightweight alternatives (same price tier, different vendor): `Xiaomi: MiMo-V2.5 (openrouter)` and `Xiaomi: MiMo-V2.5-Pro (openrouter)` can substitute Flash and Pro tiers respectively if preferred.

Fallback: `MiniMax: MiniMax M3 (openrouter)` and `Z.ai: GLM 5.2 (openrouter)` are held as fallback options if latency/uptime drops on primary routes.

## Prompt Caching Guidance

To maximize prompt caching (lower latency and cost), orchestrators should **reuse the same model for the same category within a session**. When an orchestrator calls the model routing skill multiple times in one session (e.g. planning multiple features), it should:

1. Cache the returned model per category on first call (e.g. in session memory)
2. Reuse that cached model for subsequent delegations of the same category
3. Only re-route if the complexity tier changes (e.g. a later task is High when the first was Low)

This means a session that plans three Low-complexity features should delegate all three to the same `Planner` model, not re-evaluate each time.

## Output Format

Respond with the following JSON schema. No conversational filler. The `model` field MUST be the exact string from the Routing Table.

```json
{
  "model": "Vendor: Model Name (openrouter)",
  "short_name": "Human-readable model name",
  "routing_reason": "Brief 1-sentence logic explaining why this is the cheapest viable option."
}
```

## Execution Rules

1. Validate inputs before routing — reject unknown categories or complexity values
2. Apply category first — determines the preference order (coding vs planning)
3. Apply complexity second — selects the tier within the category's preference order
4. Return only the JSON block — no preamble, no explanation outside the `routing_reason` field
5. Never invent models — the `model` field MUST be an exact copy from the Routing Table
6. Fall back to next-tier model if the primary model is unavailable