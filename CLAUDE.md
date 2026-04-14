# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

COP v1 is a cTrader/cAlgo trading robot (C# / .NET 6.0) that adds an interactive order management panel to the charting platform. It handles limit/market order placement with risk-based position sizing, draggable chart lines, multi-language UI, and persistent state across sessions.

## Build

```bash
dotnet build "cop v1/cop v1.csproj"
```

Output: `bin/Debug/net6.0/cop v1.algo` — load this file in cTrader's AlgoTrader.

There is no automated test suite. Verification is done by running the robot in the cTrader backtester or simulator.

## Architecture

The robot follows an MVC-style structure with a clear separation:

```
COP.cs (Robot entry point)
├── UI/MainPanel.cs        — Panel layout, input fields, buttons, collapsible state
├── UI/PanelStyles.cs      — Centralized colors, dimensions, button styles
├── UI/Localization.cs     — All user-visible strings in 9 languages
├── Chart/ChartLineManager.cs   — Entry/SL/TP lines: draw, drag, restore after timeframe change
├── Chart/FastOrderHandler.cs   — Click-based order entry (3 clicks Limit, 2 clicks Market)
├── Trading/OrderManager.cs     — Executes orders via cTrader API
├── Trading/RiskCalculator.cs   — Volume from risk %, USD, or EUR; multi-TP volume splits
└── Trading/LevelValidator.cs   — Validates Entry/SL/TP and detects Long/Short direction
```

**Data flow:** User input (panel or chart click) → `COP.RecalculateAll()` → `LevelValidator` → `RiskCalculator` → panel display update + chart label refresh → on submit: `OrderManager`.

**State persistence** uses cTrader's `LocalStorage` (Device scope) for risk values, panel collapse state, transparency, scale, and chart level prices across timeframe switches.

**Timeframe switch handling (MAR-49):** When the chart type/timeframe changes, all chart objects are destroyed. `ChartLineManager` caches line prices and `RepairTradingLinesIfNeeded()` recreates them on the next `OnTick`.

## Key Constraints

**Localization is mandatory.** Any addition or change of user-visible text (labels, tooltips, error messages, chart line annotations) must be applied to **all** language dictionaries in `UI/Localization.cs` simultaneously. Missing translations show as raw keys — treat as a bug.

**cAlgo API limitations:**
- Chart object labels only support a limited character set; avoid Unicode that cTrader doesn't render.
- `Symbol.VolumeInUnitsStep` normalization is required before placing any order.
- `AssetConverter.Convert()` is needed for USD/EUR risk mode to convert to the account currency.
- Z-index on chart objects must be set high (see `PanelStyles`) to keep lines above price bars.

**Volume calculation formula:**
```
VolumeInUnits = RiskAmount / (SL_distance_in_pips × PipValue)
→ Normalize to VolumeInUnitsStep, clamp to [VolumeInUnitsMin, VolumeInUnitsMax]
```

## Active Development

The **multi-TP feature** (2–3 takeprofit levels) is planned next (see `TODO.md`). The implementation roadmap is fully specified there including volume distribution logic (equal volume vs. equal profit), panel layout changes, chart line handling per order mode, and integration steps.

Current branch: `ui-design-session`. Feature development targets `v2` branch.
