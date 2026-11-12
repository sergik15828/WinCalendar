---
name: WinCalendar
description: Today's date on the Windows 11 taskbar with a compact calendar and optional Google events.
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
  calendar-event-row: "54px"
components:
  date-icon:
    backgroundColor: "{colors.paper}"
    textColor: "{colors.day-text}"
    rounded: "{rounded.icon}"
  calendar-light:
    backgroundColor: "{colors.calendar-light-surface}"
    textColor: "{colors.calendar-light-ink}"
    width: "344px"
    height: "672px"
  calendar-dark:
    backgroundColor: "{colors.calendar-dark-surface}"
    textColor: "{colors.calendar-dark-ink}"
    width: "344px"
    height: "672px"
---

# Design System: WinCalendar

## Overview

The date icon remains the product's primary surface: red weekday strip, white paper, large day number, and Segoe UI. Clicking it toggles a precise, compact month calendar with an optional selected-day list from the user's primary Google calendar. The popup extends the existing visual system with restrained theme-aware surfaces and the same red accent.

Background startup keeps the native host minimized and opens neither popup nor browser. Once pinned, the launcher remains visible while the host removes its own taskbar button. The calendar must preserve this absence of a running-app indicator.

## Colors

The frontmatter preserves the icon palette and adds the implemented calendar theme tokens. The icon retains its opaque paper and transparent surroundings in both Windows themes.

The light popup uses the weekday-strip red for today, selection outlines, event dots, connection actions, the Today label, and the Google Calendar button; white weekday-text becomes the foreground on accent fills. Its neutral surface, primary ink, secondary labels, muted adjacent-month dates, hover fill, weekend ink, and divider use the calendar-light tokens.

The dark popup uses calendar-dark-accent with dark-surface ink on filled controls. Its remaining roles use the corresponding calendar-dark tokens. Windows High Contrast replaces the popup palette with system window, text, highlight, highlight-text, and control colors.

## Typography

Use Segoe UI throughout. The icon's bold glyphs fit their actual contours rather than ordinary text padding. The popup hierarchy is today's date (calendar-heading), the displayed month (calendar-month), day numbers, selected date and event rows (calendar-body), and weekday labels, sync status and connection actions (calendar-weekday). Event times use secondary ink above their titles. Calendar labels and full accessible date names are Russian; event titles retain their source text.

## Layout

The popup's natural size is fixed at 344 × 672 logical pixels at 96 DPI, with a 16px horizontal inset. The earlier 344 × 468 size is obsolete: the events extension adds space below the unchanged month grid. Today's date and weekday lead; the displayed month and two arrows precede a seven-column, six-week grid. Week rows are 40px high.

Below the grid divider, the selected-date heading starts at y=406. Its scrollable event list starts at y=438 and is 312 × 104 logical pixels. Status and last-update text occupy y=546 and y=566, the three connection actions start at y=588, and the Today/Google footer starts at y=624 below a second divider. Keep this order consistent across loading, empty and account states.

Bounds, fonts, and painted controls scale together with the popup's window DPI: the requested 120 and 144 DPI sizes are 430 × 840 and 516 × 1008 physical pixels. Place the popup near the pointer, generally above it, and keep it within that monitor's working area. If the working area is smaller than the natural panel, constrain the viewport and allow scrolling rather than shrinking the text. The setup dialog independently scales its 472 × 374 logical client area and controls using its actual window DPI.

## Elevation & Depth

The native ToolStripDropDown supplies the popup shadow. Interior surfaces remain flat; hover fill, today's solid circle, and the selected date's outline communicate state. Avoid decorative cards or additional shadow layers within this small surface.

## Shapes

Keep the icon's softly rounded paper (icon radius). The calendar uses a rectangular native popup, flat rectangular actions, simple chevrons, circular day highlights, and small circular event markers. The event list has no enclosing border; a selected row uses neutral hover fill. Keyboard focus remains visible on days, arrows and event rows.

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

### Google events

A 3px logical dot below a day number signals available events. It uses accent ink, or on-accent ink inside today's filled circle. The selected-date heading adds the event count when nonzero. Selection updates the list without changing today's header or taskbar date.

Event rows are 54 logical pixels high, with a secondary time or all-day label above a single-line title. Long text ends with an ellipsis; a tooltip exposes the full time and title. The list scrolls independently. Click or Enter opens an event's Google Calendar browser link and dismisses the popup; rows without a usable link use secondary title ink. Times follow the Windows time zone.

Connection state changes the existing event area and action labels without rearranging the panel:

| State | Event area and status | Actions |
| --- | --- | --- |
| Disconnected | Invitation to connect the primary calendar; month grid remains usable | Войти в Google |
| Loading | Loading message, with any retained snapshot still available | Duplicate refresh disabled |
| Connected | Selected day's events, or an explicit no-meetings message for an empty day | Сменить аккаунт, Обновить, Отключить |
| Offline | Retained events with a no-connection message and last-update time | Existing connection actions remain available |
| Sign-in required | Expired or revoked access message; any retained snapshot is identified by its update time | Войти в Google and Отключить; refresh disabled |
| Browser authorization pending | Instruction to finish sign-in in the browser | Отмена входа; refresh and disconnect disabled |

Refreshing stays asynchronous. Switching month or account must not allow a late response to replace the current view. Disconnecting clears the displayed account data immediately before attempting remote revocation; its transient status reports progress or a failure to finish. Cached data must never appear as a newly confirmed empty calendar.

### Google setup

The first connection opens a native fixed tool window with no taskbar button. It reuses the calendar surface, text, hover and accent roles: two labeled fields, a setup-guide link, a muted storage explanation, neutral Отмена and accent Продолжить actions. The optional Client secret field is password-masked. The guide explains the user's Desktop OAuth client prerequisite; account authorization then opens in the default browser. Reopening the popup during authorization exposes the pending state and cancellation action. Setup instructions live in GOOGLE_SETUP.md.

### Verification evidence

The native self-test report covers calendar state, keyboard controls, action dispatch, tool-window styles, working-area placement, and requested DPI geometry. Synthetic HTTP and UI tests exercise event pagination, refresh and sign-in failures, cached offline data, account changes, event-link dispatch and rendered states. They do not establish successful authorization or data retrieval from a real Google account. That check awaits the user's Desktop client described in GOOGLE_SETUP.md.

The current event captures are artifacts/google-events-{light,dark}-{96,120,144}.png. Additional artifacts/google-events-{offline,empty,loading,signin,authorizing}.png show state treatments; artifacts/calendar-light.png shows the disconnected panel, and artifacts/google-setup.png shows setup at the actual monitor DPI. These captures show the expanded layout with unclipped controls; the event samples are synthetic. Requested-DPI renders do not establish correct transitions between real monitors with different scaling.

The current installed build is captured at the user's actual 120 DPI in artifacts/calendar-installed.png; artifacts/taskbar-google-calendar-open.png confirms that the date icon has no running underline while the popup is open. Native inspection confirms a visible tool window without an app-window style, one installed process, a preserved pin, and an enabled startup shortcut targeting the installed executable with --background. The Google extension was also exercised through that actual startup shortcut: one ready hidden host, then repeat launches opening and dismissing the popup in the same process. Launch through the real Explorer broker; a temporary PowerShell parent previously reproduced a generic taskbar icon after it exited. Actual pinned-icon rollover after real midnight remains unverified.

## Do's and Don'ts

- **Do** preserve the established icon geometry, palette, large glyphs, and executable identity.
- **Do** keep theme contrast, distinct today/selection states, keyboard focus, and readable scaled labels.
- **Do** keep background startup invisible and the popup transient.
- **Do** distinguish an empty day from loading, offline data and a request to sign in again.
- **Don't** add a separate taskbar button or running-app indicator for the popup.
- **Don't** treat rendered DPI samples as proof of real mixed-monitor behavior or real-midnight rollover.
- **Don't** present synthetic events or authorization tests as a verified real-account connection.
