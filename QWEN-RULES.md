# Qwen Code Rules — COP v1 Project

These rules are derived from the `.cursor/rules/` directory and are embedded here for Qwen Code to follow when working on this project.

---

## Rule 1: cTrader UI Layout Rules

**Source:** `.cursor/rules/ctrader-ui-layout.mdc`  
**Applies to:** `cop v1/UI/**/*.cs`  
**Always apply:** true

These rules exist because cTrader's layout engine differs critically from WPF in how it handles Grid Star columns and width propagation through StackPanel chains. Violations produce code that compiles cleanly but renders with asymmetric margins.

### Rule 1.1 — Grid with all-Star columns MUST have explicit Width

A `Grid` where every column uses `SetWidthInStars()` does NOT auto-expand through a `StackPanel` parent. It collapses to minimum content width and left-aligns.

```csharp
// ❌ WRONG — compiles fine, renders narrow + left-aligned in cTrader:
var grid = new Grid(1, 2) { HorizontalAlignment = HorizontalAlignment.Stretch };
grid.Columns[0].SetWidthInStars(1);
grid.Columns[1].SetWidthInStars(1);

// ✅ CORRECT — explicit Width forces correct Star column expansion:
var grid = new Grid(1, 2) { Width = PanelStyles.ContentWidth };
grid.Columns[0].SetWidthInStars(1);
grid.Columns[1].SetWidthInStars(1);
```

**Exception:** A Grid with at least one Auto column (e.g. Auto | Star) works without explicit Width because the Auto column provides a definite size anchor.

### Rule 1.2 — HorizontalAlignment.Stretch is unreliable for Grid inside StackPanel

`HorizontalAlignment = HorizontalAlignment.Stretch` on a Grid child of a StackPanel does NOT reliably provide the full parent width in cTrader. Do not rely on it for any layout-critical Grid.

Always use `Width = PanelStyles.ContentWidth` for:
- Mode button rows (Limit / Market)
- SL / TP rows
- All settings panel rows (Take profits, Volume mode, Transparency, Scale)
- Any other row-level Grid that needs full panel width

### Rule 1.3 — Do NOT add Padding to _rootWrapper

`PanelClientWidth = PanelWidth - 2 * BorderThickness`

Adding a `Padding` to `_rootWrapper` (the root Border) shifts ALL panel content to one side:
- Actual content area = `PanelWidth - 2*border - 2*padding`
- But inner containers use `Width = PanelClientWidth` which does NOT subtract padding
- Result: containers are wider than their available area → overflow on one side → asymmetric margins

**Content inset is only through `_contentStack.Margin = ContentStackHorizontalMargin`.** Never use `Padding` on the root Border for horizontal spacing.

### Rule 1.4 — Every StackPanel in the width-propagation chain must have explicit Width

```
_rootWrapper      Border     Width = PanelWidth          (no Padding!)
_rootContainer    StackPanel Width = PanelClientWidth
_mainPanelRoot    StackPanel Width = PanelClientWidth
_mainStack        StackPanel Width = PanelClientWidth     ← must be explicit
_contentStack     StackPanel Margin = ContentStackHMarg   (no explicit Width needed)
  row Grid        Grid       Width = ContentWidth         ← Star-only Grids
```

A StackPanel **without** explicit Width in this chain may fail to pass a constrained width to its children, silently breaking Star columns and HorizontalAlignment.Stretch.

### Rule 1.5 — ContentWidth is the single source of truth for row widths

Always use `PanelStyles.ContentWidth` when setting the Width of a row-level Grid. Never hardcode pixel values or compute width manually with magic constants.

```csharp
// ❌ WRONG:
var grid = new Grid(1, 2) { Width = PanelStyles.PanelWidth - PanelStyles.S(16) };

// ✅ CORRECT:
var grid = new Grid(1, 2) { Width = PanelStyles.ContentWidth };
```

`ContentWidth = PanelClientWidth - 2 * ContentInsetX` (defined in PanelStyles.cs).

### Rule 1.6 — Settings panel rows follow the same rules as main panel rows

All Grid rows inside `settingsRowsStack` must also have `Width = PanelStyles.ContentWidth`. The Border `_settingsPanelBorder` does NOT automatically constrain its child StackPanel's available width — the same Star-column expansion problem applies there.

### Quick Diagnostic Checklist

When you see asymmetric left/right margins in cTrader panel, check in this order:

1. Does the broken row use a Grid with all-Star columns and no explicit `Width`? → Add `Width = PanelStyles.ContentWidth`
2. Does `_mainStack` or any ancestor StackPanel lack explicit `Width`? → Add `Width = PanelStyles.PanelClientWidth`
3. Does `_rootWrapper` Border have `Padding` set? → Remove it
4. Is a magic constant used instead of `ContentWidth`? → Replace with `PanelStyles.ContentWidth`

---

## Rule 2: Localization — Update ALL Languages

**Source:** `.cursor/rules/localization.mdc`  
**Applies to:** `**/UI/**`, `**/Localization.cs`, `**/COP.cs`, `**/MainPanel.cs`, `**/ChartLineManager.cs`, `**/FastOrderHandler.cs`  
**Always apply:** false (triggered when modifying user-visible text)

При любом **добавлении** или **изменении** пользовательских текстов в плагине (надписи в панели, подсказки, сообщения об ошибках, подписи линий и т. п.) необходимо одновременно обновлять переводы для **всех** языков, доступных в настройках плагина (`InterfaceLanguage`).

- **Источник строк и переводов:** `cop v1/UI/Localization.cs`
- Добавлять или менять строки нужно во всех словарях (EN, RU, DE, FR, ES, IT, PL, NL, PT)
- Отсутствие перевода приводит к отображению ключа и считается техническим долгом

### Checklist при добавлении нового текста:

1. Добавить ключ в `EN` (English) — основной язык
2. Добавить перевод в `RU` (Russian)
3. Добавить перевод в `DE` (German)
4. Добавить перевод в `FR` (French)
5. Добавить перевод в `ES` (Spanish)
6. Добавить перевод в `IT` (Italian)
7. Добавить перевод в `PL` (Polish)
8. Добавить перевод в `NL` (Dutch)
9. Добавить перевод в `PT` (Portuguese)

---

## Additional Project Conventions (from QWEN.md)

### Mandatory Rebuild

After **every** change to `.cs` files, run:
```bash
dotnet build "cop v1/cop v1.csproj"
```
The output is a `.algo` file loaded into cTrader. Never skip this step.

### Volume Calculation Formula

```
VolumeInUnits = RiskAmount / (SL_distance_in_pips × PipValue)
→ Normalize to VolumeInUnitsStep, clamp to [VolumeInUnitsMin, VolumeInUnitsMax]
```

### Robot Parameters are English-only

Parameter names and enum values in the cTrader settings window are **always in English** (e.g., `Fast Order Mode = No / Yes`). UI panel labels are localized.

### cAlgo API Limitations

- Chart object labels support a limited character set; avoid Unicode that cTrader doesn't render
- `Symbol.VolumeInUnitsStep` normalization is **required** before placing any order
- `AssetConverter.Convert()` is needed for USD/EUR risk mode to convert to account currency
- Z-index on chart objects must be set high (see `PanelStyles.ChartTradingLineZIndex`) to keep lines above price bars
