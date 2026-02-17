using cAlgo.API;

namespace COP_v1.UI
{
    /// <summary>
    /// Стили, цвета и константы размеров для UI-панели.
    /// Цветовая схема — тёмная тема, аналог Trade Helper v3.9.
    /// </summary>
    public static class PanelStyles
    {
        #region Colors

        /// <summary>Фон панели — тёмно-серый #2D2D2D</summary>
        public static readonly Color PanelBackground = Color.FromHex("2D2D2D");

        /// <summary>Фон полей ввода — чуть светлее #3C3C3C</summary>
        public static readonly Color InputBackground = Color.FromHex("3C3C3C");

        /// <summary>Основной цвет текста — белый</summary>
        public static readonly Color TextColor = Color.FromHex("FFFFFF");

        /// <summary>Приглушённый текст (подписи, подсказки)</summary>
        public static readonly Color TextMuted = Color.FromHex("AAAAAA");

        /// <summary>Активная кнопка — бирюзовый/зелёный #00BFA5</summary>
        public static readonly Color ButtonActive = Color.FromHex("00BFA5");

        /// <summary>Неактивная кнопка — серый #555555</summary>
        public static readonly Color ButtonInactive = Color.FromHex("555555");

        /// <summary>Кнопка ошибки — красный #E53935</summary>
        public static readonly Color ButtonError = Color.FromHex("E53935");

        /// <summary>Разделители — тёмный #1A1A1A</summary>
        public static readonly Color SeparatorColor = Color.FromHex("1A1A1A");

        /// <summary>Цвет hover-состояния неактивной кнопки — светло-серый #777777</summary>
        public static readonly Color ButtonHover = Color.FromHex("777777");

        /// <summary>Цвет линии Entry на графике — синий #0000FF</summary>
        public static readonly Color LineEntry = Color.FromHex("0000FF");

        /// <summary>Цвет линии Stop Loss на графике — красный #FF0000</summary>
        public static readonly Color LineStopLoss = Color.FromHex("FF0000");

        /// <summary>Цвет линии Take Profit на графике — зелёный #008000</summary>
        public static readonly Color LineTakeProfit = Color.FromHex("008000");

        #endregion

        #region Sizes

        /// <summary>Ширина панели в пикселях (208 = на 20% уже оригинальных 260).</summary>
        public const double PanelWidth = 208;

        /// <summary>Размер основного текста (поля, кнопки).</summary>
        public const int FontSizeNormal = 13;

        /// <summary>Размер мелкого текста (заголовки секций, подписи).</summary>
        public const int FontSizeSmall = 11;

        /// <summary>Стандартный внутренний отступ.</summary>
        public const int Padding = 6;

        /// <summary>Скруглённые углы рамки панели.</summary>
        public const int CornerRadius = 4;

        /// <summary>Толщина линий на графике.</summary>
        public const int LineThickness = 1;

        #endregion

        #region Helper methods

        /// <summary>
        /// Применить стиль к кнопке режима (Limit / Market).
        /// Включает hover-эффект: серая кнопка подсвечивается при наведении.
        /// </summary>
        /// <param name="btn">Кнопка</param>
        /// <param name="isActive">true — зелёная (активна), false — серая</param>
        public static void ApplyModeButtonStyle(Button btn, bool isActive)
        {
            btn.FontSize = FontSizeNormal;
            btn.FontWeight = FontWeight.Bold;
            btn.Margin = new Thickness(2);

            // ВСЕ цвета задаются ТОЛЬКО через Style — иначе hover не работает.
            // Прямое btn.BackgroundColor перекрывает Style.
            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);

            if (isActive)
            {
                style.Set(ControlProperty.BackgroundColor, ButtonActive);
                style.Set(ControlProperty.BackgroundColor, Color.FromHex("00E6C3"), ControlState.Hover);
            }
            else
            {
                style.Set(ControlProperty.BackgroundColor, ButtonInactive);
                style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
            }

            btn.Style = style;
        }

        /// <summary>
        /// Применить стиль к кнопке подтверждения ордера.
        /// Включает hover-эффект для зелёной и красной кнопки.
        /// </summary>
        /// <param name="btn">Кнопка</param>
        /// <param name="state">0 = серая (неактивна), 1 = зелёная (валидный ордер), -1 = красная (ошибка)</param>
        public static void ApplySubmitButtonStyle(Button btn, int state)
        {
            btn.FontSize = FontSizeNormal;
            btn.FontWeight = FontWeight.Bold;
            btn.Margin = new Thickness(2, 6, 2, 2);

            // ВСЕ цвета задаются ТОЛЬКО через Style — иначе hover не работает
            var style = new Style();
            style.Set(ControlProperty.ForegroundColor, TextColor);
            style.Set(ControlProperty.ForegroundColor, TextColor, ControlState.Hover);

            switch (state)
            {
                case 1:
                    btn.IsEnabled = true;
                    style.Set(ControlProperty.BackgroundColor, ButtonActive);
                    style.Set(ControlProperty.BackgroundColor, Color.FromHex("00E6C3"), ControlState.Hover);
                    break;
                case -1:
                    btn.IsEnabled = false;
                    style.Set(ControlProperty.BackgroundColor, ButtonError);
                    style.Set(ControlProperty.BackgroundColor, Color.FromHex("FF5252"), ControlState.Hover);
                    break;
                default:
                    btn.IsEnabled = false;
                    style.Set(ControlProperty.BackgroundColor, ButtonInactive);
                    style.Set(ControlProperty.BackgroundColor, ButtonHover, ControlState.Hover);
                    break;
            }

            btn.Style = style;
        }

        /// <summary>
        /// Применить стиль к полю ввода (TextBox).
        /// </summary>
        public static void ApplyTextBoxStyle(TextBox tb)
        {
            tb.BackgroundColor = InputBackground;
            tb.ForegroundColor = TextColor;
            tb.FontSize = FontSizeNormal;
            tb.Margin = new Thickness(2);
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
            tb.Margin = new Thickness(2, 4, 2, 0);
        }

        /// <summary>
        /// Применить стиль к текстовому значению (TextBlock с данными).
        /// </summary>
        public static void ApplyValueStyle(TextBlock tb)
        {
            tb.ForegroundColor = TextColor;
            tb.FontSize = FontSizeSmall;
            tb.Margin = new Thickness(2, 0, 2, 2);
        }

        #endregion
    }
}
