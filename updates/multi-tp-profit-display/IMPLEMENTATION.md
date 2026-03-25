# Multi-TP profit display (Equal Volume / Equal Profit)

## Goal

Align panel profit / RR labels and chart TP captions with the same volume split used when placing orders (`SplitVolumesForTps`), and refresh when TP count or volume mode changes.

## Changes

### `COP.cs` — `RecalculateAll`

- If `TpCount >= 2` and direction is valid: `GetTpPricesArray()`, `SplitVolumesForTps(_lastVolumeUnits, entry, tpPrices, isLong)`, sum `CalculateProfit` per leg → `tpDollars` / `tpPercent` for the panel; otherwise keep single-TP path.
- Chart step: for each active TP line, `UpdateLineTextPosition` with RR for that level and dollar profit for that leg (fallback when split returns one volume: full volume on the main/far TP line only).

### `MainPanel.cs`

- `UpdateTakeProfit(..., int tpCount, TpVolumeMode tpVolumeMode)`: one TP — unchanged `$ (%)` format; multiple — summary via `Localization.Get("TpInfoMulti", ...)` plus short mode label (`TpAllocShortEqualVolume` / `TpAllocShortEqualProfit`).
- `OnTpAllocationSettingsChanged` fired from `_tpCountCombo` and `_tpVolumeModeCombo` `SelectedItemChanged` (single-arg delegate per cAlgo API).

### `COP.cs` — settings reaction

- `HandleTpAllocationSettingsChanged`: if draft lines exist and `DisplayedTpCount != TpCount`, `SyncChartTpLineCountToPanel` rebuilds lines via `RestoreLinesFromPrices` with interpolated prices; then `RecalculateAll`. Skipped when `_isLimitMode == null` or fast order active.

### `ChartLineManager.cs`

- `DisplayedTpCount` exposes `_tpCount` for sync logic.

### `Localization.cs`

- Keys: `TpInfoMulti`, `TpAllocShortEqualVolume`, `TpAllocShortEqualProfit` (all configured languages).

### `DOCS.md`

- `UpdateTakeProfit` signature and behavior updated.

## Acceptance

- Panel Σ profit matches sum of per-leg `CalculateProfit` under the active split mode.
- Each green TP line shows its own RR and `$` for that leg.
- SL still shows full-position loss at stop.
- Changing TP count or volume mode updates numbers immediately when a draft is on chart.

## Notes

- Minor volume drift after `NormalizeVolume` across legs is acceptable per spec.

---

## Проверка и закрытие релиза (аудит кода)

**Дата:** 2026-03-25  

**Сборка:** `dotnet build` для `cop v1` — без ошибок и предупреждений.

**Логика**

- Суммарная прибыль на панели при `TpCount ≥ 2` считается как сумма `CalculateProfit` по ногам с теми же `tpPrices` и `SplitVolumesForTps`, что и при выставлении ордеров — дублирования «другой формулы» нет.
- Убыток по SL по-прежнему на полный объём (`CalculateLoss` с `_lastVolumeUnits`).
- Ветка `Invalid` направления корректно откатывается к одному TP на графике (`tpPricesMulti` / `volsMulti` не используются в шаге 9).
- `SyncChartTpLineCountToPanel` согласован с семантикой цен в `ChartLineManager` (TP1 ближе к входу, TP2/TP3 дальше при 2–3 тейках).

**Дублирование в коде (намеренно / приемлемо)**

- Два цикла по ногам в `RecalculateAll`: первый — только сумма `$` для панели и `%`, второй — RR и `$` на каждую линию. Можно было бы кэшировать `pLeg` в массиве; оставлено раздельно для читаемости, без логического расхождения.

**Техдолг (из плана, не блокер)**

- Параметр `isLong` в `SplitVolumesForTps` не используется (расчёт идёт по `|tp - entry|`).

**Git:** изменения закоммичены и отправлены в `main` (multi-TP profit display + документация).

**Linear:** [MAR-77](https://linear.app/marskorshun/issue/MAR-77) — финальный комментарий о проверке и merge в основную ветку.
