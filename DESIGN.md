---
name: WinCalendar
description: Today's date on the Windows 11 taskbar with a compact local calendar popup.
colors:
  weekday-strip: "#C42B36"
  weekday-text: "#FFFFFF"
  paper: "#FCFCFC"
  day-text: "#18181B"
  icon-border: "#D7D7D7"
  calendar-light-surface: "#FAFAFA"
  calendar-light-ink: "#202020"
  calendar-light-secondary: "#626262"
  calendar-light-adjacent: "#727272"
  calendar-light-hover: "#E8E8E8"
  calendar-light-weekend: "#A82330"
  calendar-light-rule: "#DEDEDE"
  calendar-dark-surface: "#202020"
  calendar-dark-ink: "#F5F5F5"
  calendar-dark-secondary: "#BDBDBD"
  calendar-dark-adjacent: "#A0A0A0"
  calendar-dark-accent: "#FF8B91"
  calendar-dark-hover: "#383838"
  calendar-dark-weekend: "#FFB6BB"
  calendar-dark-rule: "#444444"
typography:
  icon-weekday:
    fontFamily: "Segoe UI"
    inkHeight: "20px"
    fontWeight: 700
  icon-day:
    fontFamily: "Segoe UI"
    inkHeight: "30px"
    fontWeight: 700
  calendar-heading:
    fontFamily: "Segoe UI"
    fontSize: "20pt"
    fontWeight: 700
  calendar-month:
    fontFamily: "Segoe UI"
    fontSize: "12.5pt"
    fontWeight: 700
  calendar-body:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 400
  calendar-weekday:
    fontFamily: "Segoe UI"
    fontSize: "9pt"
    fontWeight: 400
rounded:
  icon: "6px"
spacing:
  calendar-inset: "16px"
  calendar-week-row: "40px"
components:
  date-icon:
    backgroundColor: "{colors.paper}"
    textColor: "{colors.day-text}"
    rounded: "{rounded.icon}"
  calendar-light:
    backgroundColor: "{colors.calendar-light-surface}"
    textColor: "{colors.calendar-light-ink}"
    width: "344px"
    height: "468px"
  calendar-dark:
    backgroundColor: "{colors.calendar-dark-surface}"
    textColor: "{colors.calendar-dark-ink}"
    width: "344px"
    height: "468px"
---

# Design System: WinCalendar

## Overview

The date icon remains the product's primary surface: red weekday strip, white paper, large day number, and Segoe UI. Clicking it toggles a precise, compact local month calendar. The popup extends the existing visual system with restrained theme-aware surfaces and the same red accent.

Background startup keeps the native host minimized and opens neither popup nor browser. Once pinned, the launcher remains visible while the host removes its own taskbar button. The calendar must preserve this absence of a running-app indicator.

## Colors

The frontmatter preserves the icon palette and adds the implemented calendar theme tokens. The icon retains its opaque paper and transparent surroundings in both Windows themes.

The light popup uses the weekday-strip red for today, selection outlines, the Today label, and the Google Calendar button; white weekday-text becomes the foreground on accent fills. Its neutral surface, primary ink, secondary labels, muted adjacent-month dates, hover fill, weekend ink, and divider use the calendar-light tokens.

The dark popup uses calendar-dark-accent with dark-surface ink on filled controls. Its remaining roles use the corresponding calendar-dark tokens. Windows High Contrast replaces the popup palette with system window, text, highlight, highlight-text, and control colors.

## Typography

Use Segoe UI throughout. The icon's bold glyphs fit their actual contours rather than ordinary text padding. The popup hierarchy is today's date (calendar-heading), the displayed month (calendar-month), day numbers and action labels (calendar-body), and Monday-first weekday labels (calendar-weekday). All calendar labels and full accessible date names are Russian.

## Layout

The popup's natural size is fixed at 344 × 468 logical pixels at 96 DPI, with a 16px horizontal inset. Today's date and weekday lead; the displayed month and two arrows precede a seven-column, six-week grid. Week rows are 40px high. A divider separates the grid from the bottom action row.

Bounds, fonts, and painted controls scale together with the popup's window DPI: the requested 120 and 144 DPI sizes are 430 × 585 and 516 × 702 physical pixels. Place the popup near the pointer, generally above it, and keep it within that monitor's working area. If the working area is smaller than the natural panel, constrain the viewport and allow scrolling rather than shrinking the text.

## Elevation & Depth

The native ToolStripDropDown supplies the popup shadow. Interior surfaces remain flat; hover fill, today's solid circle, and the selected date's outline communicate state. Avoid decorative cards or additional shadow layers within this small surface.

## Shapes

Keep the icon's softly rounded paper (icon radius). The calendar uses a rectangular native popup, flat rectangular footer actions, simple chevrons, and circular day highlights. Keyboard focus remains visible on day and arrow controls.

## Components

### Date icon

Render the icon independently at 16, 20, 24, 32, 40, 48, 64 and 128 pixels. Coordinates below refer to the 64px drawing plane.

- White paper at (1,1), 62x62px, corner radius 6px, 1px neutral outline.
- Red strip at (0,0), 64x27px, clipped to the paper shape.
- Fit the actual glyph contours of the two-letter Russian weekday into (5,3,54,20), in white Segoe UI Bold. This removes the normal font padding.
- Fit the day glyph contours into (4,30,56,30), in near-black Segoe UI Bold.
- Keep the background transparent and the icon's own paper opaque so it works on light and dark taskbars.
- The full local date appears as the taskbar title for accessibility and hover text.

Update the icon at local midnight, after a time change, after sleep, and on repeated launch. Retry transient write failures. Keep one stable ICO path and notify Explorer of affected own shortcuts. Use the native executable identity and an ordinary Start menu shortcut. Do not override AppUserModelID or window relaunch properties. Keep the installed executable name WinDate.exe stable for startup and pinned shortcuts. Every shipping icon frame uses native 32-bit DIB.

### Calendar popup

The native ToolStripDropDown is an auto-closing tool window, with WS_EX_TOOLWINDOW and without WS_EX_APPWINDOW. Escape, an outside click, focus loss, or another icon command dismisses it. Opening it returns selection and displayed month to today. The header continues to show today's date while the user browses other dates.

Previous/next arrows switch months; Page Up/Page Down do the same. The grid always starts on Monday and includes muted adjacent-month dates. Today has a filled accent circle; another selected date has an accent outline. Hover adds a neutral circle, and weekends retain a distinct text color. Arrow keys move day selection; Home or the Сегодня button returns to today. Selecting a date does not change the taskbar icon.

The Открыть Google Calendar button uses a solid accent fill and closes the popup before opening Google Calendar in the default browser. Settings.cmd and the Start menu settings shortcut retain the separate native startup/exit menu.

### Verification evidence

The native self-test report covers calendar state, keyboard controls, action dispatch, tool-window styles, working-area placement, and requested DPI geometry. The light and dark captures at 96, 120, and 144 DPI in artifacts/calendar-{light,dark}*.png show the implemented hierarchy and unclipped controls. These are requested-DPI renders; they do not establish correct transitions between real monitors with different scaling.

The installed popup was also captured at the user's actual 120 DPI. artifacts/taskbar-calendar-open.png confirms the weekday/day icon remains visible without an underline while the popup is open. The installed startup shortcut was exercised: one hidden process, no popup, preserved pin and enabled startup; repeat launches opened and dismissed the popup in that same process. Launch through the real Explorer broker; a temporary PowerShell parent previously reproduced a generic taskbar icon after it exited. Actual pinned-icon rollover after real midnight remains unverified.

## Do's and Don'ts

- **Do** preserve the established icon geometry, palette, large glyphs, and executable identity.
- **Do** keep theme contrast, distinct today/selection states, keyboard focus, and readable scaled labels.
- **Do** keep background startup invisible and the popup transient.
- **Don't** add a separate taskbar button or running-app indicator for the popup.
- **Don't** treat rendered DPI samples as proof of real mixed-monitor behavior or real-midnight rollover.
