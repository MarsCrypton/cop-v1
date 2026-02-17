using System;
using cAlgo.API;

namespace COP_v1.Trading
{
    /// <summary>
    /// Размещение лимитных и рыночных ордеров через cTrader API.
    /// Рассчитывает SL/TP в пипсах и передаёт в PlaceLimitOrder / ExecuteMarketOrder.
    /// </summary>
    public class OrderManager
    {
        private readonly Robot _bot;

        /// <summary>Лейбл, присваиваемый всем ордерам бота.</summary>
        public const string OrderLabel = "COP";

        public OrderManager(Robot bot)
        {
            _bot = bot;
        }

        /// <summary>
        /// Разместить лимитный ордер с SL и TP.
        /// Направление определяется автоматически: TP выше Entry → Buy, иначе Sell.
        /// Использует ProtectionType.Relative (пипсы).
        /// </summary>
        /// <returns>TradeResult для проверки успешности.</returns>
        public TradeResult PlaceLimitOrder(double entryPrice, double slPrice, double tpPrice, double volumeInUnits)
        {
            TradeType direction = tpPrice > entryPrice ? TradeType.Buy : TradeType.Sell;

            double slPips = Math.Round(Math.Abs(entryPrice - slPrice) / _bot.Symbol.PipSize, 1);
            double tpPips = Math.Round(Math.Abs(tpPrice - entryPrice) / _bot.Symbol.PipSize, 1);

            // Защита: минимум 1 пип для SL/TP
            if (slPips < 1) slPips = 1;
            if (tpPips < 1) tpPips = 1;

            _bot.Print("OrderManager: PlaceLimitOrder {0} {1} vol={2} entry={3} sl={4}pips tp={5}pips",
                direction, _bot.Symbol.Name, volumeInUnits,
                entryPrice.ToString("F" + _bot.Symbol.Digits),
                slPips.ToString("F1"), tpPips.ToString("F1"));

            // Overload 3: (tradeType, symbolName, volume, targetPrice, label, stopLoss, takeProfit, protectionType)
            return _bot.PlaceLimitOrder(
                direction,
                _bot.Symbol.Name,
                volumeInUnits,
                entryPrice,
                OrderLabel,
                slPips,
                tpPips,
                ProtectionType.Relative);
        }

        /// <summary>
        /// Открыть рыночный ордер с SL и TP.
        /// Направление определяется автоматически: TP выше текущей цены → Buy, иначе Sell.
        /// Buy использует Ask, Sell использует Bid.
        /// </summary>
        /// <returns>TradeResult для проверки успешности.</returns>
        public TradeResult PlaceMarketOrder(double slPrice, double tpPrice, double volumeInUnits)
        {
            // Определяем направление по TP относительно текущей цены
            TradeType direction;
            double currentPrice;

            if (tpPrice > _bot.Symbol.Ask)
            {
                direction = TradeType.Buy;
                currentPrice = _bot.Symbol.Ask;
            }
            else
            {
                direction = TradeType.Sell;
                currentPrice = _bot.Symbol.Bid;
            }

            double slPips = Math.Round(Math.Abs(currentPrice - slPrice) / _bot.Symbol.PipSize, 1);
            double tpPips = Math.Round(Math.Abs(tpPrice - currentPrice) / _bot.Symbol.PipSize, 1);

            // Защита: минимум 1 пип для SL/TP
            if (slPips < 1) slPips = 1;
            if (tpPips < 1) tpPips = 1;

            _bot.Print("OrderManager: ExecuteMarketOrder {0} {1} vol={2} sl={3}pips tp={4}pips",
                direction, _bot.Symbol.Name, volumeInUnits, slPips.ToString("F1"), tpPips.ToString("F1"));

            // Overload: (tradeType, symbolName, volume, label, stopLoss, takeProfit, comment, hasTrailingStop)
            return _bot.ExecuteMarketOrder(
                direction,
                _bot.Symbol.Name,
                volumeInUnits,
                OrderLabel,
                slPips,
                tpPips,
                "",
                false);
        }
    }
}
