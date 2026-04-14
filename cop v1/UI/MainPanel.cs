using System;
using System.Globalization;
using cAlgo.API;

namespace COP_v1.UI
{
    /// <summary>Режим распределения объёма между тейками: равный объём или равный профит.</summary>
    public enum TpVolumeMode
    {
        EqualVolume,
        EqualProfit
    }

    /// <summary>
    /// Главная UI-панель бота COP v1.
    /// Содержит все контролы: чекбоксы, кнопки режимов, поля ввода, кнопку подтверждения.
    /// Поддерживает сворачивание/разворачивание.
    /// </summary>
    public class MainPanel
    {
        private readonly Robot _bot;

        // === Корневой контейнер: один внешний Border (_rootWrapper) с рамкой; внутри без второй обводки ===
        private readonly Border _rootWrapper;
        private readonly StackPanel _rootContainer;
        private readonly StackPanel _mainPanelRoot;
        private readonly StackPanel _mainStack;

        // === Заголовок ===
        private readonly TextBlock _titleText;
        private readonly Button _settingsButton;
        private readonly Button _toggleButton;

        // === Панель настроек ===
        private readonly Border _settingsPanelBorder;
        private bool _settingsPanelVisible;
        private readonly ComboBox _tpCountCombo;
        private readonly ComboBox _tpVolumeModeCombo;
        private readonly ComboBox _transparencyCombo;
        private readonly ComboBox _scaleCombo;

        /// <summary>Вызывается при изменении прозрачности фона панели из настроек. Аргумент: новый процент (0–80).</summary>
        public event Action<int> OnTransparencyChanged;

        /// <summary>Вызывается при выборе масштаба панели в настройках (80–150 %).</summary>
        public event Action<int> OnScaleChanged;

        /// <summary>Текущий процент прозрачности фона панелей (0–80).</summary>
        private int _panelTransparencyPercent;

        // === Контент (скрывается при сворачивании) ===
        private readonly StackPanel _contentStack;

        // === Чекбоксы ===
        private readonly CheckBox _fastOrderCheckBox;
        private readonly TextBlock _spreadValueText;

        // === Кнопки режимов ===
        private readonly Button _limitButton;
        private readonly Button _marketButton;

        // === Блок риска (RiskMode + значение) ===
        private readonly TextBlock _riskLabel;
        private readonly ComboBox _riskModeCombo;
        private readonly TextBox _riskTextBox;
        private readonly TextBlock _riskUnitText;
        private readonly TextBlock _riskErrorText;

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

        // === Мини-панель (видна только при свёрнутой панели) ===
        /// <summary>Базовый размер шрифта мини-кнопок (9 px при 100 %) — через <see cref="PanelStyles.SF"/>.</summary>
        private static int MiniButtonFontSize => PanelStyles.SF(9);

        // Кнопка настроек: корень «Set»; + пока блок настроек закрыт, − когда открыт
        private const string HeaderSettingsLabelEn = "Set";
        private const string HeaderExpandLabelEn = "Full";
        private const string HeaderCollapseLabelEn = "Mini";
        private readonly StackPanel _miniPanelStack;
        private readonly Button _miniLimitButton;
        private readonly Button _miniMarketButton;
        private readonly Button _miniSubmitButton;
        private readonly Button _miniFoButton;

        /// <summary>Свёрнутый блок: ряд мини-кнопок, разделитель, футер.</summary>
        private readonly StackPanel _collapsedChromeStack;

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

        /// <summary>Вызывается при смене режима риска (Percent/USD/EUR).</summary>
        public event Action<RiskMode> OnRiskModeChanged;

        /// <summary>Вызывается при изменении значения цены Entry. Аргумент: новый текст.</summary>
        public event Action<string> OnPriceChanged;

        /// <summary>Вызывается при изменении значения SL. Аргумент: новый текст.</summary>
        public event Action<string> OnSlChanged;

        /// <summary>Вызывается при изменении значения TP. Аргумент: новый текст.</summary>
        public event Action<string> OnTpChanged;

        /// <summary>Вызывается при переключении чекбокса Fast Order. Аргумент: новое состояние.</summary>
        public event Action<bool> OnFastOrderToggled;

        /// <summary>Смена числа тейков или режима равный объём / равный профит в настройках.</summary>
        public event Action OnTpAllocationSettingsChanged;

        /// <summary>
        /// Создать панель COP v1.
        /// </summary>
        public MainPanel(Robot bot, VerticalPosition vPos, HorizontalPosition hPos, double maxRiskPercent, bool fastOrderMode, int panelTransparencyPercent = 0)
        {
            _bot = bot;
            _panelTransparencyPercent = Math.Max(0, Math.Min(panelTransparencyPercent, 80));
            Color panelBg = PanelStyles.GetPanelBackgroundWithTransparency(_panelTransparencyPercent);

            // ===== Заголовок: «COP v1» по центру зоны слева от кнопок + Set / Full =====
            _titleText = new TextBlock
            {
                Text = Localization.Get("PanelTitle"),
                ForegroundColor = PanelStyles.TextColor,
                FontSize = PanelStyles.FontSizeHeaderExpanded,
                FontWeight = FontWeight.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            _settingsButton = new Button
            {
                Text = "+ Set",
                FontSize = PanelStyles.HeaderBarButtonFontSize,
                FontWeight = FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(0, 1, 2, 1),
                Height = PanelStyles.HeaderBarButtonHeight,
                Style = PanelStyles.CreateToggleButtonStyle()
            };
            _settingsButton.Click += SettingsButton_Click;
            _settingsButton.CornerRadius = new CornerRadius(PanelStyles.ButtonCornerSubtle);

            _toggleButton = new Button
            {
                Text = HeaderExpandLabelEn,
                FontSize = PanelStyles.HeaderBarButtonFontSize,
                FontWeight = FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(0, 1, 4, 1),
                Height = PanelStyles.HeaderBarButtonHeight,
                Style = PanelStyles.CreateToggleButtonStyle()
            };
            _toggleButton.Click += ToggleButton_Click;
            _toggleButton.CornerRadius = new CornerRadius(PanelStyles.ButtonCornerSubtle);

            var titleCell = new Grid(1, 1)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleCell.Rows[0].SetHeightToAuto();
            titleCell.Columns[0].SetWidthInStars(1);
            titleCell.AddChild(_titleText, 0, 0);

            var headerButtonsGrid = new Grid(1, 2)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerButtonsGrid.Rows[0].SetHeightToAuto();
            headerButtonsGrid.Columns[0].SetWidthInStars(1);
            headerButtonsGrid.Columns[1].SetWidthInStars(1);
            headerButtonsGrid.AddChild(_settingsButton, 0, 0);
            headerButtonsGrid.AddChild(_toggleButton, 0, 1);

            var headerGrid = new Grid(1, 2)
            {
                BackgroundColor = PanelStyles.HeaderBarColor,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = PanelStyles.PanelWidth
            };
            headerGrid.Rows[0].SetHeightToAuto();
            // Зона заголовка (центрирование между левым краем и Set) : зона кнопок — ~1 : 2
            headerGrid.Columns[0].SetWidthInStars(1);
            headerGrid.Columns[1].SetWidthInStars(2);
            headerGrid.AddChild(titleCell, 0, 0);
            headerGrid.AddChild(headerButtonsGrid, 0, 1);

            // ===== Чекбоксы =====
            _fastOrderCheckBox = new CheckBox
            {
                IsChecked = fastOrderMode,
                ForegroundColor = PanelStyles.TextColor,
                Margin = PanelStyles.ST(4, 4, 8, 4)
            };
            _fastOrderCheckBox.Click += FastOrderCheckBox_Click;

            var fastOrderLabel = new TextBlock
            {
                Text = Localization.Get("FastOrder"),
                ForegroundColor = PanelStyles.TextColor,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(0, 0, 10, 0)
            };

            var spreadLabel = new TextBlock
            {
                Text = Localization.Get("Spread"),
                ForegroundColor = PanelStyles.TextColor,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(0, 0, 2, 0)
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
                Margin = PanelStyles.ST(2)
            };
            checkboxRow.AddChild(_fastOrderCheckBox);
            checkboxRow.AddChild(fastOrderLabel);
            checkboxRow.AddChild(spreadLabel);
            checkboxRow.AddChild(_spreadValueText);

            // ===== Кнопки режимов =====
            // Учитываем Margin(2) с каждой стороны у кнопок (ApplyModeButtonStyle) и поля ряда
            double modeRowSideMargin = PanelStyles.S(4);
            double modeInner = PanelStyles.PanelWidth - 2 * modeRowSideMargin;
            double halfWidth = (modeInner - PanelStyles.S(8)) / 2;

            _limitButton = new Button
            {
                Text = Localization.Get("Limit"),
                Width = halfWidth,
                Height = PanelStyles.S(32)
            };
            PanelStyles.ApplyModeButtonStyle(_limitButton, false);
            _limitButton.Click += LimitButton_Click;

            _marketButton = new Button
            {
                Text = Localization.Get("Market"),
                Width = halfWidth,
                Height = PanelStyles.S(32)
            };
            PanelStyles.ApplyModeButtonStyle(_marketButton, false);
            _marketButton.Click += MarketButton_Click;

            var modeRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(modeRowSideMargin, PanelStyles.S(4), modeRowSideMargin, PanelStyles.S(4))
            };
            modeRow.AddChild(_limitButton);
            modeRow.AddChild(_marketButton);

            // ===== Разделитель =====
            var sep1 = CreateSeparator();

            // ===== Блок риска (в одну строку) =====
            double innerWidth = PanelStyles.PanelWidth - PanelStyles.S(16);
            double riskComboWidth = PanelStyles.S(84);
            double riskComboHeight = PanelStyles.S(22);
            _riskLabel = new TextBlock { Text = Localization.Get("Risk") };
            PanelStyles.ApplyLabelStyle(_riskLabel);

            _riskModeCombo = new ComboBox
            {
                Width = riskComboWidth,
                Height = riskComboHeight,
                Margin = PanelStyles.ST(4, 2, 2, 2)
            };
            _riskModeCombo.AddItem("Percent");
            _riskModeCombo.AddItem("USD");
            _riskModeCombo.AddItem("EUR");
            _riskModeCombo.SelectedIndex = 0;
            _riskModeCombo.SelectedItemChanged += RiskModeCombo_SelectedItemChanged;

            _riskTextBox = new TextBox
            {
                Text = maxRiskPercent.ToString("F2"),
                Width = innerWidth - riskComboWidth - PanelStyles.S(28) - PanelStyles.S(12),
                Margin = PanelStyles.ST(2)
            };
            PanelStyles.ApplyTextBoxStyle(_riskTextBox);
            _riskTextBox.TextChanged += RiskTextBox_TextChanged;

            _riskUnitText = new TextBlock
            {
                Text = "%",
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(0, 0, 4, 0)
            };

            var riskInlineRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            riskInlineRow.AddChild(_riskModeCombo);
            riskInlineRow.AddChild(_riskTextBox);
            riskInlineRow.AddChild(_riskUnitText);

            _riskErrorText = new TextBlock
            {
                Text = "",
                ForegroundColor = PanelStyles.ButtonError,
                FontSize = PanelStyles.FontSizeSmall,
                Margin = PanelStyles.ST(4, 0, 4, 2),
                TextWrapping = TextWrapping.Wrap,
                IsVisible = false
            };

            var riskColumn = new StackPanel { Orientation = Orientation.Vertical };
            riskColumn.AddChild(_riskLabel);
            riskColumn.AddChild(riskInlineRow);
            riskColumn.AddChild(_riskErrorText);
            var riskRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(2)
            };
            riskRow.AddChild(riskColumn);

            // ===== Блок цены входа =====
            _priceLabel = new TextBlock { Text = Localization.Get("LimitOrder") };
            PanelStyles.ApplyLabelStyle(_priceLabel);

            _priceTextBox = new TextBox
            {
                Text = "",
                IsReadOnly = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(4, 2, 4, 2)
            };
            PanelStyles.ApplyTextBoxStyle(_priceTextBox);
            _priceTextBox.TextChanged += PriceTextBox_TextChanged;

            var sep2 = CreateSeparator();

            // ===== Блок SL / TP =====
            double colWidth = (PanelStyles.PanelWidth - PanelStyles.S(16)) / 2;

            // -- SL --
            _slLabel = new TextBlock { Text = Localization.Get("StopLoss") };
            PanelStyles.ApplyLabelStyle(_slLabel);

            _slTextBox = new TextBox
            {
                Text = "",
                IsReadOnly = true,
                Width = colWidth - PanelStyles.S(4),
                Margin = PanelStyles.ST(2)
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
                Width = colWidth - PanelStyles.S(4),
                Margin = PanelStyles.ST(2)
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
                Margin = PanelStyles.ST(2)
            };
            slTpRow.AddChild(slColumn);
            slTpRow.AddChild(tpColumn);

            var sep3 = CreateSeparator();

            // ===== Кнопка подтверждения =====
            _submitButton = new Button
            {
                Text = Localization.Get("PlaceOrder"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = PanelStyles.S(36),
                Margin = PanelStyles.ST(4, 4, 4, 6)
            };
            PanelStyles.ApplySubmitButtonStyle(_submitButton, 0); // серая, неактивная
            _submitButton.Click += SubmitButton_Click;

            // ===== Собираем контент (всё кроме заголовка) =====
            _contentStack = new StackPanel { Orientation = Orientation.Vertical };
            _contentStack.AddChild(checkboxRow);
            _contentStack.AddChild(modeRow);
            _contentStack.AddChild(sep1);
            _contentStack.AddChild(riskRow);
            _contentStack.AddChild(sep2);
            _contentStack.AddChild(_priceLabel);
            _contentStack.AddChild(_priceTextBox);
            _contentStack.AddChild(sep3);
            _contentStack.AddChild(slTpRow);
            _contentStack.AddChild(CreateSeparator());
            _contentStack.AddChild(_submitButton);

            // ===== Мини-панель: LM, MK, OK, FST (Fast Order) ====
            // Ряд уже по ширине, чем панель, и центрируется — слева и справа остаётся одинаковый зазор (cTrader игнорирует правый Margin у StackPanel).
            double miniPanelSideInset = PanelStyles.S(10);
            double miniPanelMarginV = PanelStyles.S(4);
            double rowInner = PanelStyles.PanelWidth - 2 * miniPanelSideInset;
            // 4 кнопки, MiniModeButtonMargin 1.5+1.5 по горизонтали → 4*W + 12 = rowInner
            double miniBtnWidth = (rowInner - PanelStyles.S(12)) / 4.0;
            double miniBtnHeight = PanelStyles.S(22);
            _miniLimitButton = new Button
            {
                Text = "LM",
                Width = miniBtnWidth,
                Height = miniBtnHeight,
                FontWeight = FontWeight.Bold,
                Margin = PanelStyles.MiniModeButtonMargin,
                VerticalAlignment = VerticalAlignment.Center
            };
            PanelStyles.ApplyModeButtonStyle(_miniLimitButton, false, PanelStyles.MiniModeButtonMargin);
            _miniLimitButton.Click += (args) => ActivateLimit();

            _miniMarketButton = new Button
            {
                Text = "MK",
                Width = miniBtnWidth,
                Height = miniBtnHeight,
                FontWeight = FontWeight.Bold,
                Margin = PanelStyles.MiniModeButtonMargin,
                VerticalAlignment = VerticalAlignment.Center
            };
            PanelStyles.ApplyModeButtonStyle(_miniMarketButton, false, PanelStyles.MiniModeButtonMargin);
            _miniMarketButton.Click += (args) => ActivateMarket();

            _miniSubmitButton = new Button
            {
                Text = "OK",
                Width = miniBtnWidth,
                Height = miniBtnHeight,
                FontWeight = FontWeight.Bold,
                Margin = PanelStyles.MiniSubmitButtonMargin,
                VerticalAlignment = VerticalAlignment.Center
            };
            ApplyMiniSubmitButtonStyle(0);
            _miniSubmitButton.Click += (args) => TrySubmitOrder();

            _miniFoButton = new Button
            {
                Text = "FST",
                Width = miniBtnWidth,
                Height = miniBtnHeight,
                FontWeight = FontWeight.Bold,
                Margin = PanelStyles.MiniModeButtonMargin,
                VerticalAlignment = VerticalAlignment.Center
            };
            PanelStyles.ApplyModeButtonStyle(_miniFoButton, _fastOrderCheckBox.IsChecked == true, PanelStyles.MiniModeButtonMargin);
            _miniFoButton.Click += (args) => SetFastOrder(!IsFastOrder);

            _miniLimitButton.FontSize = MiniButtonFontSize;
            _miniMarketButton.FontSize = MiniButtonFontSize;
            _miniSubmitButton.FontSize = MiniButtonFontSize;
            _miniFoButton.FontSize = MiniButtonFontSize;

            _miniPanelStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _miniPanelStack.AddChild(_miniLimitButton);
            _miniPanelStack.AddChild(_miniMarketButton);
            _miniPanelStack.AddChild(_miniSubmitButton);
            _miniPanelStack.AddChild(_miniFoButton);

            var miniRowWrap = new Border
            {
                Width = rowInner,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = PanelStyles.ST(0, miniPanelMarginV, 0, miniPanelMarginV),
                Child = _miniPanelStack
            };

            var miniFooterSep = new Border
            {
                BackgroundColor = PanelStyles.SeparatorLineColor,
                Height = Math.Max(1, PanelStyles.S(1)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(miniPanelSideInset, 0, miniPanelSideInset, 0)
            };

            double statusDotSize = PanelStyles.S(6);
            var statusDot = new Border
            {
                Width = statusDotSize,
                Height = statusDotSize,
                CornerRadius = statusDotSize / 2,
                BackgroundColor = PanelStyles.StatusIndicatorGreen,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(0, 0, 4, 0)
            };
            var statusText = new TextBlock
            {
                Text = Localization.Get("StatusActive"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeFooter,
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            statusRow.AddChild(statusDot);
            statusRow.AddChild(statusText);

            var footerHint = new TextBlock
            {
                Text = " " + Localization.Get("CollapsedPanelHint"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeFooter,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            // Та же ширина и центрирование, что у ряда LM…FST — «Active» с тем же отступом справа, что и FST
            var footerGrid = new Grid(1, 2)
            {
                Width = rowInner,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(0, 2, 0, miniPanelMarginV)
            };
            footerGrid.Rows[0].SetHeightToAuto();
            footerGrid.Columns[0].SetWidthInStars(1);
            footerGrid.Columns[1].SetWidthToAuto();
            footerGrid.AddChild(footerHint, 0, 0);
            footerGrid.AddChild(statusRow, 0, 1);

            _collapsedChromeStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _collapsedChromeStack.AddChild(miniRowWrap);
            _collapsedChromeStack.AddChild(miniFooterSep);
            _collapsedChromeStack.AddChild(footerGrid);

            // ===== Собираем всё в основной стек =====
            _mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            _mainStack.AddChild(headerGrid);
            _mainStack.AddChild(_collapsedChromeStack);
            _mainStack.AddChild(_contentStack);

            // Основной блок без Border — иначе двойной контур (внешняя оболочка + внутренняя «карточка»)
            _mainPanelRoot = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = PanelStyles.PanelWidth
            };
            _mainPanelRoot.AddChild(_mainStack);

            // ===== Панель настроек (такой же размер и стиль, пока placeholder) =====
            var settingsTitle = new TextBlock
            {
                Text = Localization.Get("Settings"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                FontWeight = FontWeight.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(4, 6, 0, 6)
            };
            var settingsHeaderRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                BackgroundColor = PanelStyles.HeaderBarColor,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            settingsHeaderRow.AddChild(settingsTitle);

            // --- Количество тейков (1, 2 или 3) ---
            var tpCountLabel = new TextBlock
            {
                Text = Localization.Get("TpCountLabel"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(4, 4, 8, 4)
            };
            _tpCountCombo = new ComboBox
            {
                Width = PanelStyles.S(60),
                Height = PanelStyles.S(22),
                Margin = PanelStyles.ST(4)
            };
            _tpCountCombo.AddItem(Localization.Get("TpCount1"));
            _tpCountCombo.AddItem(Localization.Get("TpCount2"));
            _tpCountCombo.AddItem(Localization.Get("TpCount3"));
            _tpCountCombo.SelectedItemChanged += _ => OnTpAllocationSettingsChanged?.Invoke();
            // По умолчанию 1 тейк (индекс 0).
            _tpCountCombo.SelectedIndex = 0;
            var tpCountRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = PanelStyles.ST(2)
            };
            tpCountRow.AddChild(tpCountLabel);
            tpCountRow.AddChild(_tpCountCombo);

            // --- Режим объёма (равный объём / равный профит) ---
            var tpVolumeModeLabel = new TextBlock
            {
                Text = Localization.Get("TpVolumeModeLabel"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(4, 4, 8, 4)
            };
            _tpVolumeModeCombo = new ComboBox
            {
                Width = PanelStyles.S(120),
                Height = PanelStyles.S(22),
                Margin = PanelStyles.ST(4)
            };
            _tpVolumeModeCombo.AddItem(Localization.Get("TpVolumeEqualVolume"));
            _tpVolumeModeCombo.AddItem(Localization.Get("TpVolumeEqualProfit"));
            _tpVolumeModeCombo.SelectedIndex = 0; // равный объём по умолчанию
            _tpVolumeModeCombo.SelectedItemChanged += _ => OnTpAllocationSettingsChanged?.Invoke();
            var tpVolumeModeRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = PanelStyles.ST(2)
            };
            tpVolumeModeRow.AddChild(tpVolumeModeLabel);
            tpVolumeModeRow.AddChild(_tpVolumeModeCombo);

            // --- Прозрачность фона панели (0–80 %) ---
            var transparencyLabel = new TextBlock
            {
                Text = Localization.Get("PanelTransparencyLabel"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(4, 4, 8, 4)
            };
            _transparencyCombo = new ComboBox
            {
                Width = PanelStyles.S(80),
                Height = PanelStyles.S(22),
                Margin = PanelStyles.ST(4)
            };
            for (int p = 0; p <= 80; p += 10)
                _transparencyCombo.AddItem(p + "%");
            _transparencyCombo.SelectedIndex = Math.Min(_panelTransparencyPercent / 10, 8);
            _transparencyCombo.SelectedItemChanged += TransparencyCombo_SelectedItemChanged;

            var transparencyRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = PanelStyles.ST(2)
            };
            transparencyRow.AddChild(transparencyLabel);
            transparencyRow.AddChild(_transparencyCombo);

            // --- Масштаб панели (80–150 %) ---
            var scaleLabel = new TextBlock
            {
                Text = Localization.Get("PanelScaleLabel"),
                ForegroundColor = PanelStyles.TextMuted,
                FontSize = PanelStyles.FontSizeSmall,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = PanelStyles.ST(4, 4, 8, 4)
            };
            _scaleCombo = new ComboBox
            {
                Width = PanelStyles.S(80),
                Height = PanelStyles.S(22),
                Margin = PanelStyles.ST(4)
            };
            for (int p = 80; p <= 150; p += 10)
                _scaleCombo.AddItem(p + "%");
            _isUpdatingFromCode = true;
            int scaleIdx = Math.Max(0, Math.Min(7, (PanelStyles.ScalePercent - 80) / 10));
            _scaleCombo.SelectedIndex = scaleIdx;
            _isUpdatingFromCode = false;
            _scaleCombo.SelectedItemChanged += ScaleCombo_SelectedItemChanged;

            var scaleRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = PanelStyles.ST(2)
            };
            scaleRow.AddChild(scaleLabel);
            scaleRow.AddChild(_scaleCombo);

            var settingsContent = new StackPanel { Orientation = Orientation.Vertical };
            settingsContent.AddChild(settingsHeaderRow);
            settingsContent.AddChild(CreateSeparator());
            settingsContent.AddChild(tpCountRow);
            settingsContent.AddChild(tpVolumeModeRow);
            settingsContent.AddChild(transparencyRow);
            settingsContent.AddChild(scaleRow);

            // Без собственного фона: фон уже у _rootWrapper на всю высоту карточки; второй слой panelBg давал бы двойное затемнение при прозрачности.
            _settingsPanelBorder = new Border
            {
                Child = settingsContent,
                BackgroundColor = Color.FromArgb(0, PanelStyles.PanelBackground),
                BorderThickness = PanelStyles.ST(0),
                CornerRadius = 0,
                Width = PanelStyles.PanelWidth,
                IsVisible = false
            };
            _settingsPanelVisible = false;

            // ===== Корневой контейнер: основная панель + панель настроек =====
            _rootContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = PanelStyles.PanelWidth
            };
            _rootContainer.AddChild(_mainPanelRoot);
            _rootContainer.AddChild(_settingsPanelBorder);

            // ===== Единственная видимая рамка карточки (cAlgo: Chart.AddControl принимает Control) =====
            _rootWrapper = new Border
            {
                Child = _rootContainer,
                VerticalAlignment = MapVertical(vPos),
                HorizontalAlignment = MapHorizontal(hPos),
                Margin = PanelStyles.ST(8),
                BackgroundColor = panelBg,
                BorderColor = PanelStyles.PanelBorderColor,
                BorderThickness = PanelStyles.ST(1),
                CornerRadius = PanelStyles.CornerRadiusPanel,
                Width = PanelStyles.PanelWidth
            };

            // Начальное состояние: по умолчанию свёрнуто; при перезапуске — по сохранённому
            if (s_savedCollapsedState)
            {
                _isCollapsed = true;
                _contentStack.IsVisible = false;
                _collapsedChromeStack.IsVisible = true;
                _toggleButton.Text = HeaderExpandLabelEn;
            }
            else
            {
                _isCollapsed = false;
                _contentStack.IsVisible = true;
                _collapsedChromeStack.IsVisible = false;
                _toggleButton.Text = HeaderCollapseLabelEn;
            }

            UpdateSettingsButtonLabel();
            UpdateRiskUnit();
        }

        #region Public properties

        /// <summary>
        /// Корневой контрол для добавления на график через Chart.AddControl().
        /// Одна рамка и фон у этого Border; основной блок и настройки внутри без второй обводки.
        /// </summary>
        public Border RootControl => _rootWrapper;

        /// <summary>Текущее состояние чекбокса Fast Order.</summary>
        public bool IsFastOrder => _fastOrderCheckBox.IsChecked == true;

        /// <summary>Текущее значение риска из поля ввода (как строка).</summary>
        public string RiskText => _riskTextBox.Text;

        /// <summary>Текущая прозрачность фона (0–80 %), как в настройках.</summary>
        public int CurrentTransparencyPercent => _panelTransparencyPercent;

        /// <summary>Видна ли секция настроек (+ Set / блок TP / прозрачность / масштаб).</summary>
        public bool IsSettingsPanelVisible => _settingsPanelVisible;

        /// <summary>Свёрнута ли основная панель (мини-режим).</summary>
        public bool CollapsedState => _isCollapsed;

        /// <summary>
        /// Перед пересозданием панели задать свёрнутость для следующего экземпляра
        /// (конструктор читает статическое <see cref="s_savedCollapsedState"/>).
        /// </summary>
        public static void SetSavedCollapsedStateForRestore(bool collapsed)
        {
            s_savedCollapsedState = collapsed;
        }

        /// <summary>Восстановить открыт/закрыт блок настроек без клика по кнопке Set.</summary>
        public void RestoreSettingsPanelOpenState(bool open)
        {
            _settingsPanelVisible = open;
            _settingsPanelBorder.IsVisible = open;
            UpdateSettingsButtonLabel();
        }

        /// <summary>Восстановить индексы комбо числа тейков и режима объёма (без лишних событий перераспределения).</summary>
        public void RestoreTpComboSettings(int tpCount, TpVolumeMode mode)
        {
            _isUpdatingFromCode = true;
            int idx = tpCount <= 1 ? 0 : (tpCount == 2 ? 1 : 2);
            _tpCountCombo.SelectedIndex = Math.Max(0, Math.Min(2, idx));
            _tpVolumeModeCombo.SelectedIndex = mode == TpVolumeMode.EqualVolume ? 0 : 1;
            _isUpdatingFromCode = false;
        }

        /// <summary>Установить чекбокс Fast Order и стиль мини-кнопки FST (без вызова <see cref="OnFastOrderToggled"/>).</summary>
        public void SetFastOrderChecked(bool value)
        {
            _isUpdatingFromCode = true;
            _fastOrderCheckBox.IsChecked = value;
            PanelStyles.ApplyModeButtonStyle(_miniFoButton, value, PanelStyles.MiniModeButtonMargin);
            _miniFoButton.FontSize = MiniButtonFontSize;
            _isUpdatingFromCode = false;
        }

        public void SetRiskMode(RiskMode mode)
        {
            _isUpdatingFromCode = true;
            _riskModeCombo.SelectedIndex = mode == RiskMode.USD ? 1 : (mode == RiskMode.EUR ? 2 : 0);
            UpdateRiskUnit();
            _isUpdatingFromCode = false;
        }

        public void ShowRiskError(string message)
        {
            _riskErrorText.Text = message ?? "";
            _riskErrorText.IsVisible = !string.IsNullOrWhiteSpace(_riskErrorText.Text);
        }

        public void ClearRiskError()
        {
            _riskErrorText.Text = "";
            _riskErrorText.IsVisible = false;
        }

        /// <summary>Установить значение риска в поле (без вызова OnRiskChanged).</summary>
        public void SetRiskText(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            _isUpdatingFromCode = true;
            _riskTextBox.Text = value;
            _isUpdatingFromCode = false;
        }

        /// <summary>Кнопка Limit активна (акцентный стиль)?</summary>
        public bool IsLimitActive { get; private set; }

        /// <summary>Кнопка Market активна (акцентный стиль)?</summary>
        public bool IsMarketActive { get; private set; }

        /// <summary>Количество тейк-профитов: 1, 2 или 3 (из панели настроек).</summary>
        public int TpCount => _tpCountCombo.SelectedIndex == 0 ? 1 : (_tpCountCombo.SelectedIndex == 1 ? 2 : 3);

        /// <summary>Режим распределения объёма между тейками (из панели настроек).</summary>
        public TpVolumeMode TpVolumeMode => _tpVolumeModeCombo.SelectedIndex == 0 ? TpVolumeMode.EqualVolume : TpVolumeMode.EqualProfit;

        #endregion

        #region Public methods — обновление данных

        /// <summary>
        /// Обновить отображение спреда (всегда отображается).
        /// </summary>
        public void UpdateSpread(double spreadPips)
        {
            _spreadValueText.Text = spreadPips.ToString("F1");
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
        /// При <paramref name="tpCount"/> ≥ 2 в подписи — суммарная прибыль по ногам и метка режима (как в <c>SplitVolumesForTps</c>).
        /// </summary>
        public void UpdateTakeProfit(double price, int digits, double profitDollars, double profitPercent, int tpCount, TpVolumeMode tpVolumeMode)
        {
            _isUpdatingFromCode = true;
            _tpTextBox.Text = price.ToString("F" + digits);
            _tpTextBox.BackgroundColor = PanelStyles.InputBackground;
            if (tpCount <= 1)
                _tpInfoText.Text = string.Format("{0:F2}$ ({1:F2}%)", profitDollars, profitPercent);
            else
            {
                string modeShort = tpVolumeMode == TpVolumeMode.EqualVolume
                    ? Localization.Get("TpAllocShortEqualVolume")
                    : Localization.Get("TpAllocShortEqualProfit");
                _tpInfoText.Text = Localization.Get("TpInfoMulti", profitDollars, profitPercent, tpCount, modeShort);
            }
            _isUpdatingFromCode = false;
        }

        /// <summary>
        /// Обновить кнопку подтверждения: текст, цвет, активность.
        /// direction: 1=Long, -1=Short, 0=Invalid. Валидно — зелёный OK, ошибка — тускло-красный OK (неактивен).
        /// </summary>
        public void UpdateSubmitButton(int direction, bool isLimit, string symbolName, string volumeLots)
        {
            if (_fastOrderCheckBox.IsChecked == true)
            {
                PanelStyles.ApplySubmitButtonStyle(_submitButton, 0);
                _submitButton.Text = Localization.Get("PlaceOrder");
                ApplyMiniSubmitButtonStyle(0);
                _miniSubmitButton.IsEnabled = false;
                _miniSubmitButton.Text = "OK";
                return;
            }

            if (direction == 0)
            {
                PanelStyles.ApplySubmitButtonStyle(_submitButton, -1);
                _submitButton.Text = Localization.Get("InvalidLevels");
                ApplyMiniSubmitButtonStyle(-1);
                _miniSubmitButton.IsEnabled = false;
                _miniSubmitButton.Text = "OK";
            }
            else if (direction == 1)
            {
                PanelStyles.ApplySubmitButtonStyle(_submitButton, 1);
                _submitButton.Text = isLimit
                    ? Localization.Get("LimitLong", symbolName, volumeLots)
                    : Localization.Get("BuyMarket", symbolName, volumeLots);
                ApplyMiniSubmitButtonStyle(1);
                _miniSubmitButton.IsEnabled = true;
                _miniSubmitButton.Text = "OK";
            }
            else
            {
                PanelStyles.ApplySubmitButtonStyle(_submitButton, 1);
                _submitButton.Text = isLimit
                    ? Localization.Get("LimitShort", symbolName, volumeLots)
                    : Localization.Get("SellMarket", symbolName, volumeLots);
                ApplyMiniSubmitButtonStyle(1);
                _miniSubmitButton.IsEnabled = true;
                _miniSubmitButton.Text = "OK";
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
        /// Восстановить UI режима Limit/Market после перезапуска cBot (смена ТФ). Без вызова OnLimitClicked / OnMarketClicked.
        /// </summary>
        /// <param name="isLimit">true = Limit активен, false = Market.</param>
        /// <param name="tpCount">1, 2 или 3 — соответствует комбо «число тейков».</param>
        public void ApplyRestoredTradingMode(bool isLimit, int tpCount)
        {
            _isUpdatingFromCode = true;

            int idx = tpCount <= 1 ? 0 : (tpCount == 2 ? 1 : 2);
            _tpCountCombo.SelectedIndex = Math.Max(0, Math.Min(2, idx));

            IsLimitActive = isLimit;
            IsMarketActive = !isLimit;
            PanelStyles.ApplyModeButtonStyle(_limitButton, isLimit);
            PanelStyles.ApplyModeButtonStyle(_marketButton, !isLimit);
            PanelStyles.ApplyModeButtonStyle(_miniLimitButton, isLimit, PanelStyles.MiniModeButtonMargin);
            PanelStyles.ApplyModeButtonStyle(_miniMarketButton, !isLimit, PanelStyles.MiniModeButtonMargin);
            _miniLimitButton.FontSize = _miniMarketButton.FontSize = MiniButtonFontSize;

            SetMode(isLimit);
            SetFieldsReadOnly(false);

            _isUpdatingFromCode = false;
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
            PanelStyles.ApplyModeButtonStyle(_miniLimitButton, false, PanelStyles.MiniModeButtonMargin);
            PanelStyles.ApplyModeButtonStyle(_miniMarketButton, false, PanelStyles.MiniModeButtonMargin);
            ApplyMiniSubmitButtonStyle(0);
            _miniSubmitButton.Text = "OK";
            _miniSubmitButton.IsEnabled = false;
            PanelStyles.ApplyModeButtonStyle(_miniFoButton, IsFastOrder, PanelStyles.MiniModeButtonMargin);
            _miniLimitButton.FontSize = MiniButtonFontSize;
            _miniMarketButton.FontSize = MiniButtonFontSize;
            _miniSubmitButton.FontSize = MiniButtonFontSize;
            _miniFoButton.FontSize = MiniButtonFontSize;
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
            _collapsedChromeStack.IsVisible = true;
            _toggleButton.Text = HeaderExpandLabelEn;
        }

        /// <summary>
        /// Программно развернуть панель.
        /// </summary>
        public void Expand()
        {
            _isCollapsed = false;
            s_savedCollapsedState = false;
            _contentStack.IsVisible = true;
            _collapsedChromeStack.IsVisible = false;
            _toggleButton.Text = HeaderCollapseLabelEn;
        }

        #endregion

        #region Private — обработчики событий

        private void UpdateSettingsButtonLabel()
        {
            _settingsButton.Text = _settingsPanelVisible ? "- " + HeaderSettingsLabelEn : "+ " + HeaderSettingsLabelEn;
        }

        private void ToggleButton_Click(ButtonClickEventArgs args)
        {
            if (_isCollapsed)
                Expand();
            else
                Collapse();
        }

        private void SettingsButton_Click(ButtonClickEventArgs args)
        {
            _settingsPanelVisible = !_settingsPanelVisible;
            _settingsPanelBorder.IsVisible = _settingsPanelVisible;
            UpdateSettingsButtonLabel();
        }

        private void TransparencyCombo_SelectedItemChanged(ComboBoxSelectedItemChangedEventArgs args)
        {
            int idx = _transparencyCombo.SelectedIndex;
            if (idx < 0 || idx > 8) return;
            int percent = idx * 10;
            ApplyPanelTransparency(percent);
            OnTransparencyChanged?.Invoke(percent);
        }

        private void ScaleCombo_SelectedItemChanged(ComboBoxSelectedItemChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            int idx = _scaleCombo.SelectedIndex;
            if (idx < 0 || idx > 7) return;
            int percent = 80 + idx * 10;
            OnScaleChanged?.Invoke(percent);
        }

        /// <summary>Применить прозрачность фона карточки (один слой на <see cref="_rootWrapper"/>).</summary>
        private void ApplyPanelTransparency(int percent)
        {
            _panelTransparencyPercent = Math.Max(0, Math.Min(percent, 80));
            Color panelBg = PanelStyles.GetPanelBackgroundWithTransparency(_panelTransparencyPercent);
            _rootWrapper.BackgroundColor = panelBg;
        }

        // Summary-текст настроек TP удалён (раньше отображался справа от риска).

        private void ActivateLimit()
        {
            if (IsLimitActive)
            {
                IsLimitActive = false;
                PanelStyles.ApplyModeButtonStyle(_limitButton, false);
                PanelStyles.ApplyModeButtonStyle(_miniLimitButton, false, PanelStyles.MiniModeButtonMargin);
                _miniLimitButton.FontSize = _miniMarketButton.FontSize = MiniButtonFontSize;
                OnLimitClicked?.Invoke(false);
            }
            else
            {
                if (IsMarketActive)
                {
                    IsMarketActive = false;
                    PanelStyles.ApplyModeButtonStyle(_marketButton, false);
                    PanelStyles.ApplyModeButtonStyle(_miniMarketButton, false, PanelStyles.MiniModeButtonMargin);
                    OnMarketClicked?.Invoke(false);
                }
                IsLimitActive = true;
                PanelStyles.ApplyModeButtonStyle(_limitButton, true);
                PanelStyles.ApplyModeButtonStyle(_miniLimitButton, true, PanelStyles.MiniModeButtonMargin);
                _miniLimitButton.FontSize = _miniMarketButton.FontSize = MiniButtonFontSize;
                OnLimitClicked?.Invoke(true);
            }
        }

        private void ActivateMarket()
        {
            if (IsMarketActive)
            {
                IsMarketActive = false;
                PanelStyles.ApplyModeButtonStyle(_marketButton, false);
                PanelStyles.ApplyModeButtonStyle(_miniMarketButton, false, PanelStyles.MiniModeButtonMargin);
                _miniLimitButton.FontSize = _miniMarketButton.FontSize = MiniButtonFontSize;
                OnMarketClicked?.Invoke(false);
            }
            else
            {
                if (IsLimitActive)
                {
                    IsLimitActive = false;
                    PanelStyles.ApplyModeButtonStyle(_limitButton, false);
                    PanelStyles.ApplyModeButtonStyle(_miniLimitButton, false, PanelStyles.MiniModeButtonMargin);
                    OnLimitClicked?.Invoke(false);
                }
                IsMarketActive = true;
                PanelStyles.ApplyModeButtonStyle(_marketButton, true);
                PanelStyles.ApplyModeButtonStyle(_miniMarketButton, true, PanelStyles.MiniModeButtonMargin);
                _miniLimitButton.FontSize = _miniMarketButton.FontSize = MiniButtonFontSize;
                OnMarketClicked?.Invoke(true);
            }
        }

        /// <summary>Выставить ордер. При свёрнутой панели ордер выставляется по последним заданным уровням (до свёртывания или в Fast Order).</summary>
        private void TrySubmitOrder()
        {
            if (_submitButton.IsEnabled)
                OnSubmitClicked?.Invoke();
        }

        /// <summary>Стиль мини-кнопки OK: как ApplySubmitButtonStyle, плюс Margin и FontSize для выравнивания с соседними мини-кнопками.</summary>
        private void ApplyMiniSubmitButtonStyle(int state)
        {
            PanelStyles.ApplySubmitButtonStyle(_miniSubmitButton, state, PanelStyles.MiniSubmitButtonMargin);
            _miniSubmitButton.FontSize = MiniButtonFontSize;
        }

        private void SetFastOrder(bool value)
        {
            _isUpdatingFromCode = true;
            _fastOrderCheckBox.IsChecked = value;
            PanelStyles.ApplyModeButtonStyle(_miniFoButton, value, PanelStyles.MiniModeButtonMargin);
            _miniFoButton.FontSize = MiniButtonFontSize;
            OnFastOrderToggled?.Invoke(value);
            _isUpdatingFromCode = false;
        }

        private void FastOrderCheckBox_Click(CheckBoxEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            bool isChecked = _fastOrderCheckBox.IsChecked == true;
            PanelStyles.ApplyModeButtonStyle(_miniFoButton, isChecked, PanelStyles.MiniModeButtonMargin);
            _miniFoButton.FontSize = MiniButtonFontSize;
            OnFastOrderToggled?.Invoke(isChecked);
        }

        private void LimitButton_Click(ButtonClickEventArgs args) => ActivateLimit();

        private void MarketButton_Click(ButtonClickEventArgs args) => ActivateMarket();

        private void SubmitButton_Click(ButtonClickEventArgs args) => TrySubmitOrder();

        private void RiskTextBox_TextChanged(TextChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            OnRiskChanged?.Invoke(_riskTextBox.Text);
        }

        private void RiskModeCombo_SelectedItemChanged(ComboBoxSelectedItemChangedEventArgs args)
        {
            if (_isUpdatingFromCode) return;
            UpdateRiskUnit();
            RiskMode mode = _riskModeCombo.SelectedIndex == 1 ? RiskMode.USD : (_riskModeCombo.SelectedIndex == 2 ? RiskMode.EUR : RiskMode.Percent);
            OnRiskModeChanged?.Invoke(mode);
        }

        private void UpdateRiskUnit()
        {
            _riskUnitText.Text = _riskModeCombo.SelectedIndex == 1 ? "USD" : (_riskModeCombo.SelectedIndex == 2 ? "EUR" : "%");
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
                BackgroundColor = PanelStyles.SeparatorLineColor,
                Height = Math.Max(1, PanelStyles.S(1)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = PanelStyles.ST(0, 2, 0, 2)
            };
        }

        /// <summary>Горизонтальный спейсер, занимающий свободное место между элементами.</summary>
        private StackPanel CreateHSpacer()
        {
            return new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = PanelStyles.PanelWidth - PanelStyles.S(80)
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
