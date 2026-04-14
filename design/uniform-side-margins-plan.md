# Uniform Side Margins — Step-by-Step Fix Plan

## Problem

Visual left and right margins of content are **not equal** across the 12 modules of `_contentStack`.

### Root cause analysis (at 100% scale, `PanelWidth = 228px`)

| Row | Available width (after row margin) | Content actually occupies | Right gap |
|-----|-----------------------------------|--------------------------|-----------|
| `checkboxRow` | 224px | auto (StackPanel, no stretch) | unkn. |
| `modeRow` | 224px | 2×halfWidth + button margins | 0 ✓ |
| `riskRow` | 224px | combo(90) + textbox(92) + unit(~18) ≈ 200 | ~24px ❌ |
| `_priceTextBox` | **220px** (margin L=4, R=4) | stretch | — |
| `slTpRow` | 224px | 2 × colWidth(106) = **212px** | **12px** ❌ |
| `_submitButton` | 224px | stretch | 0 ✓ |

**Three specific culprits:**

1. **`slTpRow` — gap 12px on the right.**  
   `colWidth = (PanelWidth − S(16)) / 2 = 106px`.  
   Two columns: `106 + 106 = 212px` in a `224px` row → 12px empty space on the right of the TP column.  
   Root: the magic constant `S(16)` over-subtracts. The row's own side margins are only `S(2+2) = S(4)`.

2. **`_priceTextBox` — asymmetric margin.**  
   Margin set in constructor: `ST(4, 2, 4, 2)` → L=4, R=4.  
   All other textboxes use `ST(2)` via `ApplyTextBoxStyle`. This makes the price field 4px narrower on each side than its neighbours.

3. **`riskRow` innerWidth — same over-subtraction.**  
   `innerWidth = PanelWidth − S(16) = 212px` instead of `PanelWidth − S(4) = 224px`.  
   The risk textbox is 12px narrower than it should be, leaving space on the right after the "%" unit label.

---

## Target state

All rows have effective side margins **Left = 2, Right = 2 (scaled)** from panel inner edge to visible content boundary.

---

## Steps

### Step 1 — Fix `slTpRow` column width calculation

**File:** `cop v1/UI/MainPanel.cs`, line ~400

**Change:**
```csharp
// BEFORE
double colWidth = (PanelStyles.PanelWidth - PanelStyles.S(16)) / 2;

// AFTER
double colWidth = (PanelStyles.PanelWidth - PanelStyles.S(4)) / 2;
```

`S(4)` = sum of `slTpRow` own side margins (`S(2)` left + `S(2)` right).  
New `colWidth = (228 − 4) / 2 = 112px`. Columns: `112 + 112 = 224px = available`. ✓

**Verify:**
```
dotnet build "cop v1/cop v1.csproj"
```
→ Load in cTrader → Check that **Stop Loss** and **Take Profit** columns each fill exactly half the panel width; right edge of "0.00$ (0.00%)" under TP aligns with the right edge of other rows.

---

### Step 2 — Fix `_priceTextBox` constructor margin

**File:** `cop v1/UI/MainPanel.cs`, line ~392

**Change:**
```csharp
// BEFORE
Margin = PanelStyles.ST(4, 2, 4, 2),

// AFTER
Margin = PanelStyles.ST(2, 2, 2, 2),
```

This aligns the price input field with all other textboxes which use `ST(2)` from `ApplyTextBoxStyle`.

> Note: `ApplyTextBoxStyle` is called afterwards and would set `ST(2)` anyway — but the explicit constructor value currently overrides it. After this fix, both are ST(2) and consistent.

**Verify:**
```
dotnet build "cop v1/cop v1.csproj"
```
→ Load in cTrader → Check that **Limit Order** textbox has the same visual left and right padding as the SL/TP textboxes.

---

### Step 3 — Fix `riskRow` inner width calculation

**File:** `cop v1/UI/MainPanel.cs`, line ~316

**Change:**
```csharp
// BEFORE
double innerWidth = PanelStyles.PanelWidth - PanelStyles.S(16);

// AFTER
double innerWidth = PanelStyles.PanelWidth - PanelStyles.S(4);
```

`S(4)` = `riskRow` own side margins (`S(2)` + `S(2)`).  
New `innerWidth = 224px`. The risk textbox width becomes:  
`224 − 84(combo) − S(28) − S(12) = 100px` (was 88px).  
This stretches the risk input field to fill the row correctly so the "%" unit sits flush with the right margin.

**Verify:**
```
dotnet build "cop v1/cop v1.csproj"
```
→ Load in cTrader → Check that the **risk textbox** ("0.50") fills the space between the combo and "%" so the "%" label lands near the right edge (≈ same distance as the left edge of "Risk" label).

---

### Step 4 — Full visual audit

After all three fixes, load the `.algo` in cTrader at **100% panel scale** and confirm visually:

| Row | Left visible gap | Right visible gap | Match? |
|-----|-----------------|------------------|--------|
| fast order / spread | ≈ same | ≈ same | ✓ |
| Limit / Market | ≈ same | ≈ same | ✓ |
| Risk label + controls | ≈ same | ≈ same | ✓ |
| Limit Order label + field | ≈ same | ≈ same | ✓ |
| Stop Loss / Take Profit | ≈ same | ≈ same | ✓ |
| Place order button | ≈ same | ≈ same | ✓ |

Also test at **80% and 150% UI scale** — proportional `S()` scaling ensures the fix remains valid at all scales.

---

## Architecture note (standard approach)

The WPF/MAUI standard for consistent indentation is to apply **padding on the container** rather than margin on each child:

```csharp
_contentStack.Margin = new Thickness(SideInset, 0, SideInset, 0);
// children then use Margin = ST(0, topBottom, 0, topBottom)
```

This guarantees every row is constrained by the same horizontal bounds with a single value.  
The current implementation uses per-row margins instead. The three fixes above align the rows within that existing approach without architectural refactoring. If a future refactor is warranted, migrating to container-level padding is the cleaner long-term solution.

---

## Files changed

| File | Lines | Change |
|------|-------|--------|
| `cop v1/UI/MainPanel.cs` | ~400 | `colWidth`: `S(16)` → `S(4)` |
| `cop v1/UI/MainPanel.cs` | ~392 | `_priceTextBox` margin: `ST(4,2,4,2)` → `ST(2,2,2,2)` |
| `cop v1/UI/MainPanel.cs` | ~316 | `innerWidth`: `S(16)` → `S(4)` |
