# Winhance UI Patterns — Design Bar for the Redesign

Extracted 2026-10-04 from the real Winhance source at `../Winhance`
(read-only reference — never committed, never built, never copied into the repo).
All paths below are relative to the Winhance repo root.

## 1. Setting row anatomy (the core pattern)

Reference: `src/Winhance.UI/Features/Common/Resources/SettingTemplates.xaml`
(`SettingItemTemplate`, lines 123-201).

Each setting renders as ONE card with exactly three zones:

| Zone | Content | Rule |
|------|---------|------|
| Header | `Name` text only (e.g. "Game Mode"), optional "New" badge | Never paragraphs |
| Description | ONE caption line (`SettingDescriptionWithBadges.DescriptionText`), e.g. *"Optimize your PC for play by turning things off in the background"* | One sentence, plain gamer language, no registry paths |
| Badge pills | `BadgeRow`: Recommended / Default / Custom / Preference pills with tooltips, dimmed when not matching current state | Status at a glance, not sentences |
| Right side | Input control per `InputType`: ToggleSwitch, ComboBox, NumberBox, Action button, CheckBox | Live-apply on change, not tick-then-bulk |

Copy evidence (`src/Winhance.UI/Features/Common/Localization/en.json`):
- `Setting_gaming-game-mode_Name`: "Game Mode"
- `Setting_gaming-game-mode_Description`: "Optimize your PC for play by turning things off in the background"
- `Setting_gaming-xbox-game-dvr_Description`: "Record gameplay clips and take screenshots using the Xbox Game Bar overlay. Disabling reduces CPU/GPU usage and can improve frame rates"

## 2. Per-row Recommended / Default quick-set

Reference: `src/Winhance.UI/Features/Common/Controls/SettingsCardItem.xaml`
(`ToggleSettingTemplate`, lines 29-57; same pair repeats for every input type).

Every control is preceded by two small buttons: green "Set to Recommended"
(glyph E735) + grey "Set to Default", each with a tooltip naming the target
value. Data comes from the definition: `RecommendedToggleState`,
`DefaultToggleState`, per-registry `RecommendedValue` / `DefaultValue`
(`SettingItemViewModel.cs` lines 1810-1930).

Akari mapping: FR33THY "recommended vs stock" becomes a per-row one-click
pair, replacing the old bulk tick-then-apply model.

## 3. Technical-details expander (details on demand)

Reference: `SettingTemplates.xaml` lines 201-408; styles in
`src/Winhance.UI/Features/Common/Resources/TechnicalDetailsStyles.xaml`.

A slim bar attached below the card (info icon + "Technical details" + chevron),
collapsed by default. Expanding shows `TechnicalDetailSections` rows:
- Registry rows: path + value name/type (Consolas, selectable), Current /
  Recommended / Default values side by side, "Open in Registry Editor" button
- Scheduled-task rows, powercfg AC/DC rows, PowerShell/`.reg` code blocks,
  dependency chips

This is the "in detail like Winhance does it" contract: full transparency
without cluttering the scan path.

## 4. Page composition

Reference: `src/Winhance.UI/Features/Optimize/Pages/GamingOptimizePage.xaml`
(full file, 23 lines): a page is a `SettingsListView` over grouped settings
via `CollectionViewSource` (`IsSourceGrouped=True`), with `IsLoading` and
`HasNoSearchResults` states. Search filters across groups; section headers are
`BodyStrong` with wide top margin (`SettingsSectionHeaderTextBlockStyle`).

Parent settings with children render as expanders (chevron, click-to-expand,
`SettingExpanderItemTemplate`, lines 503-609); keyboard accessible
(Tab + Enter/Space), Narrator-named.

## 5. Status feedback per row

- Applying: smoke overlay + `ProgressRing` on the card (`IsApplying`)
- Compatibility / restart / value banners: `QuietInfoBar` strip attached below
  the card (`HasStatusBanner`), never a modal
- Locked (advanced) settings: overlay with click-to-unlock (`IsLocked`)

## 6. What we adopt (binding redesign constraints)

1. Row = title + ONE description line + badge pills + live control. No
   always-visible paragraph stacks (the exact failure of attempt 1).
2. Every tweak carries Recommended + Default values in its definition and
   exposes the per-row quick-set pair.
3. Every tweak carries technical details (registry footprint, current/
   recommended/default) behind a collapsed expander.
4. Toggle-first: checkboxes only where multi-select bulk-apply is genuinely
   needed; default to live-apply toggles.
5. Search + grouped sections + empty state, as before (that half worked).
6. Fully reversible line stays: restore point + per-tweak rollback; journal
   concept may return but must serve toggle semantics (revert per row to its
   Default), not the old bulk-prefix model.
