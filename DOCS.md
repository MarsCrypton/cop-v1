# Документация по функциям COP v1

Полное описание всех классов и методов робота COP v1.

---

## Соглашения

- **Параметры робота (настройки)** — названия параметров и значения выбора в окне настроек задаются **только на английском языке** (например: Fast Order Mode = No / Yes). Это обеспечивает единообразие независимо от языка интерфейса панели.
- **Локализация UI-текстов** — при любом добавлении или изменении пользовательских текстов (надписи в панели, подсказки, сообщения об ошибках, подписи линий и т. п.) необходимо одновременно обновлять переводы для всех языков, доступных в настройках плагина (`InterfaceLanguage`). Источник строк и переводов: `cop v1/UI/Localization.cs`. Отсутствие перевода приводит к отображению ключа и считается техническим долгом.
- **Правило для Cursor** — то же требование по локализации задано в `.cursor/rules/localization.mdc`: при правках UI-текста или файлов панели/линий Cursor должен обновлять переводы в `Localization.cs` для всех языков.

---

## 1. COP.cs — главный класс робота

Робот наследует `cAlgo.API.Robot`. Управляет жизненным циклом, панелью, линиями и размещением ордеров.

### Параметры (Properties)

| Параметр | Тип | Описание |
|----------|-----|----------|
| `VPosition` | `VerticalPosition` | Вертикальная позиция панели (Top, Center, Bottom). |
| `HPosition` | `HorizontalPosition` | Горизонтальная позиция панели (Left, Center, Right). |
| `PanelTransparencyPercent` | `int` | Прозрачность фона панелей 0–80 % (по умолчанию 10). Можно также менять в панели настроек («...» → Panel transparency). |
| `RiskMode` | `RiskMode` | Режим риска: Percent / USD / EUR. По умолчанию Percent. |
| `MaxRiskPercent` | `double` | Максимальный риск в % от баланса (0.1–100). |
| `MaxRiskUsd` | `double` | Максимальный риск суммой в USD (> 0). |
| `MaxRiskEur` | `double` | Максимальный риск суммой в EUR (> 0). |
| `FastOrderMode` | `YesNo` | Режим быстрого ордера по умолчанию (No / Yes). В настройках отображается только на английском. |
| `InterfaceLanguage` | `Language` | Язык интерфейса (EN, RU). |

### Методы жизненного цикла

| Метод | Описание |
|-------|----------|
| `OnStart()` | Инициализация: локализация, создание панели и менеджеров (ChartLineManager, RiskCalculator, OrderManager, FastOrderHandler), **`ChartLineManager.ConfigureRedrawSupport`**, подписка на события панели и линий. **MAR-49:** в конце — **`TryRestoreChartLevelsAfterStart()`** (черновик из `LocalStorage` после перезапуска экземпляра, в т.ч. при смене ТФ). |
| `OnTick()` | Обновление спреда; **MAR-49:** `RepairTradingLinesIfNeeded()` при активном Limit/Market (без Fast Order); в Market-режиме — обновление цены, линии Entry и `RecalculateAll()`. |
| `OnStop()` | **MAR-49:** **`SaveChartLevelsDraft()`** (черновик уровней в `LocalStorage` для обычного Limit/Market). Затем отписка от событий, удаление линий с графика, отписка FastOrderHandler. |

### Внутренние методы (Core logic)

| Метод | Описание |
|-------|----------|
| `RecalculateAll()` | Пересчитывает все данные: получает цены Entry/SL/TP, валидирует уровни (LevelValidator), получает риск из поля (Percent/USD/EUR), при USD/EUR конвертирует сумму через встроенный `AssetConverter`, считает объём (RiskCalculator), убыток/прибыль, RR; обновляет панель и подписи на линиях. При ошибке конвертации показывает ошибку на панели и пропускает расчёт. |
| `ParseRisk(string text)` | Парсит значение риска из текстового поля. Поддерживает точку и запятую. Возвращает значение в диапазоне 0.1–100 или MaxRiskPercent при ошибке. |

### Обработчики событий панели

| Метод | Описание |
|-------|----------|
| `HandleLimitClicked(bool isActive)` | При активации: включает Limit-режим, показывает линии (обычный режим) или запускает FastOrderHandler (Fast Order). При деактивации: снимает линии, сбрасывает панель в IDLE. |
| `HandleMarketClicked(bool isActive)` | Аналогично для Market: линии Market (Entry = текущая цена) или Fast Order (клики SL → TP). |
| `HandleSubmitClicked()` | Проверяет направление и режим; размещает Limit или Market ордер через OrderManager; при любом исходе удаляет линии, сбрасывает панель, сворачивает её. |
| `HandleRiskChanged(string newText)` | При изменении риска пересчитывает всё (RecalculateAll), если режим активен и линии есть. |
| `HandleFastOrderToggled(bool isEnabled)` | При переключении Fast Order отменяет текущий процесс; при активном обычном режиме снимает линии и сбрасывает в IDLE. |

### Обработчики ручного ввода цен

| Метод | Описание |
|-------|----------|
| `HandlePriceFieldChanged(string text)` | Пользователь ввёл цену Entry (только Limit, не Fast Order). Перемещает синюю линию, обновляет подпись, вызывает RecalculateAll(). |
| `HandleSlFieldChanged(string text)` | Ввод цены SL — перемещает красную линию и пересчитывает. |
| `HandleTpFieldChanged(string text)` | Ввод цены TP — перемещает зелёную линию и пересчитывает. |
| `TryParseFieldPrice(string text, out double price)` | Парсит цену из поля (точка/запятая, положительное число). Возвращает true при успехе. |

### Обработчики линий графика

| Метод | Описание |
|-------|----------|
| `HandleLinesChanged()` | Вызывается при перетаскивании линий. Вызывает RecalculateAll() только в обычном режиме (не во время Fast Order). |

### Fast Order callback

| Метод | Описание |
|-------|----------|
| `HandleFastOrderReady(double entryPrice, double slPrice, double tpPrice, bool isMarket)` | Вызывается FastOrderHandler после фиксации всех уровней. Валидирует уровни, считает объём, размещает ордер (Limit или Market), удаляет линии, сбрасывает панель и сворачивает её. |

### Перечисления (Enums)

| Тип | Значения | Описание |
|-----|----------|----------|
| `VerticalPosition` | Top, Center, Bottom | Вертикальная позиция панели. |
| `HorizontalPosition` | Left, Center, Right | Горизонтальная позиция панели. |
| `YesNo` | No, Yes | Варианты выбора для параметров (в настройках — только на английском). |
| `Language` | EN, RU, DE, … | Язык интерфейса панели. |

---

## 2. ChartLineManager.cs — управление линиями на графике

Рисует и обновляет горизонтальные линии Entry, SL, TP (1–3 уровня) и текстовые подписи. Отслеживает перетаскивание. После смены таймфрейма или типа графика **восстанавливает** отсутствующие объекты из кэша цен (MAR-49).

### Константы (ID объектов)

- `EntryLineId`, `EntryTextId` — линия и текст Entry.
- `SlLineId`, `SlTextId` — Stop Loss.
- `Tp1LineId` … `Tp3LineId` и соответствующие `Tp*TextId` — Take Profit (до трёх уровней).

### События

| Событие | Описание |
|---------|----------|
| `OnLinesChanged` | Вызывается при перетаскивании любой из линий (по событию Chart.ObjectsUpdated), если не включён внутренний флаг подавления. |

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `EntryPrice` | `double` | Текущая цена линии Entry. |
| `StopLossPrice` | `double` | Текущая цена SL. |
| `TakeProfitPrice` | `double` | Текущая цена «основного» TP (зависит от числа тейков). |
| `MainTpLineId` / `MainTpTextId` | `string` | ID основной линии/текста TP для расчёта и ввода с панели. |
| `HasEntryLine` | `bool` | Есть ли линия Entry на графике. |
| `HasAnyLines` | `bool` | Есть ли хотя бы одна линия (Entry или SL). |

### Методы

| Метод | Описание |
|-------|----------|
| `GetLabelAnchorTime(Robot bot)` | Статический: время последнего бара `Bars.OpenTimes` или `Server.Time` — якорь X для подписей (устойчиво к смене ТФ). |
| `ConfigureRedrawSupport(...)` | Задаёт из COP: когда торговые линии должны быть на графике, Limit vs Market, колбэк после восстановления (обычно `RecalculateAll`). Число тейков при восстановлении берётся из внутреннего `_tpCount` менеджера (как при последнем `Show*Lines`). |
| `RepairTradingLinesIfNeeded()` | Публичный вызов той же логики, что и обработчики смены ТФ: досоздать отсутствующие линии/тексты из кэша; вызывается из `OnTick`. |
| `ChartLineManager(Robot bot)` | Конструктор. Подписка на `ObjectsUpdated`, `DisplaySettingsChanged`, `ChartTypeChanged`, `ObjectsRemoved`. |
| `ShowLimitLines(int tpCount)` | Удаляет старые линии, вычисляет начальные цены по видимой области, рисует Entry/SL/TP с подписями. |
| `ShowMarketLines(int tpCount)` | Entry = Bid, линия Entry неинтерактивная; SL/TP интерактивные. |
| `UpdateMarketEntryLine(double price, string text)` | Обновляет линию и текст Market Entry (из OnTick). |
| `RemoveAllLines()` | Удаляет все объекты COP с графика. |
| `UpdateLineText` / `UpdateLineTextPosition` | Обновление подписей; позиция — перерисовка через `DrawLineText`. |
| `GetPrice`, `MoveLineTo` | Чтение Y с графика / программный сдвиг линии и кэша. |
| `Detach()` | Отписка от всех подписок графика. Вызывать в OnStop. |

### Внутренние методы (существенные)

| Метод | Описание |
|-------|----------|
| `CalculateInitialPrices` | Начальные цены по Chart.TopY/BottomY; fallback Bid ± 0.5%. |
| `DrawLine` / `DrawLineText` | Линия; текст через `Chart.DrawText(..., DateTime, y, ...)` и выравнивание. |
| `TryRestoreTradingDrawings` / `RestoreTradingDrawingsFromCache` | Если не хватает линий или текстов — создать из кэша без сброса уровней по экрану. |
| `Chart_ObjectsUpdated` | Перетаскивание линий → кэш → `OnLinesChanged` (если не `_suppressLineEvents`). |

---

## 3. FastOrderHandler.cs — режим быстрого ордера

Привязывает линии к курсору мыши; фиксация по клику. Limit: 3 клика (Entry → SL → TP). Market: 2 клика (SL → TP). По последнему клику вызывается callback для размещения ордера.

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsActive` | `bool` | Идёт ли процесс Fast Order (_step > 0). |

### Методы

| Метод | Описание |
|-------|----------|
| `FastOrderHandler(Robot, ChartLineManager, RiskCalculator, Action<...> onOrderReady, Func<double> getRiskPercent)` | Конструктор. Сохраняет callback для размещения ордера и функцию получения % риска. |
| `StartLimit()` | Сбрасывает предыдущий процесс. Шаг 1: рисует синюю линию Entry у курсора, подписывается на MouseMove/MouseDown. |
| `StartMarket()` | Entry = текущая цена (неинтерактивная синяя линия). Шаг 2: красная линия SL привязана к курсору. |
| `Cancel()` | Отписывается от мыши, удаляет все линии, _step = 0. |
| `Detach()` | То же, что Cancel; вызывать при остановке бота. |

### Внутренние обработчики и методы

| Метод | Описание |
|-------|----------|
| `OnMouseMove(ChartMouseEventArgs args)` | Перемещает текущую линию к args.YValue; вызывает UpdateDynamicText. |
| `OnMouseDown(ChartMouseEventArgs args)` | Фиксирует текущую линию, переходит к следующему шагу (Entry→SL→TP или SL→TP→готово); на последнем шаге вызывает _onOrderReady. |
| `UpdateDynamicText(double cursorPrice)` | Обновляет подпись на текущей линии: шаг 1 — объём (по примерному SL); шаг 2 — % убытка и текст на Entry; шаг 3 — RR. В Market обновляет позицию Entry-линии. |
| `DrawFastLine(string id, double price, Color color)` | Рисует неинтерактивную линию (IsInteractive = false). |
| `DrawFastText(...)` | Рисует текст у линии (время якоря — `ChartLineManager.GetLabelAnchorTime`). |

---

## 4. OrderManager.cs — размещение ордеров

Размещает лимитные и рыночные ордера через API cTrader. Направление определяется по уровням; SL/TP передаются в пипсах (ProtectionType.Relative).

### Константы

| Константа | Значение | Описание |
|-----------|----------|----------|
| `OrderLabel` | "COP" | Метка для всех ордеров бота. |

### Методы

| Метод | Описание |
|-------|----------|
| `OrderManager(Robot bot)` | Конструктор. |
| `PlaceLimitOrder(double entryPrice, double slPrice, double tpPrice, double volumeInUnits)` | Определяет направление: TP > Entry → Buy, иначе Sell. Считает SL/TP в пипсах (мин. 1 пип), вызывает PlaceLimitOrder с ProtectionType.Relative. Возвращает TradeResult. |
| `PlaceMarketOrder(double slPrice, double tpPrice, double volumeInUnits)` | Направление по TP относительно Ask/Bid: TP > Ask → Buy, иначе Sell. SL/TP в пипсах, ExecuteMarketOrder. Возвращает TradeResult. |

---

## 5. RiskCalculator.cs — расчёт объёма и риска

Считает объём позиции по % риска, убыток/прибыль в $ и %, Risk-Reward. Нормализует объём по шагу и мин/макс символа.

### Методы

| Метод | Описание |
|-------|----------|
| `RiskCalculator(Robot bot)` | Конструктор. |
| `CalculateVolume(double entryPrice, double slPrice, double riskPercent)` | VolumeInUnits = RiskAmount / (SL_pips * PipValue). Защита от NaN/0/отрицательных; нормализация через NormalizeVolume. Возвращает units. |
| `CalculateLoss(double entryPrice, double slPrice, double volumeInUnits, out dollars, out percent)` | Считает убыток в $ и в % от баланса при срабатывании SL. |
| `CalculateProfit(double entryPrice, double tpPrice, double volumeInUnits, out dollars, out percent)` | Аналогично для Take Profit. |
| `CalculateRR(double entryPrice, double slPrice, double tpPrice)` | RR = расстояние до TP / расстояние до SL. |
| `ToLots(double volumeInUnits)` | Конвертирует units в лоты для отображения (VolumeInUnitsToQuantity). |
| `NormalizeVolume(double volumeInUnits)` | Округляет к VolumeInUnitsStep, ограничивает мин/макс. (приватный) |

---

## 6. LevelValidator.cs — валидация уровней

Статический класс. Определяет направление ордера по взаимному расположению Entry, SL, TP.

### Перечисление OrderDirection

| Значение | Описание |
|----------|----------|
| Long | TP > Entry > SL. |
| Short | SL > Entry > TP. |
| Invalid | Иначе (некорректная расстановка). |

### Методы

| Метод | Описание |
|-------|----------|
| `Validate(double entry, double sl, double tp)` | Long: tp > entry && sl < entry. Short: tp < entry && sl > entry. Иначе Invalid. |

---

## 7. MainPanel.cs — главная UI-панель

Строит панель: заголовок с кнопкой свёртывания, чекбокс Fast order и отображение спреда, кнопки Limit/Market, поля риска и цены, SL/TP с подсказками $ и %, кнопка подтверждения. Поддерживает сворачивание; состояние свёрнутости сохраняется в статической переменной между перезапусками. При **свёрнутой** панели под шапкой отображается **мини-панель** (вторая строка) с кнопками **LM** (Limit), **MK** (Market), **OK** (выставить ордер), **FST** (быстрый ордер): LM и MK переключают режим без разворота, OK выставляет ордер по последним заданным уровням без разворота, FST переключает быстрый ордер. При развороте мини-панель скрывается. Состояние (Limit/Market, доступность кнопки подтверждения, включён ли Fast Order) сохраняется и отображается и в мини-строке, и в основной панели.

### События

| Событие | Описание |
|---------|----------|
| `OnLimitClicked` | bool — кнопка Limit стала активной/неактивной. |
| `OnMarketClicked` | bool — кнопка Market. |
| `OnSubmitClicked` | Нажата кнопка подтверждения ордера. |
| `OnRiskChanged` | string — новый текст поля риска. |
| `OnPriceChanged` | string — новое значение поля цены Entry. |
| `OnSlChanged` | string — новое значение SL. |
| `OnTpChanged` | string — новое значение TP. |
| `OnFastOrderToggled` | bool — состояние чекбокса Fast order. |

### Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `RootControl` | `Border` | Корневой контрол для Chart.AddControl() (обёртка: основная панель + панель настроек под ней). |
| `IsFastOrder` | `bool` | Чекбокс Fast order включён. |
| `RiskText` | `string` | Текст поля риска. |
| `IsLimitActive` | `bool` | Кнопка Limit активна. |
| `IsMarketActive` | `bool` | Кнопка Market активна. |

### Методы

| Метод | Описание |
|-------|----------|
| `MainPanel(Robot bot, VerticalPosition, HorizontalPosition, double maxRiskPercent, bool fastOrderMode)` | Строит всю панель; восстанавливает свёрнутость из s_savedCollapsedState. |
| `UpdateSpread(double spreadPips)` | Обновляет текст спреда (спред всегда отображается). |
| `UpdateEntryPrice(double price, int digits)` | Устанавливает цену входа в поле (без вызова OnPriceChanged). |
| `UpdateMarketPrice(double bid, double ask, int digits)` | Устанавливает рыночную цену в поле. |
| `UpdateStopLoss(double price, int digits, double lossDollars, double lossPercent)` | Обновляет поле SL и подпись $ (%). |
| `UpdateTakeProfit(double price, int digits, double profitDollars, double profitPercent)` | Обновляет поле TP и подпись $ (%). |
| `UpdateSubmitButton(int direction, bool isLimit, string symbolName, string volumeLots)` | direction: 1 Long, -1 Short, 0 Invalid. Меняет текст и стиль кнопки (серая/зелёная/красная). |
| `SetMode(bool isLimit)` | Меняет заголовок блока цены (Limit Order / Market Order) и readonly поля цены. |
| `SetFieldsReadOnly(bool readOnly)` | Делает поля SL/TP (и при readOnly — цену) только для чтения. |
| `ResetToIdle()` | Сбрасывает кнопки режимов, кнопку подтверждения, очищает поля и подсказки, снимает подсветку ошибок. |
| `Collapse()` | Сворачивает панель (контент скрыт, кнопка ▼), сохраняет состояние в s_savedCollapsedState. |
| `Expand()` | Разворачивает панель, s_savedCollapsedState = false. |

---

## 8. PanelStyles.cs — стили и константы UI

Статический класс. Цвета (фон, текст, кнопки, линии), размеры (ширина панели, шрифты, отступы), методы применения стилей к кнопкам и полям.

### Цвета (поля)

- `PanelBackground`, `InputBackground`, `TextColor`, `TextMuted`
- `ButtonActive`, `ButtonInactive`, `ButtonError`, `ButtonHover`, `SeparatorColor`
- `LineEntry`, `LineStopLoss`, `LineTakeProfit`

### Размеры

- `PanelWidth`, `FontSizeNormal`, `FontSizeSmall`, `Padding`, `CornerRadius`, `LineThickness`

### Методы

| Метод | Описание |
|-------|----------|
| `ApplyModeButtonStyle(Button btn, bool isActive)` | Стиль кнопки Limit/Market: активная — зелёная, неактивная — серая; hover через Style. |
| `ApplySubmitButtonStyle(Button btn, int state)` | state: 0 серая, 1 зелёная, -1 красная; включает hover. |
| `ApplyTextBoxStyle(TextBox tb)` | Фон, цвет текста, шрифт, отступ. |
| `CreateToggleButtonStyle()` | Стиль кнопки свёртывания с hover. |
| `ApplyLabelStyle(TextBlock tb)` | Приглушённый текст, мелкий шрифт. |
| `ApplyValueStyle(TextBlock tb)` | Основной цвет текста для значений. |

---

## 9. Localization.cs — локализация

Статический класс. Словарь строк для EN и RU. Установка языка и получение строк по ключу.

### Методы

| Метод | Описание |
|-------|----------|
| `SetLanguage(Language lang)` | Устанавливает текущий язык (_lang). |
| `Get(string key)` | Возвращает локализованную строку по ключу; при отсутствии — ключ. |
| `Get(string key, params object[] args)` | То же с подстановкой аргументов (string.Format). |

Ключи включают: PanelTitle, FastOrder, Spread, Limit, Market, MaxRisk, LimitOrder, MarketOrder, StopLoss, TakeProfit, PlaceOrder, InvalidLevels, LimitLong, LimitShort, BuyMarket, SellMarket, StopText, TpText, LimitText, MarketText.
