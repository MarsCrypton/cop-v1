using System;
using cAlgo.API;
using COP_v1.UI;
using COP_v1.Trading;

namespace COP_v1.Chart
{
    /// <summary>
    /// Обработчик режима Fast Order.
    /// Привязывает линии к курсору, фиксирует по клику, автоматически размещает ордер.
    ///
    /// Limit: 3 клика (Entry → SL → TP) → ордер.
    /// Market: 2 клика (SL → TP) → ордер.
    /// </summary>
    public class FastOrderHandler
    {
        private readonly Robot _bot;
        private readonly ChartLineManager _lineManager;
        private readonly RiskCalculator _riskCalculator;

        /// <summary>
        /// Callback когда все уровни зафиксированы.
        /// Аргументы: entryPrice, slPrice, tpPrice, isMarket.
        /// </summary>
        private readonly Action<double, double, double, bool> _onOrderReady;

        /// <summary>
        /// Callback для получения текущего % риска из панели.
        /// </summary>
        private readonly Func<double> _getRiskPercent;

        // Состояние
        // 0 = неактивен, 1 = выбор Entry, 2 = выбор SL, 3 = выбор TP
        private int _step = 0;
        private bool _isMarketMode;

        // Зафиксированные цены
        private double _entryPrice;
        private double _slPrice;
        private double _tpPrice;

        // ID текущей линии, привязанной к курсору
        private string _currentLineId;
        private string _currentTextId;
        private Color _currentColor;

        /// <summary>Активен ли процесс Fast Order.</summary>
        public bool IsActive => _step > 0;

        /// <summary>
        /// Создать обработчик Fast Order.
        /// </summary>
        /// <param name="bot">Робот (для доступа к Chart, Symbol)</param>
        /// <param name="lineManager">Менеджер линий (для RemoveAllLines)</param>
        /// <param name="riskCalculator">Калькулятор риска (для динамических текстов)</param>
        /// <param name="onOrderReady">Callback(entry, sl, tp, isMarket) — вызывается когда все уровни выбраны</param>
        /// <param name="getRiskPercent">Функция для получения текущего % риска</param>
        public FastOrderHandler(
            Robot bot,
            ChartLineManager lineManager,
            RiskCalculator riskCalculator,
            Action<double, double, double, bool> onOrderReady,
            Func<double> getRiskPercent)
        {
            _bot = bot;
            _lineManager = lineManager;
            _riskCalculator = riskCalculator;
            _onOrderReady = onOrderReady;
            _getRiskPercent = getRiskPercent;
        }

        #region Public methods

        /// <summary>
        /// Начать Fast Order в режиме Limit.
        /// Шаг 1: к курсору привязывается синяя линия Entry.
        /// </summary>
        public void StartLimit()
        {
            Cancel(); // на всякий случай очистить предыдущий

            _isMarketMode = false;
            _step = 1;
            _entryPrice = _bot.Symbol.Bid;

            // Создать синюю линию Entry, привязанную к курсору
            _currentLineId = ChartLineManager.EntryLineId;
            _currentTextId = ChartLineManager.EntryTextId;
            _currentColor = PanelStyles.LineEntry;

            DrawFastLine(_currentLineId, _entryPrice, _currentColor);
            DrawFastText(_currentTextId, _entryPrice, _currentColor, Localization.Get("LimitText", "0.00"));

            _bot.Chart.MouseMove += OnMouseMove;
            _bot.Chart.MouseDown += OnMouseDown;

            _bot.Print("FastOrder: StartLimit — step 1 (Entry)");
        }

        /// <summary>
        /// Начать Fast Order в режиме Market.
        /// Entry = текущая цена (синяя линия, неперемещаемая, показывает лоты).
        /// Шаг 2: к курсору привязывается красная линия SL.
        /// </summary>
        public void StartMarket()
        {
            Cancel();

            _isMarketMode = true;
            _entryPrice = _bot.Symbol.Bid;
            _step = 2; // пропускаем выбор Entry

            // Показать синюю линию Market Entry (неинтерактивная, отображает текущую цену и лоты)
            DrawFastLine(ChartLineManager.EntryLineId, _entryPrice, PanelStyles.LineEntry);
            DrawFastText(ChartLineManager.EntryTextId, _entryPrice, PanelStyles.LineEntry,
                Localization.Get("MarketText", "0.00"));

            // Создать красную линию SL, привязанную к курсору
            _currentLineId = ChartLineManager.SlLineId;
            _currentTextId = ChartLineManager.SlTextId;
            _currentColor = PanelStyles.LineStopLoss;

            DrawFastLine(_currentLineId, _entryPrice, _currentColor);
            DrawFastText(_currentTextId, _entryPrice, _currentColor, Localization.Get("StopText", "0.00"));

            _bot.Chart.MouseMove += OnMouseMove;
            _bot.Chart.MouseDown += OnMouseDown;

            _bot.Print("FastOrder: StartMarket — step 2 (SL), Entry line at {0}", _entryPrice.ToString("F" + _bot.Symbol.Digits));
        }

        /// <summary>
        /// Отменить текущий процесс Fast Order.
        /// Убирает все линии и отписывается от событий мыши.
        /// </summary>
        public void Cancel()
        {
            if (_step == 0)
                return;

            _bot.Chart.MouseMove -= OnMouseMove;
            _bot.Chart.MouseDown -= OnMouseDown;

            _lineManager.RemoveAllLines();
            _step = 0;

            _bot.Print("FastOrder: Cancelled");
        }

        /// <summary>
        /// Отписаться от всех событий. Вызывать при остановке бота.
        /// </summary>
        public void Detach()
        {
            if (_step > 0)
            {
                _bot.Chart.MouseMove -= OnMouseMove;
                _bot.Chart.MouseDown -= OnMouseDown;
            }
            _step = 0;
        }

        #endregion

        #region Mouse event handlers

        private void OnMouseMove(ChartMouseEventArgs args)
        {
            if (_step == 0)
                return;

            // Переместить текущую линию к позиции курсора
            var line = _bot.Chart.FindObject(_currentLineId) as ChartHorizontalLine;
            if (line != null)
            {
                line.Y = args.YValue;
            }

            // Обновить текст динамически
            UpdateDynamicText(args.YValue);
        }

        private void OnMouseDown(ChartMouseEventArgs args)
        {
            if (_step == 0)
                return;

            double clickPrice = args.YValue;

            // Зафиксировать текущую линию
            var line = _bot.Chart.FindObject(_currentLineId) as ChartHorizontalLine;
            if (line != null)
            {
                line.Y = clickPrice;
            }

            // Обновить текст на зафиксированной позиции
            UpdateDynamicText(clickPrice);

            // Переход к следующему шагу
            if (_step == 1)
            {
                // Entry зафиксирован → начинаем SL
                _entryPrice = clickPrice;
                _step = 2;

                _currentLineId = ChartLineManager.SlLineId;
                _currentTextId = ChartLineManager.SlTextId;
                _currentColor = PanelStyles.LineStopLoss;

                DrawFastLine(_currentLineId, clickPrice, _currentColor);
                DrawFastText(_currentTextId, clickPrice, _currentColor, Localization.Get("StopText", "0.00"));

                _bot.Print("FastOrder: Entry fixed at {0} — step 2 (SL)", _entryPrice.ToString("F" + _bot.Symbol.Digits));
            }
            else if (_step == 2)
            {
                // SL зафиксирован → начинаем TP
                _slPrice = clickPrice;
                _step = 3;

                _currentLineId = ChartLineManager.TpLineId;
                _currentTextId = ChartLineManager.TpTextId;
                _currentColor = PanelStyles.LineTakeProfit;

                DrawFastLine(_currentLineId, clickPrice, _currentColor);
                DrawFastText(_currentTextId, clickPrice, _currentColor, Localization.Get("TpText", "0.0"));

                _bot.Print("FastOrder: SL fixed at {0} — step 3 (TP)", _slPrice.ToString("F" + _bot.Symbol.Digits));
            }
            else if (_step == 3)
            {
                // TP зафиксирован → готово, размещаем ордер!
                _tpPrice = clickPrice;

                _bot.Print("FastOrder: TP fixed at {0} — placing order!", _tpPrice.ToString("F" + _bot.Symbol.Digits));

                // Отписаться от событий
                _bot.Chart.MouseMove -= OnMouseMove;
                _bot.Chart.MouseDown -= OnMouseDown;

                // Для Market: обновить entry на актуальную цену
                if (_isMarketMode)
                    _entryPrice = _bot.Symbol.Bid;

                int stepBackup = _step;
                _step = 0;

                // Вызвать callback — COP.cs разместит ордер
                _onOrderReady?.Invoke(_entryPrice, _slPrice, _tpPrice, _isMarketMode);
            }
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Обновить динамический текст на линии при движении курсора.
        /// </summary>
        private void UpdateDynamicText(double cursorPrice)
        {
            string text = "";

            if (_step == 1)
            {
                // Entry — показать предполагаемый объём
                double riskPercent = _getRiskPercent();
                // Примерный SL — 40% от видимой области ниже курсора
                double approxSl = cursorPrice - (_bot.Chart.TopY - _bot.Chart.BottomY) * 0.10;
                double vol = _riskCalculator.CalculateVolume(cursorPrice, approxSl, riskPercent);
                double lots = _riskCalculator.ToLots(vol);
                text = Localization.Get("LimitText", lots.ToString("F2"));
            }
            else if (_step == 2)
            {
                // SL — показать % убытка
                double entry = _isMarketMode ? _bot.Symbol.Bid : _entryPrice;
                double riskPercent = _getRiskPercent();
                double vol = _riskCalculator.CalculateVolume(entry, cursorPrice, riskPercent);
                double lots = _riskCalculator.ToLots(vol);
                double slDollars, slPercent;
                _riskCalculator.CalculateLoss(entry, cursorPrice, vol, out slDollars, out slPercent);
                text = Localization.Get("StopText", slPercent.ToString("F2"));

                // Обновить текст на Entry-линии с актуальным объёмом
                string entryTextKey = _isMarketMode ? "MarketText" : "LimitText";
                var entryTextObj = _bot.Chart.FindObject(ChartLineManager.EntryTextId) as ChartText;
                if (entryTextObj != null)
                {
                    entryTextObj.Text = Localization.Get(entryTextKey, lots.ToString("F2"));
                }

                // В Market-режиме обновить позицию Entry-линии на актуальную цену
                if (_isMarketMode)
                {
                    _entryPrice = _bot.Symbol.Bid;
                    var entryLine = _bot.Chart.FindObject(ChartLineManager.EntryLineId) as ChartHorizontalLine;
                    if (entryLine != null)
                        entryLine.Y = _entryPrice;
                    if (entryTextObj != null)
                        entryTextObj.Y = _entryPrice;
                }
            }
            else if (_step == 3)
            {
                // TP — показать RR
                double entry = _isMarketMode ? _bot.Symbol.Bid : _entryPrice;
                double rr = _riskCalculator.CalculateRR(entry, _slPrice, cursorPrice);
                text = Localization.Get("TpText", rr.ToString("F1"));
            }

            // Обновить текст
            var textObj = _bot.Chart.FindObject(_currentTextId) as ChartText;
            if (textObj != null)
            {
                textObj.Text = text;
                textObj.Y = cursorPrice;
            }
        }

        /// <summary>
        /// Нарисовать линию для Fast Order (НЕ интерактивная — двигается программно).
        /// </summary>
        private void DrawFastLine(string id, double price, Color color)
        {
            var line = _bot.Chart.DrawHorizontalLine(id, price, color, PanelStyles.LineThickness, LineStyle.Solid);
            line.IsInteractive = false; // двигаем программно через MouseMove
        }

        /// <summary>
        /// Нарисовать текст рядом с линией.
        /// </summary>
        private void DrawFastText(string textId, double price, Color color, string text)
        {
            int barIndex = _bot.Chart.LastVisibleBarIndex + 5;
            if (barIndex < 0) barIndex = 0;
            _bot.Chart.DrawText(textId, text, barIndex, price, color);
        }

        #endregion
    }
}
