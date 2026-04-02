using System;
using System.Collections.Generic;

namespace COP_v1.UI
{
    /// <summary>
    /// Система локализации интерфейса (EN, RU, DE, FR, ES, IT, PL, NL, PT).
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
                [Language.RU] = "COP v1",
                [Language.DE] = "COP v1",
                [Language.FR] = "COP v1",
                [Language.ES] = "COP v1",
                [Language.IT] = "COP v1",
                [Language.PL] = "COP v1",
                [Language.NL] = "COP v1",
                [Language.PT] = "COP v1"
            },
            ["MiniPanelTitle"] = new Dictionary<Language, string>
            {
                [Language.EN] = "COP V1 — MINI PANEL",
                [Language.RU] = "COP V1 — МИНИ-ПАНЕЛЬ",
                [Language.DE] = "COP V1 — MINI-PANEL",
                [Language.FR] = "COP V1 — MINI PANEL",
                [Language.ES] = "COP V1 — MINI PANEL",
                [Language.IT] = "COP V1 — MINI PANNELLO",
                [Language.PL] = "COP V1 — MINI PANEL",
                [Language.NL] = "COP V1 — MINI PANEEL",
                [Language.PT] = "COP V1 — MINI PAINEL"
            },
            ["CollapsedPanelHint"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Subscription",
                [Language.RU] = "Подписка",
                [Language.DE] = "Abonnement",
                [Language.FR] = "Abonnement",
                [Language.ES] = "Suscripción",
                [Language.IT] = "Abbonamento",
                [Language.PL] = "Subskrypcja",
                [Language.NL] = "Abonnement",
                [Language.PT] = "Assinatura"
            },
            ["StatusActive"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Active",
                [Language.RU] = "Активна",
                [Language.DE] = "Aktiv",
                [Language.FR] = "Actif",
                [Language.ES] = "Activa",
                [Language.IT] = "Attiva",
                [Language.PL] = "Aktywna",
                [Language.NL] = "Actief",
                [Language.PT] = "Ativa"
            },
            // --- Чекбоксы ---
            ["FastOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "fast order",
                [Language.RU] = "быстрый ордер",
                [Language.DE] = "Schnellorder",
                [Language.FR] = "Ordre rapide",
                [Language.ES] = "Orden rápida",
                [Language.IT] = "Ordine rapido",
                [Language.PL] = "Szybkie zlecenie",
                [Language.NL] = "Snelle order",
                [Language.PT] = "Ordem rápida"
            },
            ["Spread"] = new Dictionary<Language, string>
            {
                [Language.EN] = "spred",
                [Language.RU] = "спред",
                [Language.DE] = "Spread",
                [Language.FR] = "Écart",
                [Language.ES] = "Spread",
                [Language.IT] = "Spread",
                [Language.PL] = "Spread",
                [Language.NL] = "Spread",
                [Language.PT] = "Spread"
            },

            // --- Кнопки режимов ---
            ["Limit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Limit",
                [Language.RU] = "Limit",
                [Language.DE] = "Limit",
                [Language.FR] = "Limit",
                [Language.ES] = "Limit",
                [Language.IT] = "Limit",
                [Language.PL] = "Limit",
                [Language.NL] = "Limit",
                [Language.PT] = "Limit"
            },
            ["Market"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Market",
                [Language.RU] = "Market",
                [Language.DE] = "Market",
                [Language.FR] = "Market",
                [Language.ES] = "Market",
                [Language.IT] = "Market",
                [Language.PL] = "Market",
                [Language.NL] = "Market",
                [Language.PT] = "Market"
            },

            // --- Поле риска ---
            ["MaxRisk"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Max risk, %",
                [Language.RU] = "Макс. риск, %",
                [Language.DE] = "Max. Risiko %",
                [Language.FR] = "Risque max. %",
                [Language.ES] = "Riesgo máx. %",
                [Language.IT] = "Rischio max. %",
                [Language.PL] = "Maks. ryzyko %",
                [Language.NL] = "Max. risico %",
                [Language.PT] = "Risco máx. %"
            },

            ["Risk"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Risk",
                [Language.RU] = "Риск",
                [Language.DE] = "Risiko",
                [Language.FR] = "Risque",
                [Language.ES] = "Riesgo",
                [Language.IT] = "Rischio",
                [Language.PL] = "Ryzyko",
                [Language.NL] = "Risico",
                [Language.PT] = "Risco"
            },

            ["RiskConvertError"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Currency conversion failed",
                [Language.RU] = "Ошибка конвертации валюты",
                [Language.DE] = "Währungsumrechnung fehlgeschlagen",
                [Language.FR] = "Conversion de devise échouée",
                [Language.ES] = "Fallo en la conversión de divisa",
                [Language.IT] = "Conversione valuta non riuscita",
                [Language.PL] = "Nieudana konwersja waluty",
                [Language.NL] = "Valutaconversie mislukt",
                [Language.PT] = "Falha na conversão de moeda"
            },

            // --- Заголовок блока цены ---
            ["LimitOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Limit Order",
                [Language.RU] = "Лимитный ордер",
                [Language.DE] = "Limit-Order",
                [Language.FR] = "Ordre limite",
                [Language.ES] = "Orden límite",
                [Language.IT] = "Ordine limite",
                [Language.PL] = "Zlecenie limit",
                [Language.NL] = "Limitorder",
                [Language.PT] = "Ordem limite"
            },
            ["MarketOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Market Order",
                [Language.RU] = "Рыночный ордер",
                [Language.DE] = "Markt-Order",
                [Language.FR] = "Ordre marché",
                [Language.ES] = "Orden mercado",
                [Language.IT] = "Ordine mercato",
                [Language.PL] = "Zlecenie rynkowe",
                [Language.NL] = "Marktorder",
                [Language.PT] = "Ordem mercado"
            },

            // --- SL / TP заголовки ---
            ["StopLoss"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Stop Loss",
                [Language.RU] = "Stop Loss",
                [Language.DE] = "Stop Loss",
                [Language.FR] = "Stop Loss",
                [Language.ES] = "Stop Loss",
                [Language.IT] = "Stop Loss",
                [Language.PL] = "Stop Loss",
                [Language.NL] = "Stop Loss",
                [Language.PT] = "Stop Loss"
            },
            ["TakeProfit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Take Profit",
                [Language.RU] = "Take Profit",
                [Language.DE] = "Take Profit",
                [Language.FR] = "Take Profit",
                [Language.ES] = "Take Profit",
                [Language.IT] = "Take Profit",
                [Language.PL] = "Take Profit",
                [Language.NL] = "Take Profit",
                [Language.PT] = "Take Profit"
            },

            // --- Кнопка подтверждения ---
            ["PlaceOrder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Place order",
                [Language.RU] = "Выставить ордер",
                [Language.DE] = "Order platzieren",
                [Language.FR] = "Placer ordre",
                [Language.ES] = "Colocar orden",
                [Language.IT] = "Piazzare ordine",
                [Language.PL] = "Zleć zlecenie",
                [Language.NL] = "Order plaatsen",
                [Language.PT] = "Colocar ordem"
            },
            ["InvalidLevels"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Set levels correctly !",
                [Language.RU] = "Выстави верно уровни !",
                [Language.DE] = "Levels korrekt setzen !",
                [Language.FR] = "Réglage des niveaux !",
                [Language.ES] = "Ajusta los niveles !",
                [Language.IT] = "Imposta i livelli !",
                [Language.PL] = "Ustaw poziomy !",
                [Language.NL] = "Stel niveaus in !",
                [Language.PT] = "Ajuste os níveis !"
            },

            // --- Тексты кнопки подтверждения (с параметрами {0}=symbol, {1}=volume) ---
            ["LimitLong"] = new Dictionary<Language, string>
            {
                [Language.EN] = "LIMIT long {0} {1} lot",
                [Language.RU] = "LIMIT long {0} {1} лот",
                [Language.DE] = "LIMIT long {0} {1} Lot",
                [Language.FR] = "LIMIT long {0} {1} lot",
                [Language.ES] = "LIMIT long {0} {1} lote",
                [Language.IT] = "LIMIT long {0} {1} lotto",
                [Language.PL] = "LIMIT long {0} {1} lot",
                [Language.NL] = "LIMIT long {0} {1} lot",
                [Language.PT] = "LIMIT long {0} {1} lote"
            },
            ["LimitShort"] = new Dictionary<Language, string>
            {
                [Language.EN] = "LIMIT short {0} {1} lot",
                [Language.RU] = "LIMIT short {0} {1} лот",
                [Language.DE] = "LIMIT short {0} {1} Lot",
                [Language.FR] = "LIMIT short {0} {1} lot",
                [Language.ES] = "LIMIT short {0} {1} lote",
                [Language.IT] = "LIMIT short {0} {1} lotto",
                [Language.PL] = "LIMIT short {0} {1} lot",
                [Language.NL] = "LIMIT short {0} {1} lot",
                [Language.PT] = "LIMIT short {0} {1} lote"
            },
            ["BuyMarket"] = new Dictionary<Language, string>
            {
                [Language.EN] = "BUY {0} {1} lot",
                [Language.RU] = "BUY {0} {1} лот",
                [Language.DE] = "BUY {0} {1} Lot",
                [Language.FR] = "BUY {0} {1} lot",
                [Language.ES] = "BUY {0} {1} lote",
                [Language.IT] = "BUY {0} {1} lotto",
                [Language.PL] = "BUY {0} {1} lot",
                [Language.NL] = "BUY {0} {1} lot",
                [Language.PT] = "BUY {0} {1} lote"
            },
            ["SellMarket"] = new Dictionary<Language, string>
            {
                [Language.EN] = "SELL {0} {1} lot",
                [Language.RU] = "SELL {0} {1} лот",
                [Language.DE] = "SELL {0} {1} Lot",
                [Language.FR] = "SELL {0} {1} lot",
                [Language.ES] = "SELL {0} {1} lote",
                [Language.IT] = "SELL {0} {1} lotto",
                [Language.PL] = "SELL {0} {1} lot",
                [Language.NL] = "SELL {0} {1} lot",
                [Language.PT] = "SELL {0} {1} lote"
            },

            // --- Тексты на линиях графика (с параметрами) ---
            ["StopText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "SL {0}% · {1}",
                [Language.RU] = "SL {0}% · {1}",
                [Language.DE] = "SL {0}% · {1}",
                [Language.FR] = "SL {0}% · {1}",
                [Language.ES] = "SL {0}% · {1}",
                [Language.IT] = "SL {0}% · {1}",
                [Language.PL] = "SL {0}% · {1}",
                [Language.NL] = "SL {0}% · {1}",
                [Language.PT] = "SL {0}% · {1}"
            },
            // Средняя точка (·) между RR и суммой — как в StopText между % и $ (визуально по центру строки, не baseline «.»).
            ["TpText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "TP RR {0} · {1}",
                [Language.RU] = "TP RR {0} · {1}",
                [Language.DE] = "TP RR {0} · {1}",
                [Language.FR] = "TP RR {0} · {1}",
                [Language.ES] = "TP RR {0} · {1}",
                [Language.IT] = "TP RR {0} · {1}",
                [Language.PL] = "TP RR {0} · {1}",
                [Language.NL] = "TP RR {0} · {1}",
                [Language.PT] = "TP RR {0} · {1}"
            },
            ["LimitText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "LIMIT {0} lot",
                [Language.RU] = "LIMIT {0} лот",
                [Language.DE] = "LIMIT {0} Lot",
                [Language.FR] = "LIMIT {0} lot",
                [Language.ES] = "LIMIT {0} lote",
                [Language.IT] = "LIMIT {0} lotto",
                [Language.PL] = "LIMIT {0} lot",
                [Language.NL] = "LIMIT {0} lot",
                [Language.PT] = "LIMIT {0} lote"
            },
            ["MarketText"] = new Dictionary<Language, string>
            {
                [Language.EN] = "MKT {0} lot",
                [Language.RU] = "МКТ {0} лот",
                [Language.DE] = "MKT {0} Lot",
                [Language.FR] = "MKT {0} lot",
                [Language.ES] = "MKT {0} lote",
                [Language.IT] = "MKT {0} lot",
                [Language.PL] = "MKT {0} lot",
                [Language.NL] = "MKT {0} lot",
                [Language.PT] = "MKT {0} lote"
            },

            // --- Панель настроек ---
            ["Settings"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Settings",
                [Language.RU] = "Настройки",
                [Language.DE] = "Einstellungen",
                [Language.FR] = "Paramètres",
                [Language.ES] = "Ajustes",
                [Language.IT] = "Impostazioni",
                [Language.PL] = "Ustawienia",
                [Language.NL] = "Instellingen",
                [Language.PT] = "Definições"
            },
            ["SettingsPlaceholder"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Other settings will be here",
                [Language.RU] = "Здесь будут другие настройки",
                [Language.DE] = "Weitere Einstellungen hier",
                [Language.FR] = "Autres réglages ici",
                [Language.ES] = "Más ajustes aquí",
                [Language.IT] = "Altre impostazioni qui",
                [Language.PL] = "Inne ustawienia tutaj",
                [Language.NL] = "Overige instellingen hier",
                [Language.PT] = "Outras definições aqui"
            },

            // --- Multi-TP настройки ---
            ["TpCountLabel"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Take profits",
                [Language.RU] = "Тейков",
                [Language.DE] = "Take Profits",
                [Language.FR] = "Take Profits",
                [Language.ES] = "Take Profits",
                [Language.IT] = "Take Profits",
                [Language.PL] = "Take Profits",
                [Language.NL] = "Take Profits",
                [Language.PT] = "Take Profits"
            },
            ["TpCount1"] = new Dictionary<Language, string>
            {
                [Language.EN] = "1",
                [Language.RU] = "1",
                [Language.DE] = "1",
                [Language.FR] = "1",
                [Language.ES] = "1",
                [Language.IT] = "1",
                [Language.PL] = "1",
                [Language.NL] = "1",
                [Language.PT] = "1"
            },
            ["TpCount2"] = new Dictionary<Language, string>
            {
                [Language.EN] = "2",
                [Language.RU] = "2",
                [Language.DE] = "2",
                [Language.FR] = "2",
                [Language.ES] = "2",
                [Language.IT] = "2",
                [Language.PL] = "2",
                [Language.NL] = "2",
                [Language.PT] = "2"
            },
            ["TpCount3"] = new Dictionary<Language, string>
            {
                [Language.EN] = "3",
                [Language.RU] = "3",
                [Language.DE] = "3",
                [Language.FR] = "3",
                [Language.ES] = "3",
                [Language.IT] = "3",
                [Language.PL] = "3",
                [Language.NL] = "3",
                [Language.PT] = "3"
            },
            ["TpVolumeModeLabel"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Volume mode",
                [Language.RU] = "Режим объёма",
                [Language.DE] = "Volumenmodus",
                [Language.FR] = "Mode volume",
                [Language.ES] = "Modo volumen",
                [Language.IT] = "Modalità volume",
                [Language.PL] = "Tryb wolumenu",
                [Language.NL] = "Volumemodus",
                [Language.PT] = "Modo volume"
            },
            ["PanelTransparencyLabel"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Panel transparency",
                [Language.RU] = "Прозрачность панели",
                [Language.DE] = "Panel-Transparenz",
                [Language.FR] = "Transparence du panneau",
                [Language.ES] = "Transparencia del panel",
                [Language.IT] = "Trasparenza pannello",
                [Language.PL] = "Przezroczystość panelu",
                [Language.NL] = "Paneeltransparantie",
                [Language.PT] = "Transparência do painel"
            },
            ["TpVolumeEqualVolume"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Equal volume",
                [Language.RU] = "Равный объём",
                [Language.DE] = "Gleiches Vol.",
                [Language.FR] = "Vol. égal",
                [Language.ES] = "Vol. igual",
                [Language.IT] = "Vol. uguale",
                [Language.PL] = "Równy wolumen",
                [Language.NL] = "Gelijk vol.",
                [Language.PT] = "Vol. igual"
            },
            ["TpVolumeEqualProfit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "Equal profit",
                [Language.RU] = "Равный профит",
                [Language.DE] = "Gleicher Gewinn",
                [Language.FR] = "Profit égal",
                [Language.ES] = "Beneficio igual",
                [Language.IT] = "Profitto uguale",
                [Language.PL] = "Równy zysk",
                [Language.NL] = "Gelijke winst",
                [Language.PT] = "Lucro igual"
            },
            // Суммарная прибыль при нескольких TP: {0} $, {1} % баланса, {2} число тейков (2/3), {3} краткая метка режима
            ["TpInfoMulti"] = new Dictionary<Language, string>
            {
                [Language.EN] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.RU] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.DE] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.FR] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.ES] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.IT] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.PL] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.NL] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}",
                [Language.PT] = "{0:F2}$ ({1:F2}%) · Σ {2} TP · {3}"
            },
            ["TpAllocShortEqualVolume"] = new Dictionary<Language, string>
            {
                [Language.EN] = "eq.vol",
                [Language.RU] = "равн.объём",
                [Language.DE] = "gl.Vol.",
                [Language.FR] = "éq.vol",
                [Language.ES] = "eq.vol",
                [Language.IT] = "eq.vol",
                [Language.PL] = "rów.wol.",
                [Language.NL] = "eq.vol",
                [Language.PT] = "eq.vol"
            },
            ["TpAllocShortEqualProfit"] = new Dictionary<Language, string>
            {
                [Language.EN] = "eq.pr.",
                [Language.RU] = "равн.профит",
                [Language.DE] = "gl.Gw.",
                [Language.FR] = "éq.pr.",
                [Language.ES] = "eq.pr.",
                [Language.IT] = "eq.pr.",
                [Language.PL] = "rów.zysk",
                [Language.NL] = "eq.pr.",
                [Language.PT] = "eq.pr."
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
        /// Если для выбранного языка нет перевода — возвращает английскую строку (fallback).
        /// </summary>
        public static string Get(string key)
        {
            if (Strings.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_lang, out var value))
                    return value;
                if (translations.TryGetValue(Language.EN, out var enValue))
                    return enValue;
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
