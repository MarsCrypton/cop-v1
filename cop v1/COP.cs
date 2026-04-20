using System;
using System.Globalization;
using cAlgo.API;
using COP_v1.UI;
using COP_v1.Chart;
using COP_v1.Trading;

namespace COP_v1
{
    /// <summary>
    /// COP v1 — cTrader Order Panel.
    /// Торговый бот с графической панелью для быстрого размещения ордеров
    /// с автоматическим расчётом объёма на основе допустимого риска.
    /// </summary>
    [Robot(AccessRights = AccessRights.None)]
    public class COP : Robot
    {
        #region Parameters — Panel alignment

        [Parameter("Vertical Position", Group = "Panel alignment", DefaultValue = VerticalPosition.Top)]
        public VerticalPosition VPosition { get; set; }

        [Parameter("Horizontal Position", Group = "Panel alignment", DefaultValue = HorizontalPosition.Right)]
        public HorizontalPosition HPosition { get; set; }

        [Parameter("Panel transparency %", Group = "Panel alignment", DefaultValue = 0, MinValue = 0, MaxValue = 80)]
        public int PanelTransparencyPercent { get; set; }

        /// <summary>Выпадающий список в настройках экземпляра: 80–150 % с шагом 10.</summary>
        [Parameter("Panel scale %", Group = "Panel alignment", DefaultValue = PanelScale.Scale100)]
        public PanelScale PanelScale { get; set; }

        #endregion

        #region Parameters — Default trade parameters

        [Parameter("Risk Mode", Group = "Default trade parameters", DefaultValue = RiskMode.Percent)]
        public RiskMode RiskMode { get; set; }

        [Parameter("Max Risk %", Group = "Default trade parameters", DefaultValue = 2.0, MinValue = 0.1, MaxValue = 100.0)]
        public double MaxRiskPercent { get; set; }

        [Parameter("Max Risk USD", Group = "Default trade parameters", DefaultValue = 50.0, MinValue = 0.01)]
        public double MaxRiskUsd { get; set; }

        [Parameter("Max Risk EUR", Group = "Default trade parameters", DefaultValue = 50.0, MinValue = 0.01)]
        public double MaxRiskEur { get; set; }

        [Parameter("Fast Order Mode", Group = "Default trade parameters", DefaultValue = YesNo.No)]
        public YesNo FastOrderMode { get; set; }

        [Parameter("Interface Language", Group = "Default trade parameters", DefaultValue = Language.EN)]
        public Language InterfaceLanguage { get; set; }

        #endregion

        #region Private fields

        private MainPanel _mainPanel;
        private ChartLineManager _chartLineManager;
        private RiskCalculator _riskCalculator;
        private OrderManager _orderManager;
        private FastOrderHandler _fastOrderHandler;

        // Текущий режим: true = Limit, false = Market, null = IDLE
        private bool? _isLimitMode = null;

        // Последний рассчитанный объём (units) — нужен для кнопки подтверждения
        private double _lastVolumeUnits;
        private double _lastVolumeLots;
        private OrderDirection _lastDirection = OrderDirection.Invalid;

        private RiskMode _currentRiskMode = RiskMode.Percent;

        #endregion

        #region Lifecycle

        protected override void OnStart()
        {
            // Инициализировать локализацию
            Localization.SetLanguage(InterfaceLanguage);

            // Режим риска: приоритет у сохранённого значения (совместимость)
            _currentRiskMode = LoadSavedRiskMode();
            if (_currentRiskMode == RiskMode.Unknown)
                _currentRiskMode = RiskMode;

            // Масштаб UI панели: та же схема приоритета, что и для прозрачности (параметр ↔ LocalStorage).
            int paramScale = Math.Max(80, Math.Min(150, (int)PanelScale));
            int savedScale = LoadSavedScalePercent();
            int panelScale;
            if (savedScale < 0)
            {
                panelScale = paramScale;
            }
            else if (savedScale != paramScale)
            {
                panelScale = paramScale;
                SaveScalePercent(panelScale);
            }
            else
            {
                panelScale = savedScale;
            }

            PanelStyles.SetScalePercent(panelScale);

            // Создать и отобразить панель
            // Прозрачность панели: синхронизация "шестерёнка" ↔ in-panel настройки.
            // Правило:
            // - если LocalStorage пустой → берём параметр бота
            // - если LocalStorage есть, но пользователь поменял параметр в настройках бота → считаем параметр новым источником правды и перезаписываем LocalStorage
            // - иначе берём сохранённое значение
            int savedTransparency = LoadSavedTransparency();
            int panelTransparency;
            if (savedTransparency < 0)
            {
                panelTransparency = PanelTransparencyPercent;
            }
            else if (savedTransparency != PanelTransparencyPercent)
            {
                panelTransparency = PanelTransparencyPercent;
                SaveTransparency(panelTransparency);
            }
            else
            {
                panelTransparency = savedTransparency;
            }

            _mainPanel = new MainPanel(this, VPosition, HPosition, MaxRiskPercent, FastOrderMode == YesNo.Yes, panelTransparency);
            Chart.AddControl(_mainPanel.RootControl);

            ApplyInitialRiskToPanel();

            // Инициализировать менеджеры
            _chartLineManager = new ChartLineManager(this);
            _riskCalculator = new RiskCalculator(this);
            _orderManager = new OrderManager(this);
            _fastOrderHandler = new FastOrderHandler(
                this,
                _chartLineManager,
                _riskCalculator,
                HandleFastOrderReady,
                () => GetCurrentRiskInput());

            _chartLineManager.ConfigureRedrawSupport(
                () => _isLimitMode != null && !_fastOrderHandler.IsActive,
                () => _isLimitMode == true,
                () =>
                {
                    if (_isLimitMode != null && !_fastOrderHandler.IsActive)
                        RecalculateAll();
                });

            SubscribePanelEvents();

            // Подписаться на изменение линий
            _chartLineManager.OnLinesChanged += HandleLinesChanged;

            TryRestoreChartLevelsAfterStart();

            Print("COP v1 started. Language: {0}", InterfaceLanguage);
        }

        protected override void OnTick()
        {
            // Обновить спред на панели (всегда отображается)
            if (_mainPanel != null)
            {
                double spreadPips = Symbol.Spread / Symbol.PipSize;
                _mainPanel.UpdateSpread(spreadPips);
            }

            // MAR-49: перезапуск (смена ТФ) — уровни из LocalStorage в OnStart; здесь тот же экземпляр: объекты могли исчезнуть без OnStop, событие графика не всегда приходит.
            if (_chartLineManager != null && _isLimitMode != null && !_fastOrderHandler.IsActive)
                _chartLineManager.RepairTradingLinesIfNeeded();

            // Если Market-режим активен — обновить текущую цену, линию и пересчитать
            if (_isLimitMode == false && _chartLineManager.HasAnyLines)
            {
                _mainPanel.UpdateMarketPrice(Symbol.Bid, Symbol.Ask, Symbol.Digits);

                // Перемещаем линию Market Entry за текущей ценой
                string entryText = Localization.Get("MarketText", _lastVolumeLots.ToString("F2"));
                _chartLineManager.UpdateMarketEntryLine(Symbol.Bid, entryText);

                RecalculateAll();
            }
        }

        protected override void OnStop()
        {
            // Смена ТФ перезапускает экземпляр cBot — сохраняем уровни до RemoveAllLines (иначе память и график обнуляются).
            SaveChartLevelsDraft();

            UnsubscribePanelEvents();

            // Отписаться от ChartLineManager и удалить линии
            if (_chartLineManager != null)
            {
                _chartLineManager.OnLinesChanged -= HandleLinesChanged;
                _chartLineManager.RemoveAllLines();
                _chartLineManager.Detach();
            }

            // Отписаться от FastOrderHandler
            if (_fastOrderHandler != null)
            {
                _fastOrderHandler.Detach();
            }

            Print("COP v1 stopped.");
        }

        #endregion

        #region Core logic — RecalculateAll

        /// <summary>
        /// Пересчитать все данные: объём, убыток, прибыль, RR, валидацию.
        /// Обновить панель и тексты на линиях.
        /// </summary>
        private void RecalculateAll()
        {
            if (_isLimitMode == null)
                return; // IDLE — нечего считать

            // === 1. Получить цены ===
            double entryPrice;
            double slPrice = _chartLineManager.StopLossPrice;
            double tpPrice = _chartLineManager.TakeProfitPrice;

            if (_isLimitMode == true)
            {
                // Limit-режим: цена входа = синяя линия
                entryPrice = _chartLineManager.EntryPrice;
            }
            else
            {
                // Market-режим: цена входа = текущая рыночная
                entryPrice = Symbol.Bid;
            }

            // Защита: пропустить расчёт если цены невалидны
            if (entryPrice <= 0 || slPrice <= 0 || tpPrice <= 0
                || double.IsNaN(entryPrice) || double.IsNaN(slPrice) || double.IsNaN(tpPrice))
                return;

            // === 2. Валидация уровней ===
            _lastDirection = LevelValidator.Validate(entryPrice, slPrice, tpPrice);

            // Для Market: если Buy → Ask, если Sell → Bid
            if (_isLimitMode == false && _lastDirection == OrderDirection.Long)
                entryPrice = Symbol.Ask;
            else if (_isLimitMode == false)
                entryPrice = Symbol.Bid;

            // === 3. Получить риск и пересчитать объём ===
            if (!TryGetRiskAmountInAccountCurrency(out double riskMoneyAccount, out string riskError))
            {
                _mainPanel.ShowRiskError(riskError);
                return;
            }
            _mainPanel.ClearRiskError();

            // === 4. Рассчитать объём ===
            _lastVolumeUnits = _riskCalculator.CalculateVolumeFromRiskAmount(entryPrice, slPrice, riskMoneyAccount);
            _lastVolumeLots = _riskCalculator.ToLots(_lastVolumeUnits);

            // === 5. Рассчитать убыток (полная позиция по SL) ===
            double slDollars, slPercent;
            _riskCalculator.CalculateLoss(entryPrice, slPrice, _lastVolumeUnits, out slDollars, out slPercent);

            int tpN = _mainPanel.TpCount;
            double tpDollars, tpPercent;
            double rr = _riskCalculator.CalculateRR(entryPrice, slPrice, tpPrice);
            double[] tpPricesMulti = null;
            double[] volsMulti = null;

            if (tpN >= 2 && _lastDirection != OrderDirection.Invalid)
            {
                tpPricesMulti = GetTpPricesArray();
                volsMulti = SplitVolumesForTps(_lastVolumeUnits, entryPrice, tpPricesMulti, _lastDirection == OrderDirection.Long);
                double sumProfit = 0;
                int legs = Math.Min(volsMulti.Length, tpPricesMulti.Length);
                for (int i = 0; i < legs; i++)
                {
                    double d, pct;
                    _riskCalculator.CalculateProfit(entryPrice, tpPricesMulti[i], volsMulti[i], out d, out pct);
                    sumProfit += d;
                }
                tpDollars = sumProfit;
                tpPercent = Account.Balance > 0 ? (sumProfit / Account.Balance) * 100.0 : 0;
            }
            else
                _riskCalculator.CalculateProfit(entryPrice, tpPrice, _lastVolumeUnits, out tpDollars, out tpPercent);

            // === 6–7. Обновить панель ===
            int digits = Symbol.Digits;

            if (_isLimitMode == true)
                _mainPanel.UpdateEntryPrice(entryPrice, digits);
            // Market цена обновляется в OnTick → UpdateMarketPrice

            _mainPanel.UpdateStopLoss(slPrice, digits, slDollars, slPercent);
            _mainPanel.UpdateTakeProfit(tpPrice, digits, tpDollars, tpPercent, tpN, _mainPanel.TpVolumeMode);

            // === 8. Обновить кнопку подтверждения ===
            string volText = _lastVolumeLots.ToString("F2");
            bool isLimit = _isLimitMode == true;

            _mainPanel.UpdateSubmitButton((int)_lastDirection, isLimit, Symbol.Name, volText);

            // === 9. Обновить тексты на линиях ===
            if (_chartLineManager.HasEntryLine)
            {
                string entryLineText;
                if (_isLimitMode == true)
                    entryLineText = Localization.Get("LimitText", _lastVolumeLots.ToString("F2"));
                else
                    entryLineText = Localization.Get("MarketText", _lastVolumeLots.ToString("F2"));

                _chartLineManager.UpdateLineTextPosition(
                    ChartLineManager.EntryTextId,
                    entryPrice,
                    entryLineText);
            }

            _chartLineManager.UpdateLineTextPosition(
                ChartLineManager.SlTextId,
                slPrice,
                Localization.Get("StopText", slPercent.ToString("F2"),
                    ChartLineManager.FormatChartMoneyDollar(slDollars)));

            // === TP: одна линия или подпись на каждой (объём по SplitVolumesForTps) ===
            if (tpN >= 2 && tpPricesMulti != null && volsMulti != null && _lastDirection != OrderDirection.Invalid)
            {
                string[] tpTextIds = { ChartLineManager.Tp1TextId, ChartLineManager.Tp2TextId, ChartLineManager.Tp3TextId };
                string[] tpLineIds = { ChartLineManager.Tp1LineId, ChartLineManager.Tp2LineId, ChartLineManager.Tp3LineId };
                for (int i = 0; i < tpN; i++)
                {
                    if (_fastOrderHandler.IsActive && Chart.FindObject(tpLineIds[i]) is not ChartHorizontalLine)
                    {
                        Chart.RemoveObject(tpTextIds[i]);
                        continue;
                    }

                    double volI = volsMulti.Length == 1
                        ? (i == tpN - 1 ? volsMulti[0] : 0)
                        : (i < volsMulti.Length ? volsMulti[i] : 0);
                    double pLeg, pLegPct;
                    _riskCalculator.CalculateProfit(entryPrice, tpPricesMulti[i], volI, out pLeg, out pLegPct);
                    double rrI = _riskCalculator.CalculateRR(entryPrice, slPrice, tpPricesMulti[i]);
                    string tpLabel = volI > 0
                        ? Localization.Get("TpText", rrI.ToString("F1"), ChartLineManager.FormatChartMoneyDollar(pLeg))
                        : ChartLineManager.InitialTpLineLabel();
                    _chartLineManager.UpdateLineTextPosition(tpTextIds[i], tpPricesMulti[i], tpLabel);
                }
            }
            else
            {
                if (_fastOrderHandler.IsActive && Chart.FindObject(_chartLineManager.MainTpLineId) is not ChartHorizontalLine)
                    Chart.RemoveObject(_chartLineManager.MainTpTextId);
                else
                {
                    _chartLineManager.UpdateLineTextPosition(
                        _chartLineManager.MainTpTextId,
                        tpPrice,
                        Localization.Get("TpText",
                            rr.ToString("F1"),
                            ChartLineManager.FormatChartMoneyDollar(tpDollars)));
                }
            }
        }

        /// <summary>
        /// Смена «число тейков» или «равный объём / равный профит» в настройках: синхронизировать линии с панелью и пересчитать.
        /// </summary>
        private void HandleTpAllocationSettingsChanged()
        {
            if (_isLimitMode == null || _fastOrderHandler.IsActive)
                return;

            if (_chartLineManager.HasAnyLines)
            {
                int want = _mainPanel.TpCount;
                int have = _chartLineManager.DisplayedTpCount;
                if (want != have)
                    SyncChartTpLineCountToPanel(want, have);
            }

            RecalculateAll();
        }

        /// <summary>
        /// Подогнать число линий TP под выбор в настройках, сохранив цены по возможности.
        /// </summary>
        private void SyncChartTpLineCountToPanel(int want, int have)
        {
            double e = _chartLineManager.EntryPrice;
            double sl = _chartLineManager.StopLossPrice;
            double p1 = _chartLineManager.TakeProfitPrice1;
            double p2 = _chartLineManager.TakeProfitPrice2;
            double p3 = _chartLineManager.TakeProfitPrice3;
            bool limit = _isLimitMode == true;

            if (want == 1)
            {
                double far = have == 1 ? p1 : (have == 2 ? p2 : p3);
                _chartLineManager.RestoreLinesFromPrices(limit, 1, e, sl, far, 0, 0);
                return;
            }

            if (want == 2)
            {
                if (have == 1)
                {
                    double mid = (e + p1) * 0.5;
                    _chartLineManager.RestoreLinesFromPrices(limit, 2, e, sl, mid, p1, 0);
                }
                else
                    _chartLineManager.RestoreLinesFromPrices(limit, 2, e, sl, p1, p3, 0);
                return;
            }

            // want == 3
            if (have == 1)
            {
                bool tpAbove = p1 > e;
                if (tpAbove)
                {
                    double step = (p1 - e) / 3.0;
                    _chartLineManager.RestoreLinesFromPrices(limit, 3, e, sl, e + step, e + 2.0 * step, p1);
                }
                else
                {
                    double step = (e - p1) / 3.0;
                    _chartLineManager.RestoreLinesFromPrices(limit, 3, e, sl, e - step, e - 2.0 * step, p1);
                }
                return;
            }

            // have == 2 → 3
            bool longTp = p2 > p1;
            if (longTp)
            {
                double p3n = p2 + (p2 - p1);
                _chartLineManager.RestoreLinesFromPrices(limit, 3, e, sl, p1, p2, p3n);
            }
            else
            {
                double p3n = p2 - (p1 - p2);
                _chartLineManager.RestoreLinesFromPrices(limit, 3, e, sl, p1, p2, p3n);
            }
        }

        /// <summary>
        /// Получить массив цен TP по текущему TpCount (1, 2 или 3).
        /// </summary>
        private double[] GetTpPricesArray()
        {
            int n = _mainPanel.TpCount;
            if (n == 1)
                return new[] { _chartLineManager.TakeProfitPrice1 };
            if (n == 2)
                return new[] { _chartLineManager.TakeProfitPrice1, _chartLineManager.TakeProfitPrice2 };
            return new[] { _chartLineManager.TakeProfitPrice1, _chartLineManager.TakeProfitPrice2, _chartLineManager.TakeProfitPrice3 };
        }

        /// <summary>
        /// Поделить общий объём на N тейков (равный объём или равный профит).
        /// Если объём на часть меньше минимального — возвращает один элемент (весь объём на последний TP).
        /// </summary>
        private double[] SplitVolumesForTps(double totalVolumeUnits, double entryPrice, double[] tpPrices, bool isLong)
        {
            if (tpPrices == null || tpPrices.Length == 0)
                return new[] { totalVolumeUnits };

            int n = tpPrices.Length;
            double minVol = Symbol.VolumeInUnitsMin;

            if (totalVolumeUnits / n < minVol)
            {
                // Не хватает объёма на N ордеров — один ордер с последним TP
                return new[] { _riskCalculator.NormalizeVolume(totalVolumeUnits) };
            }

            double[] volumes = new double[n];

            if (_mainPanel.TpVolumeMode == TpVolumeMode.EqualVolume)
            {
                double raw = totalVolumeUnits / n;
                for (int i = 0; i < n; i++)
                    volumes[i] = _riskCalculator.NormalizeVolume(raw);
            }
            else
            {
                // Равный профит: v_i ~ 1/|tp_i - entry|
                double[] dist = new double[n];
                double sumInv = 0;
                for (int i = 0; i < n; i++)
                {
                    dist[i] = Math.Abs(tpPrices[i] - entryPrice);
                    if (dist[i] < Symbol.PipSize * 0.1) dist[i] = Symbol.PipSize * 0.1;
                    sumInv += 1.0 / dist[i];
                }
                for (int i = 0; i < n; i++)
                {
                    double raw = totalVolumeUnits * (1.0 / dist[i]) / sumInv;
                    volumes[i] = _riskCalculator.NormalizeVolume(raw);
                }
            }

            return volumes;
        }

        /// <summary>
        /// Загрузить сохранённый % риска из LocalStorage.
        /// Если значение не найдено или невалидно — возвращает MaxRiskPercent (параметр бота).
        /// </summary>
        private double LoadSavedRiskPercent()
        {
            const string key = "COP MaxRiskPercent";
            try
            {
                // Читаем явно из Device — туда же сохраняем при смене процента
                string saved = LocalStorage.GetString(key, LocalStorageScope.Device);
                if (string.IsNullOrWhiteSpace(saved))
                    return MaxRiskPercent;

                string cleaned = saved.Replace(',', '.');
                if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    double clamped = Math.Max(0.1, Math.Min(result, 100.0));
                    return clamped;
                }
            }
            catch
            {
                // LocalStorage может быть недоступен (например, в части окружений)
            }
            return MaxRiskPercent;
        }

        private const string TransparencyStorageKey = "COP PanelTransparencyPercent";

        private const string ScaleStorageKey = "COP PanelScalePercent";

        /// <summary>
        /// Загрузить сохранённый масштаб панели (80–150 %). Если нет или невалидно — <c>-1</c>.
        /// </summary>
        private int LoadSavedScalePercent()
        {
            try
            {
                string saved = LocalStorage.GetString(ScaleStorageKey, LocalStorageScope.Device);
                if (string.IsNullOrWhiteSpace(saved)) return -1;
                saved = saved.Trim();
                if (int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                    return Math.Max(80, Math.Min(result, 150));
            }
            catch { }
            return -1;
        }

        private void SaveScalePercent(int percent)
        {
            try
            {
                int clamped = Math.Max(80, Math.Min(percent, 150));
                LocalStorage.SetString(ScaleStorageKey, clamped.ToString(CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.Flush(LocalStorageScope.Device);
            }
            catch { }
        }

        private int LoadSavedTransparency()
        {
            try
            {
                string saved = LocalStorage.GetString(TransparencyStorageKey, LocalStorageScope.Device);
                if (string.IsNullOrWhiteSpace(saved)) return -1;
                saved = saved.Trim();
                if (int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                    return Math.Max(0, Math.Min(result, 80));
            }
            catch { }
            return -1;
        }

        private void SaveTransparency(int percent)
        {
            try
            {
                int clamped = Math.Max(0, Math.Min(percent, 80));
                LocalStorage.SetString(TransparencyStorageKey, clamped.ToString(CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.Flush(LocalStorageScope.Device);
            }
            catch { }
        }

        #region LocalStorage — черновик уровней на графике (перезапуск cBot при смене ТФ)

        private const string ChartLevelsKeyActive = "COP ChartLevels Active";
        private const string ChartLevelsKeySymbol = "COP ChartLevels Symbol";
        private const string ChartLevelsKeyAccount = "COP ChartLevels Account";
        private const string ChartLevelsKeyLimit = "COP ChartLevels Limit";
        private const string ChartLevelsKeyTpCount = "COP ChartLevels TpCount";
        private const string ChartLevelsKeyE = "COP ChartLevels E";
        private const string ChartLevelsKeyS = "COP ChartLevels S";
        private const string ChartLevelsKeyT1 = "COP ChartLevels T1";
        private const string ChartLevelsKeyT2 = "COP ChartLevels T2";
        private const string ChartLevelsKeyT3 = "COP ChartLevels T3";

        private void ClearChartLevelsDraft()
        {
            try
            {
                LocalStorage.SetString(ChartLevelsKeyActive, "0", LocalStorageScope.Device);
                LocalStorage.Flush(LocalStorageScope.Device);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Сохранить Entry/SL/TP перед OnStop: при смене таймфрейма cTrader перезапускает cBot, события графика не помогают.
        /// </summary>
        private void SaveChartLevelsDraft()
        {
            try
            {
                if (_chartLineManager == null || _mainPanel == null)
                    return;

                if (_isLimitMode == null)
                {
                    ClearChartLevelsDraft();
                    return;
                }

                if ((_fastOrderHandler != null && _fastOrderHandler.IsActive) || _mainPanel.IsFastOrder)
                {
                    ClearChartLevelsDraft();
                    return;
                }

                double e = _chartLineManager.EntryPrice;
                double s = _chartLineManager.StopLossPrice;
                double t1 = _chartLineManager.TakeProfitPrice1;
                int tpN = _mainPanel.TpCount;
                double t2 = tpN >= 2 ? _chartLineManager.TakeProfitPrice2 : 0;
                double t3 = tpN >= 3 ? _chartLineManager.TakeProfitPrice3 : 0;

                if (e <= 0 || s <= 0 || t1 <= 0 || double.IsNaN(e) || double.IsNaN(s) || double.IsNaN(t1))
                {
                    ClearChartLevelsDraft();
                    return;
                }

                LocalStorage.SetString(ChartLevelsKeyActive, "1", LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeySymbol, Symbol.Name, LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyAccount, Account.Number.ToString(CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyLimit, _isLimitMode == true ? "1" : "0", LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyTpCount, tpN.ToString(CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyE, e.ToString("G17", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyS, s.ToString("G17", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyT1, t1.ToString("G17", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyT2, t2.ToString("G17", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.SetString(ChartLevelsKeyT3, t3.ToString("G17", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                LocalStorage.Flush(LocalStorageScope.Device);
            }
            catch
            {
                // ignore
            }
        }

        private static bool TryParseChartLevelDouble(string raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            return double.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void TryRestoreChartLevelsAfterStart()
        {
            try
            {
                string active = LocalStorage.GetString(ChartLevelsKeyActive, LocalStorageScope.Device);
                if (active != "1")
                    return;

                string sym = LocalStorage.GetString(ChartLevelsKeySymbol, LocalStorageScope.Device);
                if (string.IsNullOrEmpty(sym) || sym != Symbol.Name)
                {
                    ClearChartLevelsDraft();
                    return;
                }

                string accSaved = LocalStorage.GetString(ChartLevelsKeyAccount, LocalStorageScope.Device);
                if (accSaved != Account.Number.ToString(CultureInfo.InvariantCulture))
                {
                    ClearChartLevelsDraft();
                    return;
                }

                bool limit = LocalStorage.GetString(ChartLevelsKeyLimit, LocalStorageScope.Device) == "1";
                string tpRaw = LocalStorage.GetString(ChartLevelsKeyTpCount, LocalStorageScope.Device);
                int tpCount = 2;
                if (!string.IsNullOrWhiteSpace(tpRaw) && int.TryParse(tpRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tpParsed))
                    tpCount = Math.Max(1, Math.Min(3, tpParsed));

                string eStr = LocalStorage.GetString(ChartLevelsKeyE, LocalStorageScope.Device);
                string sStr = LocalStorage.GetString(ChartLevelsKeyS, LocalStorageScope.Device);
                string t1Str = LocalStorage.GetString(ChartLevelsKeyT1, LocalStorageScope.Device);
                string t2Str = LocalStorage.GetString(ChartLevelsKeyT2, LocalStorageScope.Device);
                string t3Str = LocalStorage.GetString(ChartLevelsKeyT3, LocalStorageScope.Device);

                if (!TryParseChartLevelDouble(eStr, out double e)
                    || !TryParseChartLevelDouble(sStr, out double s)
                    || !TryParseChartLevelDouble(t1Str, out double t1))
                {
                    ClearChartLevelsDraft();
                    return;
                }

                TryParseChartLevelDouble(t2Str, out double t2);
                TryParseChartLevelDouble(t3Str, out double t3);

                if (e <= 0 || s <= 0 || t1 <= 0)
                {
                    ClearChartLevelsDraft();
                    return;
                }

                _mainPanel.ApplyRestoredTradingMode(limit, tpCount);
                _isLimitMode = limit ? true : false;
                _chartLineManager.RestoreLinesFromPrices(limit, tpCount, e, s, t1, t2, t3);

                if (!limit)
                    _mainPanel.UpdateMarketPrice(Symbol.Bid, Symbol.Ask, Symbol.Digits);

                RecalculateAll();
            }
            catch
            {
                ClearChartLevelsDraft();
            }
        }

        #endregion

        private double LoadSavedRiskUsd()
        {
            const string key = "COP MaxRiskUsd";
            try
            {
                string saved = LocalStorage.GetString(key, LocalStorageScope.Device);
                if (string.IsNullOrWhiteSpace(saved))
                    return MaxRiskUsd;
                string cleaned = saved.Replace(',', '.');
                if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                    return Math.Max(0.01, result);
            }
            catch { }
            return MaxRiskUsd;
        }

        private double LoadSavedRiskEur()
        {
            const string key = "COP MaxRiskEur";
            try
            {
                string saved = LocalStorage.GetString(key, LocalStorageScope.Device);
                if (string.IsNullOrWhiteSpace(saved))
                    return MaxRiskEur;
                string cleaned = saved.Replace(',', '.');
                if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                    return Math.Max(0.01, result);
            }
            catch { }
            return MaxRiskEur;
        }

        private RiskMode LoadSavedRiskMode()
        {
            const string key = "COP RiskMode";
            try
            {
                string saved = LocalStorage.GetString(key, LocalStorageScope.Device);
                if (string.IsNullOrWhiteSpace(saved))
                    return RiskMode.Unknown;
                if (Enum.TryParse(saved, true, out RiskMode mode))
                    return mode;
            }
            catch { }
            return RiskMode.Unknown;
        }

        private void SaveRiskMode(RiskMode mode)
        {
            try
            {
                LocalStorage.SetString("COP RiskMode", mode.ToString(), LocalStorageScope.Device);
                LocalStorage.Flush(LocalStorageScope.Device);
            }
            catch { }
        }

        private void SaveRiskValueForMode(RiskMode mode, string text)
        {
            try
            {
                switch (mode)
                {
                    case RiskMode.Percent:
                        {
                            double parsed = ParseRisk(text);
                            LocalStorage.SetString("COP MaxRiskPercent", parsed.ToString("F2", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                            break;
                        }
                    case RiskMode.USD:
                        {
                            double parsed = ParsePositiveNumber(text, MaxRiskUsd);
                            LocalStorage.SetString("COP MaxRiskUsd", parsed.ToString("F2", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                            break;
                        }
                    case RiskMode.EUR:
                        {
                            double parsed = ParsePositiveNumber(text, MaxRiskEur);
                            LocalStorage.SetString("COP MaxRiskEur", parsed.ToString("F2", CultureInfo.InvariantCulture), LocalStorageScope.Device);
                            break;
                        }
                }
                LocalStorage.Flush(LocalStorageScope.Device);
            }
            catch { }
        }

        private double ParsePositiveNumber(string text, double fallback)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;
            string cleaned = text.Replace(',', '.');
            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) && result > 0)
                return result;
            return fallback;
        }

        private void ApplyInitialRiskToPanel()
        {
            _mainPanel.SetRiskMode(_currentRiskMode);
            try
            {
                switch (_currentRiskMode)
                {
                    case RiskMode.Percent:
                        _mainPanel.SetRiskText(LoadSavedRiskPercent().ToString("F2", CultureInfo.InvariantCulture));
                        break;
                    case RiskMode.USD:
                        _mainPanel.SetRiskText(LoadSavedRiskUsd().ToString("F2", CultureInfo.InvariantCulture));
                        break;
                    case RiskMode.EUR:
                        _mainPanel.SetRiskText(LoadSavedRiskEur().ToString("F2", CultureInfo.InvariantCulture));
                        break;
                }
            }
            catch { }
        }

        private (RiskMode mode, string text) GetCurrentRiskInput() => (_currentRiskMode, _mainPanel.RiskText);

        private bool TryGetRiskAmountInAccountCurrency(out double riskMoneyAccount, out string error)
        {
            riskMoneyAccount = 0;
            error = null;

            string accountCurrency = Account.Asset?.Name;
            if (string.IsNullOrWhiteSpace(accountCurrency))
                accountCurrency = "USD";

            if (_currentRiskMode == RiskMode.Percent)
            {
                double percent = ParseRisk(_mainPanel.RiskText);
                riskMoneyAccount = Account.Balance * (percent / 100.0);
                return riskMoneyAccount > 0;
            }

            double riskMoney = ParsePositiveNumber(_mainPanel.RiskText, _currentRiskMode == RiskMode.USD ? MaxRiskUsd : MaxRiskEur);
            string from = _currentRiskMode == RiskMode.USD ? "USD" : "EUR";
            try
            {
                double converted = AssetConverter.Convert(riskMoney, from, accountCurrency);
                if (double.IsNaN(converted) || double.IsInfinity(converted) || converted <= 0)
                {
                    error = Localization.Get("RiskConvertError");
                    return false;
                }
                riskMoneyAccount = converted;
                return true;
            }
            catch
            {
                error = Localization.Get("RiskConvertError");
                return false;
            }
        }

        /// <summary>
        /// Распарсить значение риска из текстового поля.
        /// Поддерживает точку и запятую как разделитель.
        /// </summary>
        private double ParseRisk(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return MaxRiskPercent;

            string cleaned = text.Replace(',', '.');
            double result;
            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return Math.Max(0.1, Math.Min(result, 100.0));

            return MaxRiskPercent;
        }

        #endregion

        #region Event handlers — Panel

        private void HandleLimitClicked(bool isActive)
        {
            if (isActive)
            {
                _isLimitMode = true;
                _mainPanel.SetMode(true);
                _mainPanel.SetFieldsReadOnly(_mainPanel.IsFastOrder);

                if (_mainPanel.IsFastOrder)
                {
                    // Fast Order: линии привязываются к курсору
                    _fastOrderHandler.StartLimit(_mainPanel.TpCount);
                    // Кнопка подтверждения серая (ордер автоматически)
                    _mainPanel.UpdateSubmitButton(0, true, Symbol.Name, "0.00");
                    Print("Fast Limit mode activated — click to place Entry");
                }
                else
                {
                    // Обычный режим: линии перетаскиваемые
                    _chartLineManager.ShowLimitLines(_mainPanel.TpCount);
                    RecalculateAll();
                    Print("Limit mode activated — 3 lines shown");
                }
            }
            else
            {
                // Деактивация
                ClearChartLevelsDraft();
                if (_mainPanel.IsFastOrder && _fastOrderHandler.IsActive)
                    _fastOrderHandler.Cancel();
                else
                    _chartLineManager.RemoveAllLines();

                _isLimitMode = null;
                _mainPanel.ResetToIdle();
                _lastDirection = OrderDirection.Invalid;
                Print("Limit mode deactivated");
            }
        }

        private void HandleMarketClicked(bool isActive)
        {
            if (isActive)
            {
                _isLimitMode = false;
                _mainPanel.SetMode(false);
                _mainPanel.SetFieldsReadOnly(_mainPanel.IsFastOrder);

                if (_mainPanel.IsFastOrder)
                {
                    // Fast Order Market: SL и TP по кликам
                    _fastOrderHandler.StartMarket(_mainPanel.TpCount);
                    _mainPanel.UpdateMarketPrice(Symbol.Bid, Symbol.Ask, Symbol.Digits);
                    _mainPanel.UpdateSubmitButton(0, false, Symbol.Name, "0.00");
                    Print("Fast Market mode activated — click to place SL");
                }
                else
                {
                    // Обычный режим
                    _chartLineManager.ShowMarketLines(_mainPanel.TpCount);
                    _mainPanel.UpdateMarketPrice(Symbol.Bid, Symbol.Ask, Symbol.Digits);
                    RecalculateAll();
                    Print("Market mode activated — 2 lines shown");
                }
            }
            else
            {
                // Деактивация
                ClearChartLevelsDraft();
                if (_mainPanel.IsFastOrder && _fastOrderHandler.IsActive)
                    _fastOrderHandler.Cancel();
                else
                    _chartLineManager.RemoveAllLines();

                _isLimitMode = null;
                _mainPanel.ResetToIdle();
                _lastDirection = OrderDirection.Invalid;
                Print("Market mode deactivated");
            }
        }

        private void HandleSubmitClicked()
        {
            if (_lastDirection == OrderDirection.Invalid || _isLimitMode == null)
            {
                Print("Cannot place order: invalid levels or no mode selected");
                return;
            }

            double slPrice = _chartLineManager.StopLossPrice;
            double entryPrice = _isLimitMode == true
                ? _chartLineManager.EntryPrice
                : ( _lastDirection == OrderDirection.Long ? Symbol.Ask : Symbol.Bid );

            double[] tpPrices = GetTpPricesArray();
            bool isLong = _lastDirection == OrderDirection.Long;
            double[] volumes = SplitVolumesForTps(_lastVolumeUnits, entryPrice, tpPrices, isLong);

            // Размещаем столько ордеров, сколько объёмов (1, 2 или 3)
            int placed = 0;
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i] <= 0) continue;
                // При одном ордере (fallback) — используем последний TP
                double tpPrice = (volumes.Length == 1) ? tpPrices[tpPrices.Length - 1] : (i < tpPrices.Length ? tpPrices[i] : tpPrices[tpPrices.Length - 1]);
                TradeResult result;

                if (_isLimitMode == true)
                    result = _orderManager.PlaceLimitOrder(entryPrice, slPrice, tpPrice, volumes[i]);
                else
                    result = _orderManager.PlaceMarketOrder(slPrice, tpPrice, volumes[i]);

                if (result.IsSuccessful)
                    placed++;
                else
                    Print("Order {0} FAILED: {1}", i + 1, result.Error);
            }

            if (placed > 0)
            {
                Print("Placed {0} order(s): {1} {2} total vol={3}",
                    placed, _isLimitMode == true ? "LIMIT" : "MARKET", Symbol.Name, _lastVolumeLots.ToString("F2"));
            }
            else
            {
                Print("All orders FAILED");
            }

            ClearChartLevelsDraft();
            _chartLineManager.RemoveAllLines();
            _mainPanel.ResetToIdle();
            _mainPanel.Collapse();
            _isLimitMode = null;
            _lastDirection = OrderDirection.Invalid;
        }

        private void HandleRiskChanged(string newText)
        {
            SaveRiskValueForMode(_currentRiskMode, newText);

            // Пересчитать всё при изменении риска
            if (_isLimitMode != null && _chartLineManager.HasAnyLines)
            {
                RecalculateAll();
            }
        }

        private void HandleRiskModeChanged(RiskMode newMode)
        {
            if (newMode == _currentRiskMode)
                return;

            _currentRiskMode = newMode;
            SaveRiskMode(_currentRiskMode);

            switch (_currentRiskMode)
            {
                case RiskMode.Percent:
                    _mainPanel.SetRiskText(LoadSavedRiskPercent().ToString("F2", CultureInfo.InvariantCulture));
                    break;
                case RiskMode.USD:
                    _mainPanel.SetRiskText(LoadSavedRiskUsd().ToString("F2", CultureInfo.InvariantCulture));
                    break;
                case RiskMode.EUR:
                    _mainPanel.SetRiskText(LoadSavedRiskEur().ToString("F2", CultureInfo.InvariantCulture));
                    break;
            }

            if (_isLimitMode != null && _chartLineManager.HasAnyLines)
                RecalculateAll();
        }

        private void HandleFastOrderToggled(bool isEnabled)
        {
            Print("Fast Order toggled: {0}", isEnabled);

            // Если шёл процесс Fast Order — отменить
            if (_fastOrderHandler.IsActive)
            {
                ClearChartLevelsDraft();
                _fastOrderHandler.Cancel();
                _isLimitMode = null;
                _mainPanel.ResetToIdle();
                _lastDirection = OrderDirection.Invalid;
                return;
            }

            // Если переключили во время активного обычного режима
            if (_isLimitMode != null && _chartLineManager.HasAnyLines)
            {
                ClearChartLevelsDraft();
                // Убрать линии обычного режима, начать заново
                _chartLineManager.RemoveAllLines();
                _isLimitMode = null;
                _mainPanel.ResetToIdle();
                _lastDirection = OrderDirection.Invalid;
            }
        }

        #endregion

        #region Event handlers — Manual price input

        /// <summary>
        /// Пользователь ввёл цену Entry вручную в поле "Limit Order".
        /// Перемещаем синюю линию и пересчитываем всё.
        /// </summary>
        private void HandlePriceFieldChanged(string text)
        {
            // Только в Limit-режиме (в Market поле readonly)
            if (_isLimitMode != true || _mainPanel.IsFastOrder)
                return;

            double price;
            if (TryParseFieldPrice(text, out price))
            {
                _chartLineManager.MoveLineTo(ChartLineManager.EntryLineId, price);
                _chartLineManager.UpdateLineTextPosition(
                    ChartLineManager.EntryTextId,
                    price,
                    Localization.Get("LimitText", "0.00"));
                RecalculateAll();
            }
        }

        /// <summary>
        /// Пользователь ввёл цену Stop Loss вручную.
        /// Перемещаем красную линию и пересчитываем.
        /// </summary>
        private void HandleSlFieldChanged(string text)
        {
            if (_isLimitMode == null || _mainPanel.IsFastOrder)
                return;

            double price;
            if (TryParseFieldPrice(text, out price))
            {
                _chartLineManager.MoveLineTo(ChartLineManager.SlLineId, price);
                _chartLineManager.UpdateLineTextPosition(
                    ChartLineManager.SlTextId,
                    price,
                    ChartLineManager.InitialStopLineLabel());
                RecalculateAll();
            }
        }

        /// <summary>
        /// Пользователь ввёл цену Take Profit вручную.
        /// Перемещаем зелёную линию и пересчитываем.
        /// </summary>
        private void HandleTpFieldChanged(string text)
        {
            if (_isLimitMode == null || _mainPanel.IsFastOrder)
                return;

            double price;
            if (TryParseFieldPrice(text, out price))
            {
                _chartLineManager.MoveLineTo(_chartLineManager.MainTpLineId, price);
                _chartLineManager.UpdateLineTextPosition(
                    _chartLineManager.MainTpTextId,
                    price,
                    ChartLineManager.InitialTpLineLabel());
                RecalculateAll();
            }
        }

        /// <summary>
        /// Парсинг цены из текстового поля (точка/запятая, положительное число).
        /// </summary>
        private bool TryParseFieldPrice(string text, out double price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string cleaned = text.Replace(',', '.');
            if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out price) && price > 0)
                return true;

            return false;
        }

        #endregion

        #region Event handlers — Chart lines

        private void HandleLinesChanged()
        {
            // Пересчитать всё при перетаскивании линий (только в обычном режиме)
            if (_isLimitMode != null && !_fastOrderHandler.IsActive)
            {
                RecalculateAll();
            }
        }

        #endregion

        #region Fast Order callback

        /// <summary>
        /// Вызывается FastOrderHandler когда все уровни зафиксированы кликами.
        /// Размещает ордер, убирает линии, сворачивает панель.
        /// </summary>
        private void HandleFastOrderReady(double entryPrice, double slPrice, double[] tpPrices, bool isMarket)
        {
            if (tpPrices == null || tpPrices.Length == 0)
                return;

            OrderDirection direction = LevelValidator.Validate(entryPrice, slPrice, tpPrices[0]);
            if (direction == OrderDirection.Invalid)
            {
                Print("Fast Order: invalid levels — entry={0} sl={1} tp={2}",
                    entryPrice.ToString("F" + Symbol.Digits),
                    slPrice.ToString("F" + Symbol.Digits),
                    tpPrices[0].ToString("F" + Symbol.Digits));
                ClearChartLevelsDraft();
                _chartLineManager.RemoveAllLines();
                _mainPanel.ResetToIdle();
                _isLimitMode = null;
                _lastDirection = OrderDirection.Invalid;
                return;
            }

            if (isMarket)
                entryPrice = direction == OrderDirection.Long ? Symbol.Ask : Symbol.Bid;

            if (!TryGetRiskAmountInAccountCurrency(out double riskMoneyAccount, out string riskError))
            {
                _mainPanel.ShowRiskError(riskError);
                ClearChartLevelsDraft();
                _chartLineManager.RemoveAllLines();
                _mainPanel.ResetToIdle();
                _isLimitMode = null;
                _lastDirection = OrderDirection.Invalid;
                return;
            }
            _mainPanel.ClearRiskError();

            double totalVolumeUnits = _riskCalculator.CalculateVolumeFromRiskAmount(entryPrice, slPrice, riskMoneyAccount);
            bool isLong = direction == OrderDirection.Long;
            double[] volumes = SplitVolumesForTps(totalVolumeUnits, entryPrice, tpPrices, isLong);

            int placed = 0;
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i] <= 0) continue;
                double tpPrice = (volumes.Length == 1) ? tpPrices[tpPrices.Length - 1] : (i < tpPrices.Length ? tpPrices[i] : tpPrices[tpPrices.Length - 1]);

                TradeResult result;
                if (isMarket)
                    result = _orderManager.PlaceMarketOrder(slPrice, tpPrice, volumes[i]);
                else
                    result = _orderManager.PlaceLimitOrder(entryPrice, slPrice, tpPrice, volumes[i]);

                if (result.IsSuccessful)
                    placed++;
                else
                    Print("Fast Order {0} FAILED: {1}", i + 1, result.Error);
            }

            if (placed > 0)
            {
                Print("Fast Order: placed {0} order(s) {1} {2}",
                    placed, isMarket ? "MARKET" : "LIMIT", Symbol.Name);
            }
            else
            {
                Print("Fast Order: all orders FAILED");
            }

            ClearChartLevelsDraft();
            _chartLineManager.RemoveAllLines();
            _mainPanel.ResetToIdle();
            _mainPanel.Collapse();
            _isLimitMode = null;
            _lastDirection = OrderDirection.Invalid;
        }

        #endregion

        #region MainPanel — масштаб (пересоздание UI)

        private struct PanelUiSnapshot
        {
            public int TransparencyPercent;
            public bool SettingsOpen;
            public string RiskText;
            public bool FastOrder;
            public int TpCount;
            public TpVolumeMode TpVolumeMode;
            public bool Collapsed;
            public bool? LimitMode;
        }

        private void SubscribePanelEvents()
        {
            _mainPanel.OnLimitClicked += HandleLimitClicked;
            _mainPanel.OnMarketClicked += HandleMarketClicked;
            _mainPanel.OnSubmitClicked += HandleSubmitClicked;
            _mainPanel.OnRiskChanged += HandleRiskChanged;
            _mainPanel.OnRiskModeChanged += HandleRiskModeChanged;
            _mainPanel.OnFastOrderToggled += HandleFastOrderToggled;
            _mainPanel.OnPriceChanged += HandlePriceFieldChanged;
            _mainPanel.OnSlChanged += HandleSlFieldChanged;
            _mainPanel.OnTpChanged += HandleTpFieldChanged;
            _mainPanel.OnTransparencyChanged += SaveTransparency;
            _mainPanel.OnTpAllocationSettingsChanged += HandleTpAllocationSettingsChanged;
            _mainPanel.OnScaleChanged += HandlePanelScaleChanged;
        }

        private void UnsubscribePanelEvents()
        {
            if (_mainPanel == null)
                return;
            _mainPanel.OnLimitClicked -= HandleLimitClicked;
            _mainPanel.OnMarketClicked -= HandleMarketClicked;
            _mainPanel.OnSubmitClicked -= HandleSubmitClicked;
            _mainPanel.OnRiskChanged -= HandleRiskChanged;
            _mainPanel.OnRiskModeChanged -= HandleRiskModeChanged;
            _mainPanel.OnFastOrderToggled -= HandleFastOrderToggled;
            _mainPanel.OnPriceChanged -= HandlePriceFieldChanged;
            _mainPanel.OnSlChanged -= HandleSlFieldChanged;
            _mainPanel.OnTpChanged -= HandleTpFieldChanged;
            _mainPanel.OnTransparencyChanged -= SaveTransparency;
            _mainPanel.OnTpAllocationSettingsChanged -= HandleTpAllocationSettingsChanged;
            _mainPanel.OnScaleChanged -= HandlePanelScaleChanged;
        }

        private PanelUiSnapshot CapturePanelUiSnapshot()
        {
            return new PanelUiSnapshot
            {
                TransparencyPercent = _mainPanel.CurrentTransparencyPercent,
                SettingsOpen = _mainPanel.IsSettingsPanelVisible,
                RiskText = _mainPanel.RiskText,
                FastOrder = _mainPanel.IsFastOrder,
                TpCount = _mainPanel.TpCount,
                TpVolumeMode = _mainPanel.TpVolumeMode,
                Collapsed = _mainPanel.CollapsedState,
                LimitMode = _isLimitMode
            };
        }

        private void RestoreMainPanelUi(PanelUiSnapshot s)
        {
            _mainPanel.RestoreSettingsPanelOpenState(s.SettingsOpen);
            _mainPanel.RestoreTpComboSettings(s.TpCount, s.TpVolumeMode);
            _mainPanel.SetRiskMode(_currentRiskMode);
            if (!string.IsNullOrWhiteSpace(s.RiskText))
                _mainPanel.SetRiskText(s.RiskText);
            _mainPanel.SetFastOrderChecked(s.FastOrder);

            if (s.LimitMode == null)
                _mainPanel.ResetToIdle();
            else
                _mainPanel.ApplyRestoredTradingMode(s.LimitMode == true, s.TpCount);

            _mainPanel.ClearRiskError();
            _mainPanel.UpdateSpread(Symbol.Spread / Symbol.PipSize);
        }

        private void HandlePanelScaleChanged(int percent)
        {
            int clamped = Math.Max(80, Math.Min(150, percent));
            if (clamped == PanelStyles.ScalePercent)
                return;

            if (_fastOrderHandler != null && _fastOrderHandler.IsActive)
            {
                ClearChartLevelsDraft();
                _fastOrderHandler.Cancel();
                _isLimitMode = null;
            }

            PanelUiSnapshot snapshot = CapturePanelUiSnapshot();

            SaveScalePercent(clamped);
            PanelStyles.SetScalePercent(clamped);

            UnsubscribePanelEvents();
            Chart.RemoveControl(_mainPanel.RootControl);
            _mainPanel = null;

            MainPanel.SetSavedCollapsedStateForRestore(snapshot.Collapsed);
            _mainPanel = new MainPanel(this, VPosition, HPosition, MaxRiskPercent, FastOrderMode == YesNo.Yes, snapshot.TransparencyPercent);
            Chart.AddControl(_mainPanel.RootControl);
            SubscribePanelEvents();
            RestoreMainPanelUi(snapshot);

            if (_chartLineManager != null && _isLimitMode != null && !_fastOrderHandler.IsActive)
                RecalculateAll();
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Вертикальная позиция панели на графике.
    /// </summary>
    public enum VerticalPosition
    {
        Top,
        Center,
        Bottom
    }

    /// <summary>
    /// Горизонтальная позиция панели на графике.
    /// </summary>
    public enum HorizontalPosition
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Масштаб панели в настройках экземпляра бота (выпадающий список 80–150 %, шаг 10).
    /// </summary>
    public enum PanelScale
    {
        Scale80 = 80,
        Scale90 = 90,
        Scale100 = 100,
        Scale110 = 110,
        Scale120 = 120,
        Scale130 = 130,
        Scale140 = 140,
        Scale150 = 150
    }

    /// <summary>
    /// Варианты выбора для параметров (Yes/No) — отображаются в настройках робота только на английском.
    /// </summary>
    public enum YesNo
    {
        No,
        Yes
    }

    /// <summary>
    /// Язык интерфейса панели.
    /// </summary>
    public enum Language
    {
        EN,
        RU,
        DE,
        FR,
        ES,
        IT,
        PL,
        NL,
        PT
    }

    public enum RiskMode
    {
        Unknown = 0,
        Percent = 1,
        USD = 2,
        EUR = 3
    }

    #endregion
}
