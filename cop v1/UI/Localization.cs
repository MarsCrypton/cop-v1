using System;
using System.Collections.Generic;

namespace COP_v1.UI
{
    /// <summary>
    /// Система локализации интерфейса (EN / RU).
    /// Все строки панели берутся через Localization.Get("Key").
    /// </summary>
    public static class Localization
    {
        private static Language _lang = Language.EN;

        private static readonly Dictionary<string, Dictionary<Language, string>> Strings = new Dictionary<string, Dictionary<Language, string>>
        {
            // --- Панель: заголовок ---
            ["PanelTitle"] = new Dictionary<Language, string>
            {
                [Language.EN] = "COP v1",
                [Language.RU] = "COP v1"
            },

            // --- Чекбоксы ---
            ["FastOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "fast order",
                [Language.RU] = "быстрый ордер"
            },
            ["Spread"] = new Dictionary<Language, string>
            {
                [Language.EN] = "spred",
                [Language.RU] = "спред"
            },

            // --- Кнопки режимов ---
            ["Limit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Limit",
                [Language.RU] = "Limit"
            },
            ["Market"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Market",
                [Language.RU] = "Market"
            },

            // --- Поле риска ---
            ["MaxRisk"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Max risk, %",
                [Language.RU] = "Макс. риск, %"
            },

            // --- Заголовок блока цены ---
            ["LimitOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Limit Order",
                [Language.RU] = "Лимитный ордер"
            },
            ["MarketOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Market Order",
                [Language.RU] = "Рыночный ордер"
            },

            // --- SL / TP заголовки ---
            ["StopLoss"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Stop Loss",
                [Language.RU] = "Stop Loss"
            },
            ["TakeProfit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Take Profit",
                [Language.RU] = "Take Profit"
            },

            // --- Кнопка подтверждения ---
            ["PlaceOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Place order",
                [Language.RU] = "Выставить ордер"
            },
            ["InvalidLevels"] = new Dictionary<Language, string>
            {
                [Language.EN] = "!!! Set levels correctly !!!",
                [Language.RU] = "!!! Выстави верно уровни !!!"
            },

            // --- Тексты кнопки подтверждения (с параметрами {0}=symbol, {1}=volume) ---
            ["LimitLong"] = new Dictionary<Language, string>
            {
                [Language.EN] = "LIMIT long {0} {1} lot",
                [Language.RU] = "LIMIT long {0} {1} лот"
            },
            ["LimitShort"] = new Dictionary<Language, string>
            {
                [Language.EN] = "LIMIT short {0} {1} lot",
                [Language.RU] = "LIMIT short {0} {1} лот"
            },
            ["BuyMarket"] = new Dictionary<Language, string>
            {
                [Language.EN] = "BUY {0} {1} lot",
                [Language.RU] = "BUY {0} {1} лот"
            },
            ["SellMarket"] = new Dictionary<Language, string>
            {
                [Language.EN] = "SELL {0} {1} lot",
                [Language.RU] = "SELL {0} {1} лот"
            },

            // --- Тексты на линиях графика (с параметрами) ---
            ["StopText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "STOP {0}%",
                [Language.RU] = "STOP {0}%"
            },
            ["TpText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "TP (RR {0})",
                [Language.RU] = "TP (RR {0})"
            },
            ["LimitText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "LIMIT {0} lot",
                [Language.RU] = "LIMIT {0} лот"
            },
            ["MarketText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "MARKET {0} lot",
                [Language.RU] = "MARKET {0} лот"
            },

            // --- Панель настроек ---
            ["Settings"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Settings",
                [Language.RU] = "Настройки"
            },
            ["SettingsPlaceholder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Other settings will be here",
                [Language.RU] = "Здесь будут другие настройки"
            },

            // --- Multi-TP настройки ---
            ["TpCountLabel"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Take profits",
                [Language.RU] = "Тейков"
            },
            ["TpCount1"] = new Dictionary<Language, string>
            {
                [Language.EN] = "1",
                [Language.RU] = "1"
            },
            ["TpCount2"] = new Dictionary<Language, string>
            {
                [Language.EN] = "2",
                [Language.RU] = "2"
            },
            ["TpCount3"] = new Dictionary<Language, string>
            {
                [Language.EN] = "3",
                [Language.RU] = "3"
            },
            ["TpVolumeModeLabel"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Volume mode",
                [Language.RU] = "Режим объёма"
            },
            ["TpVolumeEqualVolume"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Equal volume",
                [Language.RU] = "Равный объём"
            },
            ["TpVolumeEqualProfit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Equal profit",
                [Language.RU] = "Равный профит"
            }
        };

        /// <summary>
        /// Установить текущий язык интерфейса.
        /// </summary>
        public static void SetLanguage(Language lang)
        {
            _lang = lang;
        }

        /// <summary>
        /// Получить локализованную строку по ключу.
        /// Если ключ не найден — возвращает сам ключ.
        /// </summary>
        public static string Get(string key)
        {
            if (Strings.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_lang, out var value))
                    return value;
            }
            return key;
        }

        /// <summary>
        /// Получить локализованную строку с подставленными аргументами.
        /// Пример: Get("LimitLong", "EURUSD", "0.01") → "LIMIT long EURUSD 0.01 lot"
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }
    }
}
