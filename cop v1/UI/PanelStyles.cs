using System;
using cAlgo.API;

namespace COP_v1.UI
{
    /// <summary>
    /// Стили, цвета и константы размеров для UI-панели COP v1.
    /// Тёмная тема с оранжевым акцентом (референс мини-панели).
    /// Размеры панели масштабируются через <see cref="SetScalePercent"/> (по умолчанию 100%).
    /// </summary>
    public static class PanelStyles
    {
        #region UI scale (Step 1)

        private static int _scalePercent = 100;

        /// <summary>Текущий коэффициент масштаба (1.0 = 100%).</summary>
        public static double UiScale => _scalePercent / 100.0;

        /// <summary>Текущий масштаб в процентах (80–150 после нормализации).</summary>
        public static int ScalePercent => _scalePercent;

        /// <summary>
        /// Задать масштаб панели в процентах. Значение ограничивается диапазоном 80–150.
        /// До вызова используется 100%.
        /// </summary>
        public static void SetScalePercent(int percent)
        {
            if (percent < 80) percent = 80;
            if (percent > 150) percent = 150;
            _scalePercent = percent;
        }

        /// <summary>Масштабировать длину в пикселях (double).</summary>
        public static double S(double value) => value * UiScale;

        /// <summary>Масштабировать целочисленный размер с округлением (минимум 1 для положительных базовых значений).</summary>
        public static int SI(int value)
        {
            if (value <= 0)
                return value;
            return Math.Max(1, (int)Math.Round(value * UiScale));
        }

        /// <summary>Масштабировать размер шрифта (минимум 8 px для читаемости).</summary>
        public static int SF(int fontSize)
        {
            int scaled = (int)Math.Round(fontSize * UiScale);
            return Math.Max(8, scaled);
        }

        /// <summary>Масштабировать одинаковые отступы со всех сторон.</summary>
        public static Thickness ST(double uniform) => ST(uniform, uniform, uniform, uniform);

        /// <summary>Масштабировать отступы по сторонам.</summary>
        public static Thickness ST(double left, double top, double right, double bottom)
        {
            return new Thickness(S(left), S(top), S(right), S(bottom));
        }

        #endregion

        #region Colors

        /// <summary>Фон панели — почти чёрный #0D0D0D</summary>
        public static readonly Color PanelBackground = Color.FromHex("0D0D0D");

        /// <summary>Фон полей ввода — чуть светлее фона панели #242424</summary>
        public static readonly Color InputBackground = Color.FromHex("242424");

        /// <summary>Основной цвет текста — белый</summary>
        public static readonly Color TextColor = Color.FromHex("FFFFFF");

        /// <summary>Приглушённый текст (подписи, мини-заголовок)</summary>
        public static readonly Color TextMuted = Color.FromHex("666666");

        /// <summary>Активная кнопка / акцент — оранжевый (~на 10% светлее фона #1A для контраста с панелью)</summary>
        public static readonly Color ButtonActive = Color.FromHex("F46E1A");

        /// <summary>Hover активной кнопки</summary>
        public static readonly Color ButtonActiveHover = Color.FromHex("FF8538");

        /// <summary>Неактивная кнопка (~на 10% светлее #1A1A1A)</summary>
        public static readonly Color ButtonInactive = Color.FromHex("313131");

        /// <summary>Мини-кнопки: зазор между соседними вдвое меньше прежнего (1.5+1.5 px).</summary>
        public static Thickness MiniModeButtonMargin => ST(1.5, 2, 1.5, 2);

        /// <summary>Мини-кнопка OK — те же горизонтальные поля, что у LM/MK/FST.</summary>
        public static Thickness MiniSubmitButtonMargin => ST(1.5, 2, 1.5, 2);

        /// <summary>Кнопка ошибки — красный #E53935 (подсветка полей ввода)</summary>
        public static readonly Color ButtonError = Color.FromHex("E53935");

        /// <summary>Кнопка OK при валидном ордере — зелёный (слегка светлее для контраста)</summary>
        public static readonly Color ButtonSubmitOk = Color.FromHex("19CD5B");

        /// <summary>Hover для кнопки OK (валидный ордер)</summary>
        public static readonly Color ButtonSubmitOkHover = Color.FromHex("3DDC75");

        /// <summary>Кнопка OK при ошибке уровней — тусклый красный (светлее)</summary>
        public static readonly Color ButtonSubmitErrorDim = Color.FromHex("7A4545");

        /// <summary>Hover для тусклой ошибки (кнопка обычно неактивна)</summary>
        public static readonly Color ButtonSubmitErrorDimHover = Color.FromHex("8A5252");

        /// <summary>Фон полосы шапки (основная панель, настройки)</summary>
        public static readonly Color HeaderBarColor = Color.FromHex("1A1A1A");

        /// <summary>Горизонтальные разделители внутри контента</summary>
        public static readonly Color SeparatorLineColor = Color.FromHex("1F1F1F");

        /// <summary>Внешняя рамка корневой панели (<c>_rootWrapper</c>).</summary>
        public static readonly Color PanelBorderColor = Color.FromHex("333333");

        /// <summary>Рамка тумблера Fast Order OFF/ON (акцент).</summary>
        public static readonly Color FastOrderToggleBorderColor = Color.FromArgb(255, 235, 86, 0);

        /// <summary>Устаревшее имя: совпадает с фоном шапки (совместимость)</summary>
        public static readonly Color SeparatorColor = HeaderBarColor;

        /// <summary>Hover неактивной кнопки / переключателей (~на 10% светлее)</summary>
        public static readonly Color ButtonHover = Color.FromHex("3F3F3F");

        /// <summary>Индикатор статуса «активна» в футере мини-панели</summary>
        public static readonly Color StatusIndicatorGreen = Color.FromHex("00C853");

        /// <summary>Цвет линии Entry на графике — синий #0000FF</summary>
        public static readonly Color LineEntry = Color.FromHex("0000FF");

        /// <summary>Цвет линии Stop Loss на графике — красный #FF0000</summary>
        public static readonly Color LineStopLoss = Color.FromHex("FF0000");

        /// <summary>Цвет линии Take Profit на графике — зелёный #008000</summary>
        public static readonly Color LineTakeProfit = Color.FromHex("008000");

        #endregion

        #region Sizes (базовые значения при масштабе 100%)

        private const double BasePanelWidth = 228;
        private const int BaseFontSizeNormal = 13;
        private const int BaseFontSizeSmall = 11;
        private const int BaseFontSizeHeaderExpanded = 11;
        private const int BaseFontSizeHeaderMini = 10;
        private const int BaseFontSizeFooter = 9;
        private const int BasePadding = 6;
        private const int BaseCornerRadiusPanel = 6;

        /// <summary>Скругление заливки под внешней рамкой карточки (рамка 1 px — радиус на 1 базовый px меньше).</summary>
        private const int BaseCornerRadiusPanelInner = 5;
        private const int BaseHeaderBarButtonHeight = 19;
        private const int BaseHeaderBarButtonFontSize = 8;
        private const int BaseButtonCornerSubtle = 5;
        private const int BaseModeButtonHeight = 44;

        /// <summary>Горизонтальный inset контента от краёв панели (база @ 100%). Единая сетка для полей/кнопок.</summary>
        private const double BaseContentInsetX = 2;

        /// <summary>Зазор между кнопками Limit/Market (база @ 100%).</summary>
        private const double BaseModeButtonsGap = 8;

        /// <summary>Фиксированная ширина комбо режима риска Percent/USD/EUR (база @ 100%).</summary>
        private const double BaseRiskComboWidth = 84;

        /// <summary>Единая ширина комбо в блоке Settings (самый длинный текст — режим объёма тейков).</summary>
        private const double BaseSettingsComboWidth = 120;

        /// <summary>Высота полей ввода: компакт (SL/TP).</summary>
        private const int BaseInputHeightSm = 26;

        /// <summary>Высота полей ввода: основная (единая для премиум-сетки).</summary>
        private const int BaseInputHeightMd = 28;

        /// <summary>Высота полей: акцентная строка (цена входа).</summary>
        private const int BaseInputHeightLg = 32;

        /// <summary>Высота кнопки Place order (база @ 100%).</summary>
        private const int BaseSubmitButtonHeight = 36;

        /// <summary>Высота сегментов Fast Order OFF/ON (база @ 100%).</summary>
        private const int BaseFastToggleHeight = 20;

        /// <summary>Ширина одного сегмента OFF/ON — запас под «OFF»/«ON» без обрезки в cTrader.</summary>
        private const int BaseFastToggleSegmentWidth = 46;

        /// <summary>Шрифт подписей сегментов (база @ 100%).</summary>
        private const int BaseFastToggleFontSize = 9;

        /// <summary>Скругление внешней рамки сегмент-тумблера (база @ 100%).</summary>
        private const int BaseFastToggleOuterRadius = 4;

        /// <summary>Скругление заливки сегментов OFF/ON — чуть меньше внешнего (учёт Padding рамки).</summary>
        private const int BaseFastToggleInnerRadius = 3;

        #endregion

        #region Sizes (масштабируемые свойства — при 100% совпадают с прежними константами)

        /// <summary>Ширина панели в пикселях (шире прежних 208 — запас под симметричные поля мини-ряда в cTrader).</summary>
        public static double PanelWidth => S(BasePanelWidth);

        /// <summary>Толщина внешней рамки корневой панели (совпадает с <c>BorderThickness</c> у <c>_rootWrapper</c>).</summary>
        public static double RootBorderThickness => S(1);

        /// <summary>
        /// Ширина содержимого внутри <c>_rootWrapper</c> при <c>Padding</c> = толщине рамки —
        /// фоны не перекрывают линию обводки по углам.
        /// </summary>
        public static double PanelClientWidth => PanelWidth - 2 * RootBorderThickness;

        /// <summary>Горизонтальный отступ контента от левого/правого края панели (масштабируется).</summary>
        public static double ContentInsetX => S(BaseContentInsetX);

        /// <summary>Рабочая ширина рядов внутри <c>_contentStack</c> с симметричным inset.</summary>
        public static double ContentWidth => PanelClientWidth - 2 * ContentInsetX;

        /// <summary>Margin только по горизонтали для основного стека контента (симметричный inset).</summary>
        public static Thickness ContentStackHorizontalMargin => ST(BaseContentInsetX, 0, BaseContentInsetX, 0);

        /// <summary>Margin только по горизонтали для панели настроек (та же сетка, что у основного контента).</summary>
        public static Thickness SettingsStackHorizontalMargin => ST(BaseContentInsetX, 0, BaseContentInsetX, 0);

        /// <summary>Зазор между Limit и Market.</summary>
        public static double ModeButtonsGap => S(BaseModeButtonsGap);

        /// <summary>Ширина комбо режима риска.</summary>
        public static double RiskComboWidth => S(BaseRiskComboWidth);

        /// <summary>Единая ширина комбо в настройках.</summary>
        public static double SettingsComboWidth => S(BaseSettingsComboWidth);

        /// <summary>Высота полей SL/TP.</summary>
        public static double InputHeightSm => S(BaseInputHeightSm);

        /// <summary>Унифицированная высота полей (кроме акцентной цены входа).</summary>
        public static double InputHeightMd => S(BaseInputHeightMd);

        /// <summary>Высота поля цены входа (Limit/Market).</summary>
        public static double InputHeightLg => S(BaseInputHeightLg);

        /// <summary>Высота кнопки подтверждения ордера.</summary>
        public static double SubmitButtonHeight => S(BaseSubmitButtonHeight);

        /// <summary>Высота сегментов Fast Order OFF/ON.</summary>
        public static double FastToggleHeight => S(BaseFastToggleHeight);

        /// <summary>Ширина сегмента OFF или ON.</summary>
        public static double FastToggleSegmentWidth => S(BaseFastToggleSegmentWidth);

        /// <summary>Размер шрифта сегментов Fast Order.</summary>
        public static int FastToggleFontSize => SF(BaseFastToggleFontSize);

        /// <summary>Скругление внешней рамки сегмент-тумблера.</summary>
        public static int FastToggleOuterRadius => SI(BaseFastToggleOuterRadius);

        /// <summary>Скругление внешних углов сегментов (левый/правый pill внутри Border).</summary>
        public static int FastToggleInnerRadius => SI(BaseFastToggleInnerRadius);

        /// <summary>Размер основного текста (поля, кнопки).</summary>
        public static int FontSizeNormal => SF(BaseFontSizeNormal);

        /// <summary>Размер мелкого текста (подписи секций).</summary>
        public static int FontSizeSmall => SF(BaseFontSizeSmall);

        /// <summary>Заголовок в развёрнутой шапке (COP v1).</summary>
        public static int FontSizeHeaderExpanded => SF(BaseFontSizeHeaderExpanded);

        /// <summary>Заголовок в свёрнутой шапке (мини-панель).</summary>
        public static int FontSizeHeaderMini => SF(BaseFontSizeHeaderMini);

        /// <summary>Текст футера мини-панели.</summary>
        public static int FontSizeFooter => SF(BaseFontSizeFooter);

        /// <summary>Стандартный внутренний отступ.</summary>
        public static int Padding => SI(BasePadding);

        /// <summary>Скругление внешней рамки карточки — минимальное, чтобы углы не были острыми (слишком большое в cAlgo даёт «двойную» обводку).</summary>
        public static int CornerRadiusPanel => SI(BaseCornerRadiusPanel);

        /// <summary>Скругление верхней шапки и другой заливки внутри рамки (без «ступеньки» у обводки).</summary>
        public static int CornerRadiusPanelInner => SI(BaseCornerRadiusPanelInner);

        /// <summary>Алиас для скругления панели (историческое имя).</summary>
        public static int CornerRadius => CornerRadiusPanel;

        /// <summary>Высота кнопок Set / Full-Mini в шапке.</summary>
        public static int HeaderBarButtonHeight => SI(BaseHeaderBarButtonHeight);

        /// <summary>Шрифт кнопок Set / Full-Mini в шапке.</summary>
        public static int HeaderBarButtonFontSize => SF(BaseHeaderBarButtonFontSize);

        /// <summary>Лёгкое скругление кнопок.</summary>
        public static int ButtonCornerSubtle => SI(BaseButtonCornerSubtle);

        /// <summary>Высота кнопок Limit / Market.</summary>
        public static double ModeButtonHeight => S(BaseModeButtonHeight);

        /// <summary>Толщина линий на графике (не масштабируется — объекты графика).</summary>
        public const int LineThickness = 1;

        /// <summary>
        /// Смещение якоря подписей к линиям вправо по времени: баров от последней видимой свечи
        /// (больше — дальше от свечей, ближе к шкале цен).
        /// </summary>
        public const int ChartLineLabelRightOffsetBars = 9;

        /// <summary>
        /// ZIndex горизонтальных линий Entry/SL/TP: выше типичных пользовательских фигур на графике,
        /// чтобы линию было проще схватить для перетаскивания (см. <c>ChartObject.ZIndex</c> в cAlgo).
        /// </summary>
        public const int ChartTradingLineZIndex = 1_000_000;

        /// <summary>
        /// Подписи у линий — чуть выше линии по Z, но без интерактивности, чтобы не перехватывать фокус у линии.
        /// </summary>
        public const int ChartTradingLabelZIndex = 1_000_001;

        #endregion

        #region Helper methods

        /// <summary>
        /// Возвращает цвет фона панели с заданной прозрачностью.
        /// transparencyPercent: 0 = непрозрачный, 20 = 20% прозрачный (80% непрозрачный).
        /// </summary>
        public static Color GetPanelBackgroundWithTransparency(int transparencyPercent)
        {
            if (transparencyPercent <= 0)
                return PanelBackground;

            if (transparencyPercent >= 100)
                transparencyPercent = 99;

            int alpha = (int)Math.Round(255.0 * (100 - transparencyPercent) / 100.0);
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;

            return Color.FromArgb(alpha, PanelBackground);
        }

        /// <summary>
        /// Применить стиль к кнопке режима (Limit / Market).
        /// Включает hover-эффект: неактивная кнопка слегка светлеет при наведении.
        /// </summary>
        /// <param name="btn">Кнопка</param>
        /// <param name="isActive">true — оранжевая (активна), false — тёмная</param>
        public static void ApplyModeButtonStyle(Button btn, bool isActive)
        {
            ApplyModeButtonStyle(btn, isActive, ST(0));
        }

        /// <summary>То же с заданными отступами (мини-панель — узкие горизонтальные поля).</summary>
        public static void ApplyModeButtonStyle(Button btn, bool isActive, Thickness margin)
        {
            btn.FontSize = FontSizeNormal;
            btn.FontWeight = FontWeight.Bold;
            btn.Margin = margin;

            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);

            if (isActive)
            {
                style.Set(ControlProperty.BackgroundColor, ButtonActive);
                style.Set(ControlProperty.BackgroundColor, ButtonActiveHover, ControlState.Hover);
            }
            else
            {
                style.Set(ControlProperty.BackgroundColor, ButtonInactive);
                style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
            }

            btn.Style = style;
            btn.CornerRadius = new CornerRadius(ButtonCornerSubtle);
        }

        /// <summary>
        /// Мини-кнопка FST: при включённом Fast Order — зелёный (как сегмент ON), иначе как LM/MK.
        /// </summary>
        public static void ApplyMiniFastOrderButtonStyle(Button btn, bool fastOrderActive, Thickness margin)
        {
            btn.FontWeight = FontWeight.Bold;
            btn.Margin = margin;

            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);

            if (fastOrderActive)
            {
                style.Set(ControlProperty.BackgroundColor, ButtonSubmitOk);
                style.Set(ControlProperty.BackgroundColor, ButtonSubmitOkHover, ControlState.Hover);
            }
            else
            {
                style.Set(ControlProperty.BackgroundColor, ButtonInactive);
                style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
            }

            btn.Style = style;
            btn.CornerRadius = new CornerRadius(ButtonCornerSubtle);
        }

        /// <summary>
        /// Стиль сегмента OFF/ON в тумблере Fast Order.
        /// Активный OFF — оранжевый акцент; активный ON — зелёный (как Place order); неактивный — приглушённый.
        /// </summary>
        /// <param name="isOnSegment">true — кнопка «ON», false — кнопка «OFF».</param>
        public static void ApplyFastOrderSegmentButton(Button btn, bool isActiveSegment, bool isOnSegment)
        {
            btn.FontSize = FastToggleFontSize;
            btn.FontWeight = FontWeight.Bold;
            btn.Margin = ST(0);

            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);

            if (isActiveSegment)
            {
                if (isOnSegment)
                {
                    style.Set(ControlProperty.BackgroundColor, ButtonSubmitOk);
                    style.Set(ControlProperty.BackgroundColor, ButtonSubmitOkHover, ControlState.Hover);
                }
                else
                {
                    style.Set(ControlProperty.BackgroundColor, ButtonActive);
                    style.Set(ControlProperty.BackgroundColor, ButtonActiveHover, ControlState.Hover);
                }
            }
            else
            {
                style.Set(ControlProperty.BackgroundColor, ButtonInactive);
                style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
            }

            btn.Style = style;
            // Асимметричное скругление под внешнюю рамку: OFF — только слева, ON — только справа.
            double ir = FastToggleInnerRadius;
            if (isOnSegment)
                btn.CornerRadius = new CornerRadius(0, ir, ir, 0);
            else
                btn.CornerRadius = new CornerRadius(ir, 0, 0, ir);
        }

        /// <summary>
        /// Применить стиль к кнопке подтверждения ордера (OK).
        /// </summary>
        /// <param name="btn">Кнопка</param>
        /// <param name="state">0 = серая (неактивна), 1 = зелёная (валидный ордер), -1 = тускло-красная (ошибка)</param>
        public static void ApplySubmitButtonStyle(Button btn, int state)
        {
            ApplySubmitButtonStyle(btn, state, ST(2, 6, 2, 2));
        }

        public static void ApplySubmitButtonStyle(Button btn, int state, Thickness margin)
        {
            btn.FontSize = FontSizeNormal;
            btn.FontWeight = FontWeight.Bold;
            btn.Margin = margin;

            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);

            switch (state)
            {
                case 1:
                    btn.IsEnabled = true;
                    style.Set(ControlProperty.BackgroundColor, ButtonSubmitOk);
                    style.Set(ControlProperty.BackgroundColor, ButtonSubmitOkHover, ControlState.Hover);
                    break;
                case -1:
                    btn.IsEnabled = false;
                    style.Set(ControlProperty.BackgroundColor, ButtonSubmitErrorDim);
                    style.Set(ControlProperty.BackgroundColor, ButtonSubmitErrorDimHover, ControlState.Hover);
                    break;
                default:
                    btn.IsEnabled = false;
                    style.Set(ControlProperty.BackgroundColor, ButtonInactive);
                    style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
                    break;
            }

            btn.Style = style;
            btn.CornerRadius = new CornerRadius(ButtonCornerSubtle);
        }

        /// <summary>
        /// Применить стиль к полю ввода (TextBox).
        /// </summary>
        public static void ApplyTextBoxStyle(TextBox tb)
        {
            tb.BackgroundColor = InputBackground;
            tb.ForegroundColor = TextColor;
            tb.FontSize = FontSizeNormal;
            tb.Margin = ST(2);
        }

        /// <summary>
        /// Создать Style с hover-эффектом для кнопки сворачивания (Toggle).
        /// </summary>
        public static Style CreateToggleButtonStyle()
        {
            var style = new Style();
            style.Set(ControlProperty.BackgroundColor, ButtonInactive);
            style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);
            return style;
        }

        /// <summary>
        /// Применить стиль к метке-заголовку секции (TextBlock).
        /// </summary>
        public static void ApplyLabelStyle(TextBlock tb)
        {
            tb.ForegroundColor = TextMuted;
            tb.FontSize = FontSizeSmall;
            tb.Margin = ST(2, 4, 2, 0);
            tb.TextAlignment = TextAlignment.Center;
        }

        /// <summary>
        /// Применить стиль к текстовому значению (TextBlock с данными).
        /// </summary>
        public static void ApplyValueStyle(TextBlock tb)
        {
            tb.ForegroundColor = TextColor;
            tb.FontSize = FontSizeSmall;
            tb.Margin = ST(2, 0, 2, 2);
        }

        #endregion
    }
}
