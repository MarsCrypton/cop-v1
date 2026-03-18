using System;
using cAlgo.API;
using COP_v1.UI;

namespace COP_v1.Chart
{
    /// <summary>
    /// Управление виртуальными линиями на графике (Entry, Stop Loss, Take Profit).
    /// Рисует интерактивные горизонтальные линии с текстовыми подписями.
    /// Отслеживает перетаскивание и уведомляет через событие OnLinesChanged.
    /// </summary>
    public class ChartLineManager
    {
        // ID объектов на графике
        public const string EntryLineId = "cop_entry";
        public const string EntryTextId = "cop_entry_text";
        public const string SlLineId    = "cop_sl";
        public const string SlTextId    = "cop_sl_text";
        public const string Tp1LineId   = "cop_tp1";
        public const string Tp1TextId   = "cop_tp1_text";
        public const string Tp2LineId   = "cop_tp2";
        public const string Tp2TextId   = "cop_tp2_text";
        public const string Tp3LineId   = "cop_tp3";
        public const string Tp3TextId   = "cop_tp3_text";

        /// <summary>ID основной линии TP (одна при 1 тейке, иначе дальней).</summary>
        public string MainTpLineId => _tpCount == 1 ? Tp1LineId : (_tpCount == 2 ? Tp2LineId : Tp3LineId);
        /// <summary>ID подписи основной линии TP.</summary>
        public string MainTpTextId => _tpCount == 1 ? Tp1TextId : (_tpCount == 2 ? Tp2TextId : Tp3TextId);

        private readonly Robot _bot;

        // Кэшированные цены (обновляются при перетаскивании)
        private double _entryPrice;
        private double _slPrice;
        private double _tp1Price;
        private double _tp2Price;
        private double _tp3Price;
        private int _tpCount = 2;

        /// <summary>
        /// Вызывается при перетаскивании любой из наших линий.
        /// </summary>
        public event Action OnLinesChanged;

        public ChartLineManager(Robot bot)
        {
            _bot = bot;
            _bot.Chart.ObjectsUpdated += Chart_ObjectsUpdated;
        }

        #region Public properties

        /// <summary>Текущая цена линии Entry (лимитный ордер).</summary>
        public double EntryPrice => GetPrice(EntryLineId, _entryPrice);

        /// <summary>Текущая цена линии Stop Loss.</summary>
        public double StopLossPrice => GetPrice(SlLineId, _slPrice);

        /// <summary>Текущая цена основной линии TP.</summary>
        public double TakeProfitPrice => _tpCount == 1 ? GetPrice(Tp1LineId, _tp1Price) : (_tpCount == 2 ? GetPrice(Tp2LineId, _tp2Price) : GetPrice(Tp3LineId, _tp3Price));

        /// <summary>Цена TP1 (ближайший к Entry).</summary>
        public double TakeProfitPrice1 => GetPrice(Tp1LineId, _tp1Price);
        /// <summary>Цена TP2.</summary>
        public double TakeProfitPrice2 => GetPrice(Tp2LineId, _tp2Price);
        /// <summary>Цена TP3 (при 3 тейках).</summary>
        public double TakeProfitPrice3 => GetPrice(Tp3LineId, _tp3Price);

        /// <summary>Существует ли линия Entry на графике.</summary>
        public bool HasEntryLine => _bot.Chart.FindObject(EntryLineId) != null;

        /// <summary>Существует ли хотя бы одна наша линия.</summary>
        public bool HasAnyLines => _bot.Chart.FindObject(SlLineId) != null || _bot.Chart.FindObject(EntryLineId) != null;

        #endregion

        #region Public methods

        /// <summary>
        /// Показать линии для Limit-режима: Entry, SL, и 1, 2 или 3 TP.
        /// tpCount: 1, 2 или 3. При 2–3 основной TP (дальний) из расчёта; остальные на равных расстояниях.
        /// </summary>
        public void ShowLimitLines(int tpCount)
        {
            _tpCount = tpCount < 1 ? 1 : (tpCount > 3 ? 3 : tpCount);
            RemoveAllLines();
            CalculateInitialPrices(out _entryPrice, out _slPrice, out double mainTpPrice);

            double tp1, tp2, tp3;
            if (_tpCount == 1)
            {
                _tp1Price = mainTpPrice;
                _tp2Price = 0;
                _tp3Price = 0;
            }
            else if (_tpCount == 2)
            {
                tp2 = mainTpPrice;
                tp1 = (_entryPrice + tp2) / 2.0;
                _tp1Price = tp1;
                _tp2Price = tp2;
                _tp3Price = 0;
            }
            else
            {
                tp3 = mainTpPrice;
                double step = (tp3 - _entryPrice) / 3.0;
                tp1 = _entryPrice + step;
                tp2 = _entryPrice + step * 2.0;
                _tp1Price = tp1;
                _tp2Price = tp2;
                _tp3Price = tp3;
            }

            // Entry — синяя
            DrawLine(EntryLineId, _entryPrice, PanelStyles.LineEntry);
            DrawLineText(EntryTextId, _entryPrice, PanelStyles.LineEntry, Localization.Get("LimitText", "0.00"));

            // Stop Loss — красная
            DrawLine(SlLineId, _slPrice, PanelStyles.LineStopLoss);
            DrawLineText(SlTextId, _slPrice, PanelStyles.LineStopLoss, Localization.Get("StopText", "0.00"));

            // Take Profit — зелёные (1, 2 или 3)
            DrawLine(Tp1LineId, _tp1Price, PanelStyles.LineTakeProfit);
            DrawLineText(Tp1TextId, _tp1Price, PanelStyles.LineTakeProfit, Localization.Get("TpText", "0.0"));
            if (_tpCount >= 2)
            {
                DrawLine(Tp2LineId, _tp2Price, PanelStyles.LineTakeProfit);
                DrawLineText(Tp2TextId, _tp2Price, PanelStyles.LineTakeProfit, Localization.Get("TpText", "0.0"));
            }
            if (_tpCount == 3)
            {
                DrawLine(Tp3LineId, _tp3Price, PanelStyles.LineTakeProfit);
                DrawLineText(Tp3TextId, _tp3Price, PanelStyles.LineTakeProfit, Localization.Get("TpText", "0.0"));
            }
        }

        /// <summary>
        /// Показать линии для Market-режима: Entry (текущая цена) + SL + 1, 2 или 3 TP.
        /// </summary>
        public void ShowMarketLines(int tpCount)
        {
            _tpCount = tpCount < 1 ? 1 : (tpCount > 3 ? 3 : tpCount);
            RemoveAllLines();
            CalculateInitialPrices(out _entryPrice, out _slPrice, out double mainTpPrice);

            _entryPrice = _bot.Symbol.Bid;
            if (_tpCount == 1)
            {
                _tp1Price = mainTpPrice;
                _tp2Price = 0;
                _tp3Price = 0;
            }
            else if (_tpCount == 2)
            {
                _tp2Price = mainTpPrice;
                _tp1Price = (_entryPrice + _tp2Price) / 2.0;
                _tp3Price = 0;
            }
            else
            {
                _tp3Price = mainTpPrice;
                double step = (_tp3Price - _entryPrice) / 3.0;
                _tp1Price = _entryPrice + step;
                _tp2Price = _entryPrice + step * 2.0;
            }

            DrawLine(EntryLineId, _entryPrice, PanelStyles.LineEntry, interactive: false);
            DrawLineText(EntryTextId, _entryPrice, PanelStyles.LineEntry, Localization.Get("MarketText", "0.00"));

            DrawLine(SlLineId, _slPrice, PanelStyles.LineStopLoss);
            DrawLineText(SlTextId, _slPrice, PanelStyles.LineStopLoss, Localization.Get("StopText", "0.00"));

            DrawLine(Tp1LineId, _tp1Price, PanelStyles.LineTakeProfit);
            DrawLineText(Tp1TextId, _tp1Price, PanelStyles.LineTakeProfit, Localization.Get("TpText", "0.0"));
            if (_tpCount >= 2)
            {
                DrawLine(Tp2LineId, _tp2Price, PanelStyles.LineTakeProfit);
                DrawLineText(Tp2TextId, _tp2Price, PanelStyles.LineTakeProfit, Localization.Get("TpText", "0.0"));
            }
            if (_tpCount == 3)
            {
                DrawLine(Tp3LineId, _tp3Price, PanelStyles.LineTakeProfit);
                DrawLineText(Tp3TextId, _tp3Price, PanelStyles.LineTakeProfit, Localization.Get("TpText", "0.0"));
            }
        }

        /// <summary>
        /// Обновить позицию линии Market Entry (вызывается из OnTick).
        /// Перемещает линию и текст на текущую цену.
        /// </summary>
        public void UpdateMarketEntryLine(double price, string text)
        {
            var line = _bot.Chart.FindObject(EntryLineId) as ChartHorizontalLine;
            if (line != null)
            {
                line.Y = price;
                _entryPrice = price;
            }

            UpdateLineTextPosition(EntryTextId, price, text);
        }

        /// <summary>
        /// Удалить все виртуальные линии и тексты с графика.
        /// </summary>
        public void RemoveAllLines()
        {
            _bot.Chart.RemoveObject(EntryLineId);
            _bot.Chart.RemoveObject(EntryTextId);
            _bot.Chart.RemoveObject(SlLineId);
            _bot.Chart.RemoveObject(SlTextId);
            _bot.Chart.RemoveObject(Tp1LineId);
            _bot.Chart.RemoveObject(Tp1TextId);
            _bot.Chart.RemoveObject(Tp2LineId);
            _bot.Chart.RemoveObject(Tp2TextId);
            _bot.Chart.RemoveObject(Tp3LineId);
            _bot.Chart.RemoveObject(Tp3TextId);
        }

        /// <summary>
        /// Обновить текст подписи конкретной линии.
        /// </summary>
        /// <param name="textId">ID текстового объекта (например "cop_entry_text")</param>
        /// <param name="text">Новый текст</param>
        public void UpdateLineText(string textId, string text)
        {
            var textObj = _bot.Chart.FindObject(textId) as ChartText;
            if (textObj != null)
            {
                textObj.Text = text;
            }
        }

        /// <summary>
        /// Обновить текст и позицию (Y) подписи линии, чтобы текст следовал за линией.
        /// </summary>
        public void UpdateLineTextPosition(string textId, double price, string text)
        {
            // Проще всего — удалить и перерисовать текст
            var textObj = _bot.Chart.FindObject(textId) as ChartText;
            if (textObj != null)
            {
                Color color = textObj.Color;
                _bot.Chart.RemoveObject(textId);
                DrawLineText(textId, price, color, text);
            }
        }

        /// <summary>
        /// Получить текущую цену (Y) линии по ID.
        /// Возвращает fallback если линия не найдена.
        /// </summary>
        public double GetPrice(string lineId, double fallback = 0)
        {
            var line = _bot.Chart.FindObject(lineId) as ChartHorizontalLine;
            return line != null ? line.Y : fallback;
        }

        /// <summary>
        /// Программно переместить линию на заданную цену (для ввода из полей панели).
        /// Обновляет кэш, чтобы Chart_ObjectsUpdated не считал это пользовательским перетаскиванием.
        /// </summary>
        public void MoveLineTo(string lineId, double price)
        {
            var line = _bot.Chart.FindObject(lineId) as ChartHorizontalLine;
            if (line != null)
            {
                line.Y = price;

                if (lineId == EntryLineId) _entryPrice = price;
                else if (lineId == SlLineId) _slPrice = price;
                else if (lineId == Tp1LineId) _tp1Price = price;
                else if (lineId == Tp2LineId) _tp2Price = price;
                else if (lineId == Tp3LineId) _tp3Price = price;
            }
        }

        /// <summary>
        /// Отписаться от событий графика. Вызывать при остановке бота.
        /// </summary>
        public void Detach()
        {
            _bot.Chart.ObjectsUpdated -= Chart_ObjectsUpdated;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Рассчитать начальные цены линий по видимой области графика.
        /// Entry=50%, SL=40%, TP=75% высоты.
        /// </summary>
        private void CalculateInitialPrices(out double entry, out double sl, out double tp)
        {
            double highPrice = _bot.Chart.TopY;
            double lowPrice  = _bot.Chart.BottomY;
            double range     = highPrice - lowPrice;

            // Защита: если диапазон нулевой или невалидный, использовать текущую цену
            if (range <= 0 || double.IsNaN(range) || double.IsInfinity(range))
            {
                double bid = _bot.Symbol.Bid;
                double offset = bid * 0.005; // 0.5% от цены
                entry = bid;
                sl    = bid - offset;
                tp    = bid + offset;
                return;
            }

            entry = lowPrice + range * 0.50;
            sl    = lowPrice + range * 0.40;
            tp    = lowPrice + range * 0.75;
        }

        /// <summary>
        /// Нарисовать горизонтальную линию.
        /// </summary>
        /// <param name="interactive">true = перетаскиваемая (Limit Entry, SL, TP), false = только отображение (Market Entry)</param>
        private void DrawLine(string id, double price, Color color, bool interactive = true)
        {
            var line = _bot.Chart.DrawHorizontalLine(id, price, color, PanelStyles.LineThickness, LineStyle.Solid);
            line.IsInteractive = interactive;
        }

        /// <summary>
        /// Нарисовать текст рядом с линией.
        /// Текст размещается на 5 свечей правее последней видимой свечи.
        /// </summary>
        private void DrawLineText(string textId, double price, Color color, string text)
        {
            int barIndex = _bot.Chart.LastVisibleBarIndex + 5;
            // Защита: если график ещё не загружен
            if (barIndex < 0)
                barIndex = 0;

            _bot.Chart.DrawText(textId, text, barIndex, price, color);
        }

        /// <summary>
        /// Обработчик перетаскивания объектов на графике.
        /// Проверяет, изменились ли наши линии, и уведомляет подписчиков.
        /// </summary>
        private void Chart_ObjectsUpdated(ChartObjectsUpdatedEventArgs args)
        {
            bool changed = false;

            // Проверяем Entry
            var entryLine = _bot.Chart.FindObject(EntryLineId) as ChartHorizontalLine;
            if (entryLine != null && Math.Abs(entryLine.Y - _entryPrice) > _bot.Symbol.PipSize * 0.01)
            {
                _entryPrice = entryLine.Y;
                changed = true;
            }

            // Проверяем SL
            var slLine = _bot.Chart.FindObject(SlLineId) as ChartHorizontalLine;
            if (slLine != null && Math.Abs(slLine.Y - _slPrice) > _bot.Symbol.PipSize * 0.01)
            {
                _slPrice = slLine.Y;
                changed = true;
            }

            // Проверяем TP1, TP2, TP3
            var tp1Line = _bot.Chart.FindObject(Tp1LineId) as ChartHorizontalLine;
            if (tp1Line != null && Math.Abs(tp1Line.Y - _tp1Price) > _bot.Symbol.PipSize * 0.01) { _tp1Price = tp1Line.Y; changed = true; }
            var tp2Line = _bot.Chart.FindObject(Tp2LineId) as ChartHorizontalLine;
            if (tp2Line != null && Math.Abs(tp2Line.Y - _tp2Price) > _bot.Symbol.PipSize * 0.01) { _tp2Price = tp2Line.Y; changed = true; }
            var tp3Line = _bot.Chart.FindObject(Tp3LineId) as ChartHorizontalLine;
            if (tp3Line != null && Math.Abs(tp3Line.Y - _tp3Price) > _bot.Symbol.PipSize * 0.01) { _tp3Price = tp3Line.Y; changed = true; }

            if (changed)
            {
                // НЕ вызываем SyncTextPositions — позиции текстов обновляются
                // в RecalculateAll() через UpdateLineTextPosition().
                // Два метода с разной логикой позиционирования вызывали "прыжки" текста.
                OnLinesChanged?.Invoke();
            }
        }

        // SyncTextPositions удалён: вызывал конфликт с UpdateLineTextPosition().
        // Позиции текстов обновляются единообразно через RecalculateAll() → UpdateLineTextPosition().

        #endregion
    }
}
