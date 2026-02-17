namespace COP_v1.Trading
{
    /// <summary>
    /// Направление ордера, определяемое расположением уровней.
    /// </summary>
    public enum OrderDirection
    {
        /// <summary>Long: TP > Entry > SL</summary>
        Long = 1,

        /// <summary>Short: SL > Entry > TP</summary>
        Short = -1,

        /// <summary>Уровни выставлены некорректно.</summary>
        Invalid = 0
    }

    /// <summary>
    /// Валидация уровней Entry / SL / TP.
    /// Определяет направление ордера или ошибку расстановки.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>
        /// Определить направление ордера по расположению уровней.
        /// Long:  TP больше Entry, SL меньше Entry.
        /// Short: SL больше Entry, TP меньше Entry.
        /// Иначе: Invalid.
        /// </summary>
        public static OrderDirection Validate(double entry, double sl, double tp)
        {
            // Long: TP выше Entry, SL ниже Entry
            if (tp > entry && sl < entry)
                return OrderDirection.Long;

            // Short: TP ниже Entry, SL выше Entry
            if (tp < entry && sl > entry)
                return OrderDirection.Short;

            // Всё остальное — ошибка
            return OrderDirection.Invalid;
        }
    }
}
