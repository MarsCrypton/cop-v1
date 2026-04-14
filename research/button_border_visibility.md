# Проблема: обводка кнопок не отображается при загрузке панели

## Описание проблемы

Кнопки **Limit**, **Market** и **Place Order** обёрнуты в `Border`-контейнеры с серой обводкой `#333333`.

**Наблюдаемое поведение:**
- Limit / Market: обводка **не видна при загрузке**, появляется только **после изменения масштаба** панели
- Place Order: обводка **не появляется никогда**

**Желаемое поведение:**
- Все три кнопки всегда имеют серую рамку

---

## Текущая реализация (в коде)

```csharp
var limitBorder = new Border
{
    Child           = _limitButton,
    BorderColor     = PanelStyles.PanelBorderColor,   // #333333
    BorderThickness = PanelStyles.ST(1),
    CornerRadius    = new CornerRadius(PanelStyles.ButtonCornerSubtle),
    Margin          = PanelStyles.ST(2)
};

var submitBorder = new Border
{
    Child           = _submitButton,
    BorderColor     = PanelStyles.PanelBorderColor,
    BorderThickness = PanelStyles.ST(1),
    CornerRadius    = new CornerRadius(PanelStyles.ButtonCornerSubtle),
    Margin          = PanelStyles.ST(2, 4, 2, 6)
};
```

---

## Предпринятые попытки

| # | Подход | Результат |
|---|--------|-----------|
| 1 | Border-обёртка без Background | ❌ Limit/Market: видна только после scale-change; Place Order: не видна никогда |
| 2 | `Background = Color.Transparent` на Border | ❌ Не компилируется — cTrader `Border` не имеет свойства Background |

---

## Вероятные причины / кандидаты на исправление (от наиболее вероятной)

### [B-1] ❌ Missing `Background` на Border
**Гипотеза:** cTrader не отрисовывает `BorderColor` без Background.
**Результат:** cTrader `Border` не имеет свойства `Background` — не компилируется.

---

### [B-2+B-4] 🔥 Текущая попытка — `Padding = ST(1)` + `HorizontalAlignment.Stretch`
**Гипотеза:** `FastOrderToggleBorder` (работающий образец) использует `Padding = ST(1)`. Без padding кнопка закрывает рамку своим фоном. Без HAlign.Stretch — Border не заполняет ячейку Grid.
**Исправление:** добавить `Padding = ST(1)` и `HorizontalAlignment.Stretch` на все три Border.
**Статус:** ✅ Применено — сборка 0 ошибок. Ожидает проверки в cTrader.

---

### [B-3] Вероятность ~50% — submitBorder не имеет Width / HAlignment
**Гипотеза:** `submitBorder` находится в `_contentStack` (StackPanel). Без явного `Width` или `HorizontalAlignment = Stretch` Border может иметь нулевую ширину → не виден.
**Исправление:** добавить `Width = PanelStyles.ContentWidth` на `submitBorder`.
**Статус:** ⬜ Не применено

---

### [B-3] Вероятность ~40% — UpdateSubmitButton / ResetToIdle меняют Margin кнопки
**Гипотеза:** Вызовы `ApplySubmitButtonStyle(_submitButton, ..., margin)` внутри `UpdateSubmitButton()` / `ResetToIdle()` могут устанавливать ненулевой Margin на кнопку, случайно смещая её за пределы Border.
**Исправление:** убедиться, что все вызовы `ApplySubmitButtonStyle(_submitButton, ...)` передают `PanelStyles.ST(0)` в качестве margin.
**Статус:** ⬜ Требует проверки

---

### [B-4] Вероятность ~30% — limitBorder/marketBorder не заполняют ячейку Grid
**Гипотеза:** Border внутри ячейки Grid без явного `HorizontalAlignment = Stretch` не растягивается на всю ячейку → кнопка отображается, но малая рамка не видна из-за несоответствия размеров.
**Исправление:** добавить `HorizontalAlignment = HorizontalAlignment.Stretch` на limitBorder / marketBorder.
**Статус:** ⬜ Не применено

---

### [B-5] Вероятность ~20% — CornerRadius скрывает угловые пиксели рамки
**Гипотеза:** При маленьком BorderThickness (1px) и скруглённых углах рамка может "срезаться" до невидимости.
**Исправление:** увеличить BorderThickness до ST(2) или убрать CornerRadius у Border.
**Статус:** ⬜ Не применено

---

### [B-6] Вероятность ~15% — Альтернатива: имитация рамки через BackgroundColor
**Гипотеза:** Использование `BackgroundColor` на самой кнопке (чуть светлее/темнее фона панели) создаёт визуальную "подложку" без использования Border. Не настоящая рамка, но выглядит как выделение.
**Исправление:** убрать Border, установить `BackgroundColor` кнопки в сдвинутый оттенок.
**Статус:** ⬜ Резервный вариант

---

## План итеративного тестирования

1. Применить [B-1]: добавить `Background = Color.Transparent` на все три Border → спросить пользователя
2. Если не помогло → применить [B-2]: `Width = PanelStyles.ContentWidth` на submitBorder
3. Если не помогло → применить [B-3]: проверить UpdateSubmitButton / ResetToIdle
4. Если не помогло → применить [B-4]: HAlignment.Stretch на limitBorder / marketBorder
5. Если не помогло → применить [B-5]: увеличить BorderThickness
6. Если не помогло → перейти к [B-6]: не Border, а BackgroundColor

---

## Ссылки на код

- Limit/Market кнопки: [MainPanel.cs:352–393](../cop%20v1/UI/MainPanel.cs#L352)
- Submit кнопка: [MainPanel.cs:554–586](../cop%20v1/UI/MainPanel.cs#L554)
- UpdateSubmitButton / ResetToIdle: grep по `ApplySubmitButtonStyle` в MainPanel.cs
