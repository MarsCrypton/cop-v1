using System;
using cAlgo.API;

namespace COP_v1.Trading
{
    /// <summary>
    /// Расчёт объёма позиции по допустимому риску,
    /// а также расчёт убытка (SL), прибыли (TP) и Risk-Reward.
    /// </summary>
    public class RiskCalculator
    {
        private readonly Robot _bot;

        public RiskCalculator(Robot bot)
        {
            _bot = bot;
        }

        /// <summary>
        /// Рассчитать объём позиции (в units) на основе допустимого % риска.
        /// VolumeInUnits = RiskAmount / (SL_pips * PipValue)
        /// Результат нормализован к шагу и ограничен min/max.
        /// </summary>
        public double CalculateVolume(double entryPrice, double slPrice, double riskPercent)
        {
            // Защита от невалидных данных (NaN, 0, отрицательные)
            if (double.IsNaN(entryPrice) || double.IsNaN(slPrice) || double.IsNaN(riskPercent)
                || entryPrice <= 0 || slPrice <= 0 || riskPercent <= 0)
                return _bot.Symbol.VolumeInUnitsMin;

            double slDistancePips = Math.Abs(entryPrice - slPrice) / _bot.Symbol.PipSize;

            // Защита от деления на 0 или слишком маленького стопа
            if (slDistancePips < 0.1 || _bot.Symbol.PipValue <= 0 || _bot.Account.Balance <= 0)
                return _bot.Symbol.VolumeInUnitsMin;

            double riskAmount = _bot.Account.Balance * (riskPercent / 100.0);
            double volumeInUnits = riskAmount / (slDistancePips * _bot.Symbol.PipValue);

            // Защита от Infinity (если PipValue слишком мал)
            if (double.IsInfinity(volumeInUnits) || double.IsNaN(volumeInUnits))
                return _bot.Symbol.VolumeInUnitsMin;

            return NormalizeVolume(volumeInUnits);
        }

        /// <summary>
        /// Рассчитать убыток при срабатывании Stop Loss.
        /// Возвращает (dollars, percent от баланса).
        /// </summary>
        public void CalculateLoss(double entryPrice, double slPrice, double volumeInUnits, out double dollars, out double percent)
        {
            dollars = 0;
            percent = 0;

            if (double.IsNaN(entryPrice) || double.IsNaN(slPrice) || double.IsNaN(volumeInUnits)
                || entryPrice <= 0 || slPrice <= 0 || volumeInUnits <= 0)
                return;

            double slPips = Math.Abs(entryPrice - slPrice) / _bot.Symbol.PipSize;
            dollars = slPips * _bot.Symbol.PipValue * volumeInUnits;

            if (double.IsNaN(dollars) || double.IsInfinity(dollars))
            {
                dollars = 0;
                return;
            }

            percent = _bot.Account.Balance > 0
                ? (dollars / _bot.Account.Balance) * 100.0
                : 0;
        }

        /// <summary>
        /// Рассчитать прибыль при достижении Take Profit.
        /// Возвращает (dollars, percent от баланса).
        /// </summary>
        public void CalculateProfit(double entryPrice, double tpPrice, double volumeInUnits, out double dollars, out double percent)
        {
            dollars = 0;
            percent = 0;

            if (double.IsNaN(entryPrice) || double.IsNaN(tpPrice) || double.IsNaN(volumeInUnits)
                || entryPrice <= 0 || tpPrice <= 0 || volumeInUnits <= 0)
                return;

            double tpPips = Math.Abs(tpPrice - entryPrice) / _bot.Symbol.PipSize;
            dollars = tpPips * _bot.Symbol.PipValue * volumeInUnits;

            if (double.IsNaN(dollars) || double.IsInfinity(dollars))
            {
                dollars = 0;
                return;
            }

            percent = _bot.Account.Balance > 0
                ? (dollars / _bot.Account.Balance) * 100.0
                : 0;
        }

        /// <summary>
        /// Рассчитать Risk-Reward ratio.
        /// RR = TP_distance / SL_distance.
        /// </summary>
        public double CalculateRR(double entryPrice, double slPrice, double tpPrice)
        {
            if (double.IsNaN(entryPrice) || double.IsNaN(slPrice) || double.IsNaN(tpPrice)
                || entryPrice <= 0 || slPrice <= 0 || tpPrice <= 0)
                return 0;

            double slDistance = Math.Abs(entryPrice - slPrice);
            double tpDistance = Math.Abs(tpPrice - entryPrice);

            if (slDistance < _bot.Symbol.PipSize * 0.1)
                return 0;

            return tpDistance / slDistance;
        }

        /// <summary>
        /// Конвертировать units → lots для отображения.
        /// </summary>
        public double ToLots(double volumeInUnits)
        {
            return _bot.Symbol.VolumeInUnitsToQuantity(volumeInUnits);
        }

        /// <summary>
        /// Нормализовать объём: округлить к шагу, ограничить min/max.
        /// Публичный для расчёта объёма по каждому тейку при нескольких TP.
        /// </summary>
        public double NormalizeVolume(double volumeInUnits)
        {
            double step = _bot.Symbol.VolumeInUnitsStep;
            double minVol = _bot.Symbol.VolumeInUnitsMin;
            double maxVol = _bot.Symbol.VolumeInUnitsMax;

            // Округлить к шагу
            volumeInUnits = Math.Round(volumeInUnits / step) * step;

            // Ограничить снизу
            if (volumeInUnits < minVol)
                volumeInUnits = minVol;

            // Ограничить сверху
            if (volumeInUnits > maxVol)
                volumeInUnits = maxVol;

            return volumeInUnits;
        }
    }
}
