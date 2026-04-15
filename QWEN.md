# QWEN.md — COP v1

## Project Overview

**COP v1** is a cTrader/cAlgo trading robot (C# / .NET 6.0) that provides an interactive order management panel on the chart. It handles limit/market order placement with risk-based position sizing, draggable chart lines (Entry, SL, TP), multi-language UI (9 languages), and persistent state across sessions and timeframe changes.

**Key features:**
- **Limit/Market order modes** with visual chart lines
- **Risk-based volume calculation** (% of balance, USD, or EUR)
- **Fast Order mode** — 3 clicks for Limit, 2 clicks for Market
- **Multi Take-Profit** — up to 3 TP levels with volume split (equal volume or equal profit)
- **Collapsible panel** with mini action bar (LM / MK / OK / FST)
- **Localization** — EN, RU, DE, FR, ES, IT, PL, NL, PT
- **State persistence** via cTrader LocalStorage (Device scope)

## Build & Run

### Build
```bash
dotnet build "cop v1/cop v1.csproj"
```

Output: `bin/Debug/net6.0/cop v1.algo` — load this file in cTrader's Algo section.

### Run
No automated tests. Verification is done by running the robot in cTrader's backtester or live chart simulator.

## Architecture

MVC-style structure with clear separation of concerns:

```
COP.cs (Robot entry point — lifecycle, events, recalculation)
├── UI/
│   ├── MainPanel.cs        — Panel layout, input fields, buttons, collapsible state, mini-panel
│   ├── PanelStyles.cs      — Colors, dimensions, button styles, Z-index constants
│   └── Localization.cs     — All user-visible strings in 9 languages
├── Chart/
│   ├── ChartLineManager.cs   — Entry/SL/TP lines: draw, drag, restore after timeframe change
│   └── FastOrderHandler.cs   — Click-based order entry (3 clicks Limit, 2 clicks Market)
└── Trading/
    ├── OrderManager.cs       — Executes orders via cTrader API (PlaceLimitOrder, ExecuteMarketOrder)
    ├── RiskCalculator.cs     — Volume from risk %/USD/EUR; multi-TP volume splits; normalize volume
    └── LevelValidator.cs     — Validates Entry/SL/TP and detects Long/Short direction
```

**Data flow:**
User input (panel fields or chart click) → `COP.RecalculateAll()` → `LevelValidator.Validate()` → `RiskCalculator` calculations → update panel display + chart labels → on submit: `OrderManager` places order.

## Key Constraints & Rules

### 1. Localization is MANDATORY

Any addition or change of user-visible text (labels, tooltips, error messages, chart line annotations) **must** be applied to **all** language dictionaries in `UI/Localization.cs` simultaneously. Missing translations show as raw keys — treat as a bug.

Source: `cop v1/UI/Localization.cs`. Rule also defined in `.cursor/rules/localization.mdc`.

### 2. Robot parameters are English-only

Parameter names and enum values in the cTrader settings window are **always in English** (e.g., `Fast Order Mode = No / Yes`). UI panel labels are localized.

### 3. cAlgo API limitations

- Chart object labels support a limited character set; avoid Unicode that cTrader doesn't render.
- `Symbol.VolumeInUnitsStep` normalization is **required** before placing any order.
- `AssetConverter.Convert()` is needed for USD/EUR risk mode to convert to account currency.
- Z-index on chart objects must be set high (see `PanelStyles.ChartTradingLineZIndex`) to keep lines above price bars.

### 4. cTrader UI Layout — Critical Rules

These rules were derived from debugging symmetric-margin failures in `MainPanel.cs`. Violating them produces invisible-looking code that renders asymmetrically.

#### Rule 1 — Grid with all-Star columns MUST have explicit Width

A `Grid` whose **every** column uses `SetWidthInStars()` does **not** auto-expand through a `StackPanel` parent in cTrader. It collapses to minimum content width.

```csharp
// WRONG — renders narrow + left-aligned:
var myGrid = new Grid(1, 2) { HorizontalAlignment = HorizontalAlignment.Stretch };
myGrid.Columns[0].SetWidthInStars(1);
myGrid.Columns[1].SetWidthInStars(1);

// CORRECT — explicit width guarantees symmetric Star column split:
var myGrid = new Grid(1, 2) { Width = PanelStyles.ContentWidth };
myGrid.Columns[0].SetWidthInStars(1);
myGrid.Columns[1].SetWidthInStars(1);
```

A Grid with **at least one Auto column** works without explicit Width.

#### Rule 2 — HorizontalAlignment.Stretch is unreliable for Grid inside StackPanel

`HorizontalAlignment = HorizontalAlignment.Stretch` on a `Grid` child of a `StackPanel` does **not** reliably provide the full parent width in cTrader. Always prefer `Width = PanelStyles.ContentWidth` for row-level Grids.

#### Rule 3 — Do NOT add Padding to _rootWrapper

Adding `Padding` to `_rootWrapper` shrinks the actual content area, but inner containers still use `Width = PanelClientWidth` — they overflow and shift content. Content inset is via `_contentStack.Margin = ContentStackHorizontalMargin` only.

#### Rule 4 — Width propagation chain

```
_rootWrapper (Border, Width=PanelWidth, NO Padding)
  _rootContainer (StackPanel, Width=PanelClientWidth)
    _mainPanelRoot (StackPanel, Width=PanelClientWidth)
      _mainStack (StackPanel, Width=PanelClientWidth)
        _contentStack (StackPanel, Margin=ContentStackHorizontalMargin)
          rows with Width=ContentWidth  ← every Star-only Grid needs this
```

Any StackPanel in this chain **without** explicit Width may fail to pass a constrained width to its children.

#### Rule 5 — settingsRowsStack rows follow the same rule

Every `Grid` row inside the settings panel must also have `Width = PanelStyles.ContentWidth`.

### 5. Volume Calculation Formula

```
VolumeInUnits = RiskAmount / (SL_distance_in_pips × PipValue)
→ Normalize to VolumeInUnitsStep, clamp to [VolumeInUnitsMin, VolumeInUnitsMax]
```

### 6. State Persistence

Uses cTrader's `LocalStorage` (Device scope) for:
- Risk values (MaxRiskPercent)
- Panel collapse state
- Panel transparency and scale
- Chart level prices (Entry, SL, TP) — survives timeframe changes

### 7. Timeframe Switch Handling (MAR-49)

When the chart type/timeframe changes, all chart objects are destroyed. `ChartLineManager` caches line prices and `RepairTradingLinesIfNeeded()` recreates them on the next `OnTick`. `SaveChartLevelsDraft()` is called in `OnStop()` to persist levels before the restart.

## Development Conventions

- **No automated test suite** — manual verification in cTrader backtester/simulator.
- **Code style** — C# conventions, XML doc comments on public members, region grouping in `COP.cs`.
- **Feature development** — targets `v2` branch. See `TODO.md` for active roadmap.
- **Documentation** — update `DOCS.md` when adding new classes/methods. Update `README.md` for user-facing changes.
- **MANDATORY: Rebuild after every change** — after any modification to `.cs` files, run `dotnet build "cop v1/cop v1.csproj"` to verify the project compiles without errors. The output is a `.algo` file loaded into cTrader. Never skip this step.

## Active Development

**Multi-TP feature** (2–3 take-profit levels) is the next planned feature. See `TODO.md` for the full implementation roadmap including volume distribution logic (equal volume vs. equal profit), panel layout changes, chart line handling per order mode, and integration steps.

## File Structure

```
cop v1/
├── cop v1.sln              # Visual Studio solution
├── README.md               # User-facing documentation (Russian)
├── DOCS.md                 # API documentation for all classes (Russian)
├── TODO.md                 # Development roadmap and task tracking
├── CLAUDE.md               # Guidance for Claude Code (English)
├── website-subscription-copy.md  # Website content drafts (Russian)
├── правило.md              # Rules/notes
├── cop v1/
│   ├── cop v1.csproj       # .NET 6.0 project, cTrader.Automate package
│   ├── COP.cs              # Main robot class (1492 lines)
│   ├── Chart/
│   │   ├── ChartLineManager.cs
│   │   └── FastOrderHandler.cs
│   ├── Trading/
│   │   ├── OrderManager.cs
│   │   ├── RiskCalculator.cs
│   │   └── LevelValidator.cs
│   └── UI/
│       ├── MainPanel.cs
│       ├── PanelStyles.cs
│       └── Localization.cs
├── design/                 # Design mockups/assets
├── doc/                    # Additional documentation
├── research/               # Research notes
└── updates/                # Feature implementation logs
```

## Key Files Reference

| File | Purpose |
|------|---------|
| `COP.cs` | Robot entry point: lifecycle (OnStart/OnTick/OnStop), RecalculateAll(), event handlers, LocalStorage sync |
| `UI/MainPanel.cs` | Full panel layout: buttons, fields, collapsible state, mini-panel (LM/MK/OK/FST), settings panel |
| `Chart/ChartLineManager.cs` | Chart lines: draw, drag, restore after timeframe change, Z-index management |
| `Chart/FastOrderHandler.cs` | Mouse click-based order entry, dynamic text updates |
| `Trading/RiskCalculator.cs` | Volume calculation, loss/profit calc, RR, multi-TP volume splits |
| `Trading/OrderManager.cs` | Order execution via cTrader API with ProtectionType.Relative |
| `UI/Localization.cs` | All user-visible strings in 9 languages — **update all languages together** |
| `UI/PanelStyles.cs` | Centralized colors, dimensions, button styles, Z-index constants |
| `Trading/LevelValidator.cs` | Static validator: determines Long/Short/Invalid from Entry/SL/TP positions |
