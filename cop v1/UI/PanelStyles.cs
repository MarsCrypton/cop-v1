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

        /// <summary>Рамка внешних панелей</summary>
        public static readonly Color PanelBorderColor = Color.FromHex("333333");

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
        private const int BaseCornerRadiusPanel = 3;
        private const int BaseHeaderBarButtonHeight = 19;
        private const int BaseHeaderBarButtonFontSize = 8;
        private const int BaseButtonCornerSubtle = 2;

        #endregion

        #region Sizes (масштабируемые свойства — при 100% совпадают с прежними константами)

        /// <summary>Ширина панели в пикселях (шире прежних 208 — запас под симметричные поля мини-ряда в cTrader).</summary>
        public static double PanelWidth => S(BasePanelWidth);

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

        /// <summary>Алиас для скругления панели (историческое имя).</summary>
        public static int CornerRadius => CornerRadiusPanel;

        /// <summary>Высота кнопок Set / Full-Mini в шапке.</summary>
        public static int HeaderBarButtonHeight => SI(BaseHeaderBarButtonHeight);

        /// <summary>Шрифт кнопок Set / Full-Mini в шапке.</summary>
        public static int HeaderBarButtonFontSize => SF(BaseHeaderBarButtonFontSize);

        /// <summary>Лёгкое скругление кнопок (меньше, чем раньше).</summary>
        public static int ButtonCornerSubtle => SI(BaseButtonCornerSubtle);

        /// <summary>Толщина линий на графике (не масштабируется — объекты графика).</summary>
        public const int LineThickness = 1;

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
            ApplyModeButtonStyle(btn, isActive, ST(2));
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
