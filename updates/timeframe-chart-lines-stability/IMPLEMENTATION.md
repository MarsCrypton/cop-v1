# Стабильность линий SL/TP при смене таймфрейма (MAR-49)

## Связь

- **Linear:** [MAR-49](https://linear.app/marskorshun/issue/MAR-49/plagin-cop-pri-smene-tajmfrejma-propadaet-liniya-tejkstop-i-zavisaet)
- **Ветка Git:** `mar-49-timeframe-chart-lines`
- **Проект Linear:** Плагин COP (лимитки)

## Проблема

После включения режима с линиями на графике (в т.ч. через **MK** на мини-панели) при **смене таймфрейма** или **типа графика** пропадали линии **stop / take** и подписи. Ранее возможны были фризы терминала; актуальный симптом — исчезновение линий.

### Почему событий графика недостаточно (важно)

У экземпляра cBot в cTrader в числе параметров есть **таймфрейм** (см. [Manage instance parameters](https://help.ctrader.com/ctrader-algo/documentation/cbots/manage-instance-parameters/): *«The timeframe on which the instance operates»*; рекомендуется менять параметры при остановленном экземпляре и перезапускать). При смене ТФ графика платформа **перезапускает** экземпляр робота: вызываются **`OnStop` → `OnStart`**, вся оперативная память (`_isLimitMode`, кэш линий) обнуляется, объекты на графике снимаются. Любая логика «подписаться на смену ТФ и подправить линии» **не заменяет** сохранение черновика уровней между перезапусками.

**Решение:** в **`OnStop`** (пока цены ещё читаются с линий) сохранять Entry/SL/TP в **`LocalStorage` (Device)**; в **`OnStart`** после инициализации — восстанавливать UI и вызывать **`ChartLineManager.RestoreLinesFromPrices`**.

## Ожидаемое поведение

- Линии остаются на **тех же ценах (Y)** после переключения ТФ / типа графика (Renko, Range, тиковый и т.д.).
- Подписи пересоздаются при необходимости; после восстановления вызывается **`RecalculateAll()`** для актуальных текстов (лоты, %, RR).

## Реализация (итог)

### 1. Подписи по времени, не по индексу бара

- `Chart.DrawText(..., barIndex, y, ...)` заменён на **`DrawText(..., DateTime time, y, ...)`**.
- Время якоря: **`Bars.OpenTimes[Chart.LastVisibleBarIndex]`** (если индекс в диапазоне), иначе последний бар серии, иначе **`Server.Time`** — **`ChartLineManager.GetLabelAnchorTime(Robot)`** (и **`FastOrderHandler.DrawFastText`**).
- У `ChartText` выставлены **`HorizontalAlignment.Left`**, **`VerticalAlignment.Center`** (читаемость справа от якоря).

См. также: [Chart objects: bar index or time](https://help.ctrader.com/ctrader-algo/guides/ui-operations/chart-objects?q=).

### 2. Восстановление после перестройки графика

Подписки в **`ChartLineManager`** (отписка в **`Detach()`**):

| Событие | Назначение |
|---------|------------|
| `Chart.DisplaySettingsChanged` | Смена ТФ, символа и др. настройек отображения |
| `Chart.ChartTypeChanged` | Смена типа графика (свечи / Renko / Range / …) |
| `Chart.ObjectsRemoved` | Если среди удалённых есть наши ID — повторная проверка |

Логика **`TryRestoreTradingDrawings`**:

- Работает только если **`ConfigureRedrawSupport`** из **`COP`** сообщает: обычный режим с линиями (`_isLimitMode != null`) и **не** активен Fast Order.
- Если не хватает линий или текстов — по внутреннему **`_tpCount`** (как при последнем **`ShowLimitLines` / `ShowMarketLines`**), не по комбо панели: иначе при расхождении **`Ensure*`** не рисовал линию при **`price == 0`** в кэше.
- **`RestoreTradingDrawingsFromCache`** сначала **`EnsureValidCachedPricesForRestore`**: подставляет уровни из **`CalculateInitialPrices`**, если кэш обнулён/битой после смены ТФ.
- Публичный **`RepairTradingLinesIfNeeded()`** вызывается из **`COP.OnTick`** при активном Limit/Market (без Fast Order): на части сборок **`DisplaySettingsChanged` не приходит** при смене ТФ с UI, Limit-режим раньше вообще не заходил в ветку восстановления.
- **`UpdateLineTextPosition`**: если **`ChartText`** уже снят платформой, подпись **пересоздаётся** по цвету ID (Entry / SL / TP).
- После восстановления вызывается колбэк **`afterRestore`** → **`RecalculateAll()`** в **`COP`**.

### 3. Защита от каскада `ObjectsUpdated`

- Флаг **`_suppressLineEvents`** на время **`RestoreTradingDrawingsFromCache`**: в **`Chart_ObjectsUpdated`** не вызывается **`OnLinesChanged`**.

### 4. LocalStorage — черновик уровней

Ключи: `COP ChartLevels Active`, `Symbol`, `Account`, `Limit`, `TpCount`, `E`, `S`, `T1`–`T3`.

- **Сохранение:** `SaveChartLevelsDraft()` в начале **`OnStop`**, если активен обычный Limit/Market (не Fast Order).
- **Очистка:** `ClearChartLevelsDraft()` при выходе в IDLE, сабмите, сбое Fast Order и т.д.
- **Загрузка:** `TryRestoreChartLevelsAfterStart()` в конце **`OnStart`**; проверка **символ** и **номер счёта**.

### 5. Файлы

| Файл | Изменения |
|------|-----------|
| [`cop v1/Chart/ChartLineManager.cs`](../../cop%20v1/Chart/ChartLineManager.cs) | `PaintTradingLinesFromCache`, **`RestoreLinesFromPrices`**, MAR-49 по графику |
| [`cop v1/UI/MainPanel.cs`](../../cop%20v1/UI/MainPanel.cs) | **`ApplyRestoredTradingMode`** (кнопки + комбо TP без событий) |
| [`cop v1/COP.cs`](../../cop%20v1/COP.cs) | **`SaveChartLevelsDraft` / `TryRestoreChartLevelsAfterStart`**, вызовы **`ClearChartLevelsDraft`** |
| [`cop v1/Chart/FastOrderHandler.cs`](../../cop%20v1/Chart/FastOrderHandler.cs) | Подписи по времени якоря |

### 6. Ограничение

- Во время **активного** пошагового Fast Order восстановление через этот контур **не** выполняется (колбэк «линии должны быть» возвращает false). Отдельное восстановление mid-Fast Order в объём MAR-49 не входило.

---

## Чеклист самопроверки (ручной, в cTrader)

**Функционал**

- [ ] **Limit**, 2 TP: линии → M5 ↔ M1 несколько раз — те же Y, подписи на месте.
- [ ] **Market** (мини **MK** и большая кнопка): то же; Entry следует за Bid после смены ТФ.
- [ ] **Fast Order** выкл.: смена ТФ в обычном режиме — без пропажи линий.
- [ ] **1 / 2 / 3 TP** — все уровни сохраняют Y.
- [ ] Смена **типа графика** (Renko / Range / тиковый при наличии) — линии на тех же ценах.

**Регрессии**

- [ ] Перетаскивание линий вызывает пересчёт как раньше.
- [ ] Нет подвисаний при быстрой смене ТФ.
- [ ] **OnStop** — панель/робот останавливаются, линии снимаются.
