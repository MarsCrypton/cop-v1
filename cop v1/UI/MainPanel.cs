using System;
using System.Globalization;
using cAlgo.API;

namespace COP_v1.UI
{
    /// <summary>
    /// Главная UI-панель бота COP v1.
    /// Содержит все контролы: чекбоксы, кнопки режимов, поля ввода, кнопку подтверждения.
    /// Поддерживает сворачивание/разворачивание.
    /// </summary>
    public class MainPanel
    {
        private readonly Robot _bot;

        // === Корневой контейнер ===
        private readonly Border _rootBorder;
        private readonly StackPanel _mainStack;

        // === Заголовок ===
        private readonly TextBlock _titleText;
        private readonly Button _toggleButton;

        // === Контент (скрывается при сворачивании) ===
        private readonly StackPanel _contentStack;

        // === Чекбоксы ===
        private readonly CheckBox _fastOrderCheckBox;
        private readonly CheckBox _spreadCheckBox;
        private readonly TextBlock _spreadValueText;

        // === Кнопки режимов ===
        private readonly Button _limitButton;
        private readonly Button _marketButton;

        // === Блок риска ===
        private readonly TextBlock _riskLabel;
        private readonly TextBox _riskTextBox;

        // === Блок цены входа ===
        private readonly TextBlock _priceLabel;
        private readonly TextBox _priceTextBox;

        // === Блок SL / TP ===
        private readonly TextBlock _slLabel;
        private readonly TextBox _slTextBox;
        private readonly TextBlock _slInfoText;

        private readonly TextBlock _tpLabel;
        private readonly TextBox _tpTextBox;
        private readonly TextBlock _tpInfoText;

        // === Кнопка подтверждения ===
        private readonly Button _submitButton;

        // === Состояние ===
        /// <summary>По умолчанию панель свёрнута.</summary>
        private bool _isCollapsed = true;

        /// <summary>
        /// Сохранённое состояние свёрнутости между перезапусками робота (смена таймфрейма).
        /// true = свёрнуто (по умолчанию).
        /// </summary>
        private static bool s_savedCollapsedState = true;

        /// <summary>
        /// Флаг: true когда поля обновляются программно (из RecalculateAll / перетаскивание линий).
        /// Предотвращает бесконечный цикл: код → поле → событие → линия → код.
        /// </summary>
        private bool _isUpdatingFromCode = false;

        // Hover-эффекты реализованы через Style + ControlState.Hover (а не MouseEnter/MouseLeave)

        // === События (для подключения логики в COP.cs) ===

        /// <summary>Вызывается при нажатии кнопки Limit. Аргумент: true если кнопка стала активной, false если деактивирована.</summary>
        public event Action<bool> OnLimitClicked;

        /// <summary>Вызывается при нажатии кнопки Market. Аргумент: true если кнопка стала активной, false если деактивирована.</summary>
        public event Action<bool> OnMarketClicked;

        /// <summary>Вызывается при нажатии кнопки подтверждения ордера.</summary>
        public event Action OnSubmitClicked;

        /// <summary>Вызывается при изменении значения риска в поле ввода. Аргумент: новый текст.</summary>
        public event Action<string> OnRiskChanged;

        /// <summary>Вызывается при изменении значения цены Entry. Аргумент: новый текст.</summary>
        public event Action<string> OnPriceChanged;

        /// <summary>Вызывается при изменении значения SL. Аргумент: новый текст.</summary>
        public event Action<string> OnSlChanged;

        /// <summary>Вызывается при изменении значения TP. Аргумент: новый текст.</summary>
        public event Action<string> OnTpChanged;

        /// <summary>Вызывается при переключении чекбокса Fast Order. Аргумент: новое состояние.</summary>
        public event Action<bool> OnFastOrderToggled;

        /// <summary>Вызывается при переключении чекбокса Spread. Аргумент: новое состояние.</summary>
        public event Action<bool> OnSpreadToggled;

        /// <summary>
        /// Создать панель COP v1.
        /// </summary>
        public MainPanel(Robot bot, VerticalPosition vPos, HorizontalPosition hPos, double maxRiskPercent, bool fastOrderMode)
        {
            _bot = bot;

            // ===== Заголовок =====
            _titleText = new TextBlock
            {
                Text = Localization.Get("PanelTitle"),
                ForegroundColor = PanelStyles.TextColor,
                FontSize = PanelStyles.FontSizeNormal,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 2, 0, 2)
            };

            _toggleButton = new Button
            {
                Text = "▲",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 4, 2),
                Width = 28,
                Height = 22,
                Style = PanelStyles.CreateToggleButtonStyle()
            };
            _toggleButton.Click += ToggleButton_Click;

            var headerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                BackgroundColor = PanelStyles.SeparatorColor,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            headerStack.AddChild(_titleText);
            headerStack.AddChild(CreateHSpacer());
            headerStack.AddChild(_toggleButton);

            // ===== Чекбоксы =====
            _fastOrderCheckBox = new CheckBox
            {
                IsChecked = fastOrderMode,
                ForegroundColor = PanelStyles.TextColor,
                Margin = new Thickness(4, 4, 8, 4)
            };
            _fastOrderCheckBox.Click += FastOrderCheckBox_Click;

            var fastOrderLabel = new TextBlock
            {
                Text = Localization.Get("FastOrder"),
                ForegroundColor = PanelStyles.TextColor,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            _spreadCheckBox = new CheckBox
            {
                IsChecked = true,
                ForegroundColor = PanelStyles.TextColor,
                Margin = new Thickness(4, 4, 4, 4)
            };
            _spreadCheckBox.Click += SpreadCheckBox_Click;

            var spreadLabel = new TextBlock
            {
                Text = Localization.Get("Spread"),
                ForegroundColor = PanelStyles.TextColor,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 2, 0)
            };

            _spreadValueText = new TextBlock
            {
                Text = "",
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center
            };

            var checkboxRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(2, 2, 2, 2)
            };
            checkboxRow.AddChild(_fastOrderCheckBox);
            checkboxRow.AddChild(fastOrderLabel);
            checkboxRow.AddChild(_spreadCheckBox);
            checkboxRow.AddChild(spreadLabel);
            checkboxRow.AddChild(_spreadValueText);

            // ===== Кнопки режимов =====
            double halfWidth = (PanelStyles.PanelWidth - 16) / 2;

            _limitButton = new Button
            {
                Text = Localization.Get("Limit"),
                Width = halfWidth,
                Height = 32
            };
            PanelStyles.ApplyModeButtonStyle(_limitButton, false);
            _limitButton.Click += LimitButton_Click;

            _marketButton = new Button
            {
                Text = Localization.Get("Market"),
                Width = halfWidth,
                Height = 32
            };
            PanelStyles.ApplyModeButtonStyle(_marketButton, false);
            _marketButton.Click += MarketButton_Click;

            var modeRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(2, 4, 2, 4)
            };
            modeRow.AddChild(_limitButton);
            modeRow.AddChild(_marketButton);

            // ===== Разделитель =====
            var sep1 = CreateSeparator();

            // ===== Блок риска =====
            _riskLabel = new TextBlock { Text = Localization.Get("MaxRisk") };
            PanelStyles.ApplyLabelStyle(_riskLabel);

            _riskTextBox = new TextBox
            {
                Text = maxRiskPercent.ToString("F2"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(4, 2, 4, 2)
            };
            PanelStyles.ApplyTextBoxStyle(_riskTextBox);
            _riskTextBox.TextChanged += RiskTextBox_TextChanged;

            // ===== Блок цены входа =====
            _priceLabel = new TextBlock { Text = Localization.Get("LimitOrder") };
            PanelStyles.ApplyLabelStyle(_priceLabel);

            _priceTextBox = new TextBox
            {
                Text = "",
                IsReadOnly = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(4, 2, 4, 2)
            };
            PanelStyles.ApplyTextBoxStyle(_priceTextBox);
            _priceTextBox.TextChanged += PriceTextBox_TextChanged;

            var sep2 = CreateSeparator();

            // ===== Блок SL / TP =====
            double colWidth = (PanelStyles.PanelWidth - 16) / 2;

            // -- SL --
            _slLabel = new TextBlock { Text = Localization.Get("StopLoss") };
            PanelStyles.ApplyLabelStyle(_slLabel);

            _slTextBox = new TextBox
            {
                Text = "",
                IsReadOnly = true,
                Width = colWidth - 4,
                Margin = new Thickness(2)
            };
            PanelStyles.ApplyTextBoxStyle(_slTextBox);
            _slTextBox.TextChanged += SlTextBox_TextChanged;

            _slInfoText = new TextBlock { Text = "0.00$ (0.00%)" };
            PanelStyles.ApplyValueStyle(_slInfoText);

            var slColumn = new StackPanel { Orientation = Orientation.Vertical, Width = colWidth };
            slColumn.AddChild(_slLabel);
            slColumn.AddChild(_slTextBox);
            slColumn.AddChild(_slInfoText);

            // -- TP --
            _tpLabel = new TextBlock { Text = Localization.Get("TakeProfit") };
            PanelStyles.ApplyLabelStyle(_tpLabel);

            _tpTextBox = new TextBox
            {
                Text = "",
                IsReadOnly = true,
                Width = colWidth - 4,
                Margin = new Thickness(2)
            };
            PanelStyles.ApplyTextBoxStyle(_tpTextBox);
            _tpTextBox.TextChanged += TpTextBox_TextChanged;

            _tpInfoText = new TextBlock { Text = "0.00$ (0.00%)" };
            PanelStyles.ApplyValueStyle(_tpInfoText);

            var tpColumn = new StackPanel { Orientation = Orientation.Vertical, Width = colWidth };
            tpColumn.AddChild(_tpLabel);
            tpColumn.AddChild(_tpTextBox);
            tpColumn.AddChild(_tpInfoText);

            var slTpRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(2, 2, 2, 2)
            };
            slTpRow.AddChild(slColumn);
            slTpRow.AddChild(tpColumn);

            var sep3 = CreateSeparator();

            // ===== Кнопка подтверждения =====
            _submitButton = new Button
            {
                Text = Localization.Get("PlaceOrder"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 36,
                Margin = new Thickness(4, 4, 4, 6)
            };
            PanelStyles.ApplySubmitButtonStyle(_submitButton, 0); // серая, неактивная
            _submitButton.Click += SubmitButton_Click;

            // ===== Собираем контент (всё кроме заголовка) =====
            _contentStack = new StackPanel { Orientation = Orientation.Vertical };
            _contentStack.AddChild(checkboxRow);
            _contentStack.AddChild(modeRow);
            _contentStack.AddChild(sep1);
            _contentStack.AddChild(_riskLabel);
            _contentStack.AddChild(_riskTextBox);
            _contentStack.AddChild(sep2);
            _contentStack.AddChild(_priceLabel);
            _contentStack.AddChild(_priceTextBox);
            _contentStack.AddChild(sep3);
            _contentStack.AddChild(slTpRow);
            _contentStack.AddChild(CreateSeparator());
            _contentStack.AddChild(_submitButton);

            // ===== Собираем всё в основной стек =====
            _mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                BackgroundColor = PanelStyles.PanelBackground
            };
            _mainStack.AddChild(headerStack);
            _mainStack.AddChild(_contentStack);

            // ===== Корневая рамка =====
            _rootBorder = new Border
            {
                Child = _mainStack,
                BackgroundColor = PanelStyles.PanelBackground,
                BorderColor = PanelStyles.SeparatorColor,
                BorderThickness = new Thickness(1),
                CornerRadius = PanelStyles.CornerRadius,
                Width = PanelStyles.PanelWidth,
                VerticalAlignment = MapVertical(vPos),
                HorizontalAlignment = MapHorizontal(hPos),
                Margin = new Thickness(8)
            };

            // Начальное состояние: по умолчанию свёрнуто; при перезапуске — по сохранённому
            if (s_savedCollapsedState)
            {
                _isCollapsed = true;
                _contentStack.IsVisible = false;
                _toggleButton.Text = "▼";
            }
            else
            {
                _isCollapsed = false;
                _contentStack.IsVisible = true;
                _toggleButton.Text = "▲";
            }
        }

        #region Public properties

        /// <summary>
        /// Корневой контрол для добавления на график через Chart.AddControl().
        /// </summary>
        public Border RootControl => _rootBorder;

        /// <summary>Текущее состояние чекбокса Fast Order.</summary>
        public bool IsFastOrder => _fastOrderCheckBox.IsChecked == true;

        /// <summary>Текущее состояние чекбокса Spread.</summary>
        public bool IsSpreadVisible => _spreadCheckBox.IsChecked == true;

        /// <summary>Текущее значение риска из поля ввода (как строка).</summary>
        public string RiskText => _riskTextBox.Text;

        /// <summary>Установить значение риска в поле (без вызова OnRiskChanged).</summary>
        public void SetRiskText(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            _isUpdatingFromCode = true;
            _riskTextBox.Text = value;
            _isUpdatingFromCode = false;
        }

        /// <summary>Кнопка Limit активна (зелёная)?</summary>
        public bool IsLimitActive { get; private set; }

        /// <summary>Кнопка Market активна (зелёная)?</summary>
        public bool IsMarketActive { get; private set; }

        #endregion

        #region Public methods — обновление данных

        /// <summary>
        /// Обновить отображение спреда.
        /// </summary>
        public void UpdateSpread(double spreadPips)
        {
            if (_spreadCheckBox.IsChecked == true)
                _spreadValueText.Text = spreadPips.ToString("F1");
            else
                _spreadValueText.Text = "";
        }

        /// <summary>
        /// Обновить цену входа (для Limit-режима — от линии, для Market — от тика).
        /// Программное обновление — не вызывает OnPriceChanged.
        /// </summary>
        public void UpdateEntryPrice(double price, int digits)
        {
            _isUpdatingFromCode = true;
            _priceTextBox.Text = price.ToString("F" + digits);
            _priceTextBox.BackgroundColor = PanelStyles.InputBackground; // сбросить подсветку ошибки
            _isUpdatingFromCode = false;
        }

        /// <summary>
        /// Обновить поле Market-цены (bid/ask).
        /// Программное обновление — не вызывает OnPriceChanged.
        /// </summary>
        public void UpdateMarketPrice(double bid, double ask, int digits)
        {
            _isUpdatingFromCode = true;
            _priceTextBox.Text = bid.ToString("F" + digits);
            _isUpdatingFromCode = false;
        }

        /// <summary>
        /// Обновить данные Stop Loss на панели.
        /// Программное обновление — не вызывает OnSlChanged.
        /// </summary>
        public void UpdateStopLoss(double price, int digits, double lossDollars, double lossPercent)
        {
            _isUpdatingFromCode = true;
            _slTextBox.Text = price.ToString("F" + digits);
            _slTextBox.BackgroundColor = PanelStyles.InputBackground;
            _slInfoText.Text = string.Format("{0:F2}$ ({1:F2}%)", lossDollars, lossPercent);
            _isUpdatingFromCode = false;
        }

        /// <summary>
        /// Обновить данные Take Profit на панели.
        /// Программное обновление — не вызывает OnTpChanged.
        /// </summary>
        public void UpdateTakeProfit(double price, int digits, double profitDollars, double profitPercent)
        {
            _isUpdatingFromCode = true;
            _tpTextBox.Text = price.ToString("F" + digits);
            _tpTextBox.BackgroundColor = PanelStyles.InputBackground;
            _tpInfoText.Text = string.Format("{0:F2}$ ({1:F2}%)", profitDollars, profitPercent);
            _isUpdatingFromCode = false;
        }

        /// <summary>
        /// Обновить кнопку подтверждения: текст, цвет, активность.
        /// direction: 1=Long, -1=Short, 0=Invalid
        /// </summary>
        public void UpdateSubmitButton(int direction, bool isLimit, string symbolName, string volumeLots)
        {
            if (_fastOrderCheckBox.IsChecked == true)
            {
                // Fast Order — кнопка всегда серая/неактивна
                PanelStyles.ApplySubmitButtonStyle(_submitButton, 0);
                _submitButton.Text = Localization.Get("PlaceOrder");
                return;
            }

            if (direction == 0)
            {
                // Invalid — красная
                PanelStyles.ApplySubmitButtonStyle(_submitButton, -1);
                _submitButton.Text = Localization.Get("InvalidLevels");
            }
            else if (direction == 1)
            {
                // Long — зелёная
                PanelStyles.ApplySubmitButtonStyle(_submitButton, 1);
                _submitButton.Text = isLimit
                    ? Localization.Get("LimitLong", symbolName, volumeLots)
                    : Localization.Get("BuyMarket", symbolName, volumeLots);
            }
            else
            {
                // Short — зелёная
                PanelStyles.ApplySubmitButtonStyle(_submitButton, 1);
                _submitButton.Text = isLimit
                    ? Localization.Get("LimitShort", symbolName, volumeLots)
                    : Localization.Get("SellMarket", symbolName, volumeLots);
            }
        }

        /// <summary>
        /// Переключить заголовок блока цены: "Limit Order" или "Market Order".
        /// Управляет readonly для поля цены входа.
        /// </summary>
        public void SetMode(bool isLimit)
        {
            _priceLabel.Text = isLimit
                ? Localization.Get("LimitOrder")
                : Localization.Get("MarketOrder");

            // В Market-режиме цена всегда readonly.
            // В Limit-режиме — readonly только если Fast Order.
            if (!isLimit)
                _priceTextBox.IsReadOnly = true;
            else
                _priceTextBox.IsReadOnly = (_fastOrderCheckBox.IsChecked == true);
        }

        /// <summary>
        /// Сделать поля SL/TP и Price доступными для ручного ввода.
        /// readOnly=true — Fast Order или IDLE: все поля ReadOnly.
        /// readOnly=false — обычный режим: SL/TP редактируемы, Price зависит от SetMode.
        /// </summary>
        public void SetFieldsReadOnly(bool readOnly)
        {
            _slTextBox.IsReadOnly = readOnly;
            _tpTextBox.IsReadOnly = readOnly;

            if (readOnly)
            {
                // Fast Order: цена тоже readonly
                _priceTextBox.IsReadOnly = true;
            }
            // else: price readonly управляется через SetMode
        }

        /// <summary>
        /// Сбросить панель в исходное состояние (IDLE).
        /// </summary>
        public void ResetToIdle()
        {
            _isUpdatingFromCode = true;

            IsLimitActive = false;
            IsMarketActive = false;
            PanelStyles.ApplyModeButtonStyle(_limitButton, false);
            PanelStyles.ApplyModeButtonStyle(_marketButton, false);
            PanelStyles.ApplySubmitButtonStyle(_submitButton, 0);
            _submitButton.Text = Localization.Get("PlaceOrder");
            _priceTextBox.Text = "";
            _slTextBox.Text = "";
            _tpTextBox.Text = "";
            _slInfoText.Text = "0.00$ (0.00%)";
            _tpInfoText.Text = "0.00$ (0.00%)";
            _priceLabel.Text = Localization.Get("LimitOrder");
            _priceTextBox.IsReadOnly = true;
            _slTextBox.IsReadOnly = true;
            _tpTextBox.IsReadOnly = true;

            // Сбросить подсветку ошибок
            _priceTextBox.BackgroundColor = PanelStyles.InputBackground;
            _slTextBox.BackgroundColor = PanelStyles.InputBackground;
            _tpTextBox.BackgroundColor = PanelStyles.InputBackground;

            _isUpdatingFromCode = false;
        }

        /// <summary>
        /// Программно свернуть панель.
        /// </summary>
        public void Collapse()
        {
            _isCollapsed = true;
            s_savedCollapsedState = true;
            _contentStack.IsVisible = false;
            _toggleButton.Text = "▼";
        }

        /// <summary>
        /// Программно развернуть панель.
        /// </summary>
        public void Expand()
        {
            _isCollapsed = false;
            s_savedCollapsedState = false;
            _contentStack.IsVisible = true;
            _toggleButton.Text = "▲";
        }

        #endregion

        #region Private — обработчики событий

        private void ToggleButton_Click(ButtonClickEventArgs args)
        {
            if (_isCollapsed)
                Expand();
            else
                Collapse();
        }

        private void FastOrderCheckBox_Click(CheckBoxEventArgs args)
        {
            bool isChecked = _fastOrderCheckBox.IsChecked == true;
            OnFastOrderToggled?.Invoke(isChecked);
        }

        private void SpreadCheckBox_Click(CheckBoxEventArgs args)
        {
            bool isChecked = _spreadCheckBox.IsChecked == true;
            if (!isChecked)
                _spreadValueText.Text = "";
            OnSpreadToggled?.Invoke(isChecked);
        }

        private void LimitButton_Click(ButtonClickEventArgs args)
        {
            if (IsLimitActive)
            {
                // Деактивируем Limit
                IsLimitActive = false;
                PanelStyles.ApplyModeButtonStyle(_limitButton, false);
                OnLimitClicked?.Invoke(false);
            }
            else
            {
                // Деактивируем Market (если был), активируем Limit
                if (IsMarketActive)
                {
                    IsMarketActive = false;
                    PanelStyles.ApplyModeButtonStyle(_marketButton, false);
                    OnMarketClicked?.Invoke(false);
                }

                IsLimitActive = true;
                PanelStyles.ApplyModeButtonStyle(_limitButton, true);
                OnLimitClicked?.Invoke(true);
            }
        }

        private void MarketButton_Click(ButtonClickEventArgs args)
        {
            if (IsMarketActive)
            {
                // Деактивируем Market
                IsMarketActive = false;
                PanelStyles.ApplyModeButtonStyle(_marketButton, false);
                OnMarketClicked?.Invoke(false);
            }
            else
            {
                // Деактивируем Limit (если был), активируем Market
                if (IsLimitActive)
                {
                    IsLimitActive = false;
                    PanelStyles.ApplyModeButtonStyle(_limitButton, false);
                    OnLimitClicked?.Invoke(false);
                }

                IsMarketActive = true;
                PanelStyles.ApplyModeButtonStyle(_marketButton, true);
                OnMarketClicked?.Invoke(true);
            }
        }

        private void SubmitButton_Click(ButtonClickEventArgs args)
        {
            if (_submitButton.IsEnabled)
                OnSubmitClicked?.Invoke();
        }

        private void RiskTextBox_TextChanged(TextChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            OnRiskChanged?.Invoke(_riskTextBox.Text);
        }

        private void PriceTextBox_TextChanged(TextChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;

            // Валидация: только если поле редактируемое (Limit-режим, не fast order)
            if (_priceTextBox.IsReadOnly) return;

            if (TryParsePrice(_priceTextBox.Text, out _))
            {
                _priceTextBox.BackgroundColor = PanelStyles.InputBackground;
                OnPriceChanged?.Invoke(_priceTextBox.Text);
            }
            else
            {
                _priceTextBox.BackgroundColor = PanelStyles.ButtonError;
            }
        }

        private void SlTextBox_TextChanged(TextChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            if (_slTextBox.IsReadOnly) return;

            if (TryParsePrice(_slTextBox.Text, out _))
            {
                _slTextBox.BackgroundColor = PanelStyles.InputBackground;
                OnSlChanged?.Invoke(_slTextBox.Text);
            }
            else
            {
                _slTextBox.BackgroundColor = PanelStyles.ButtonError;
            }
        }

        private void TpTextBox_TextChanged(TextChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            if (_tpTextBox.IsReadOnly) return;

            if (TryParsePrice(_tpTextBox.Text, out _))
            {
                _tpTextBox.BackgroundColor = PanelStyles.InputBackground;
                OnTpChanged?.Invoke(_tpTextBox.Text);
            }
            else
            {
                _tpTextBox.BackgroundColor = PanelStyles.ButtonError;
            }
        }

        #endregion

        #region Private — вспомогательные методы

        /// <summary>
        /// Попытаться распарсить цену из строки.
        /// Поддерживает точку и запятую как разделитель дробной части.
        /// </summary>
        private static bool TryParsePrice(string text, out double price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string cleaned = text.Replace(',', '.');
            return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out price) && price > 0;
        }

        /// <summary>Тонкий горизонтальный разделитель.</summary>
        private Border CreateSeparator()
        {
            return new Border
            {
                BackgroundColor = PanelStyles.SeparatorColor,
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        /// <summary>Горизонтальный спейсер, занимающий свободное место между элементами.</summary>
        private StackPanel CreateHSpacer()
        {
            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = PanelStyles.PanelWidth - 80
            };
        }

        /// <summary>Маппинг нашего enum VerticalPosition → cAlgo VerticalAlignment.</summary>
        private static VerticalAlignment MapVertical(VerticalPosition pos)
        {
            switch (pos)
            {
                case VerticalPosition.Top:    return VerticalAlignment.Top;
                case VerticalPosition.Center: return VerticalAlignment.Center;
                case VerticalPosition.Bottom: return VerticalAlignment.Bottom;
                default:                      return VerticalAlignment.Top;
            }
        }

        /// <summary>Маппинг нашего enum HorizontalPosition → cAlgo HorizontalAlignment.</summary>
        private static HorizontalAlignment MapHorizontal(HorizontalPosition pos)
        {
            switch (pos)
            {
                case HorizontalPosition.Left:   return HorizontalAlignment.Left;
                case HorizontalPosition.Center: return HorizontalAlignment.Center;
                case HorizontalPosition.Right:  return HorizontalAlignment.Right;
                default:                         return HorizontalAlignment.Right;
            }
        }

        #endregion
    }
}
