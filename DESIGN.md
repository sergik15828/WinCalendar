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
  calendar-light-event-surface: "#FFFFFF"
  calendar-light-ink: "#202020"
  calendar-light-secondary: "#626262"
  calendar-light-adjacent: "#727272"
  calendar-light-hover: "#E8E8E8"
  calendar-light-weekend: "#A82330"
  calendar-light-rule: "#DEDEDE"
  calendar-dark-surface: "#202020"
  calendar-dark-event-surface: "#292929"
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
    fontSize: "17pt"
    fontWeight: 700
  calendar-month:
    fontFamily: "Segoe UI"
    fontSize: "11.5pt"
    fontWeight: 700
  calendar-body:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 400
  calendar-weekday:
    fontFamily: "Segoe UI"
    fontSize: "9pt"
    fontWeight: 400
  calendar-event-title:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 700
  calendar-event-detail:
    fontFamily: "Segoe UI"
    fontSize: "9pt"
    fontWeight: 400
rounded:
  icon: "6px"
  calendar-event-card: "8px"
spacing:
  calendar-inset: "16px"
  calendar-week-row: "32px"
  calendar-event-row: "92px"
  calendar-event-inset: "12px"
components:
  date-icon:
    backgroundColor: "{colors.paper}"
    textColor: "{colors.day-text}"
    rounded: "{rounded.icon}"
  calendar-light:
    backgroundColor: "{colors.calendar-light-surface}"
    textColor: "{colors.calendar-light-ink}"
    width: "688px"
    height: "380px"
  calendar-dark:
    backgroundColor: "{colors.calendar-dark-surface}"
    textColor: "{colors.calendar-dark-ink}"
    width: "688px"
    height: "380px"
  event-card-light:
    backgroundColor: "{colors.calendar-light-event-surface}"
    textColor: "{colors.calendar-light-ink}"
    rounded: "{rounded.calendar-event-card}"
  event-card-dark:
    backgroundColor: "{colors.calendar-dark-event-surface}"
    textColor: "{colors.calendar-dark-ink}"
    rounded: "{rounded.calendar-event-card}"
---

# Design System: WinCalendar

## Overview

The date icon remains the product's primary surface: red weekday strip, white paper, large day number, and Segoe UI. Clicking it toggles a compact two-pane calendar: month navigation on the left, clearly separated meeting cards on the right. The approved wider layout preserves the icon's identity and restrained theme-aware surfaces while making meetings easier to scan.

Background startup keeps the native host minimized and opens neither popup nor browser. Once pinned, the launcher remains visible while the host removes its own taskbar button. The calendar must preserve this absence of a running-app indicator.

**Key Characteristics:**

- Calendar left, meetings right, with a fine vertical divider.
- Compact type hierarchy and distinct event time, title and source-calendar roles.
- Settings and OAuth editing stay inside one reusable native tool window.
- A quiet reminder accent persists until the user clicks the date.

## Colors

The frontmatter preserves the icon palette and adds the implemented calendar theme tokens. The icon retains its opaque paper and transparent surroundings in both Windows themes.

The light popup uses the weekday-strip red for today, selection outlines, event dots, event times, the Today label and actions needing attention. White weekday-text becomes the foreground on accent fills. Its neutral surface, primary ink, secondary labels, muted adjacent-month dates, hover fill, weekend ink, and divider use the calendar-light tokens. White event surfaces distinguish individual cards from the surrounding panel.

The dark popup uses calendar-dark-accent with dark-surface ink on filled controls. Slightly lighter event surfaces separate cards from the main panel; borders use the rule token. Other roles use the corresponding calendar-dark tokens. Windows High Contrast replaces the popup palette with system window, text, highlight, highlight-text, and control colors.

## Typography

Use Segoe UI throughout. The icon's bold glyphs fit their actual contours rather than ordinary text padding. The popup hierarchy is today's date or page heading (calendar-heading), displayed month and Встречи heading (calendar-month), day numbers and settings actions (calendar-body), and weekday, selected-date and status labels (calendar-weekday). Cards use bold calendar-event-title text beneath an accent-colored calendar-event-detail time; their source calendar uses the same detail size in secondary ink. Calendar labels and full accessible date names are Russian; event titles retain their source text.

## Layout

The popup's natural size is 688 × 380 logical pixels at 96 DPI. This approved two-pane arrangement replaces the former tall layout. A fine divider at x=344 separates the calendar from the agenda. Both panes use 16px outer insets; the agenda starts at x=360 and is 312px wide. The seven-column, Monday-first calendar retains six week rows, now at a compact 32px pitch.

Today's date and weekday sit above the calendar. Today and the month arrows share the month-heading row. The right pane has a Встречи heading, a selected-date/count subtitle and an independently scrollable event list at (360,86), sized 312 × 278 logical pixels. The gear at (640,16) opens settings in the same footprint. Connection actions, sync status, browser launch, startup, reminder preview and Exit live behind that gear; the calendar view has no footer-control strip.

Calendar, settings and OAuth editor share the same native window and natural size. Bounds, fonts and paint scale together with its actual DPI: requested 120 and 144 DPI dimensions are 860 × 475 and 1032 × 570 physical pixels. Anchor the flyout to the pinned date when located, otherwise near the pointer, and constrain it to the monitor's working area. On a smaller work area, retain legible text and allow scrolling.

## Elevation & Depth

The native borderless Form supplies the flyout's shell shadow and requests Windows rounded corners. A fine vertical rule separates the two panes. Event cards use a distinct surface and thin border without shadows. Hover fill, today's solid circle, the selected date's outline and visible focus cues communicate interaction without stacking decorative depth.

## Shapes

Keep the icon's softly rounded paper (icon radius). The calendar uses a native flyout, flat settings actions, simple chevrons, circular day highlights and raised event dots. Event cards have gently rounded corners (calendar-event-card radius) and remain separate within the borderless scrolling list. A selected or hovered card uses neutral hover fill. The gear becomes a close symbol while settings are open; keyboard focus remains visible on all custom controls.

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

The calendar is a reusable native Form with WS_EX_TOOLWINDOW and without WS_EX_APPWINDOW. Calendar, settings and OAuth editing share that one window handle; nested dropdowns and modal setup windows are obsolete. Outside activation or another date-icon command hides the flyout. Escape returns from settings or the editor before dismissing the calendar. Reopening the calendar returns selection and displayed month to today; an unfinished editor retains its input across hiding and reopening. The date header continues to show today while the user browses other dates.

Previous/next arrows switch months; Page Up/Page Down do the same. The grid always starts on Monday and includes muted adjacent-month dates. Today has a filled accent circle; another selected date has an accent outline. Hover adds a neutral circle, and weekends retain a distinct text color. Arrow keys move day selection; Home or the Сегодня button returns to today. Selecting a date does not change the taskbar icon.

The gear opens a two-column settings page in place. Actions are flat, left-aligned rows with neutral hover fills; sign-in when needed and disconnect use accent ink. A small accent dot on the gear signals refresh failure or renewed-sign-in requirements. Открыть Google Calendar hides the flyout before opening the default browser. Startup, reminder preview and Exit remain available in settings, also reached through Settings.cmd or the Start menu settings shortcut.

### Google events

A small dot below a day number signals events from the account's readable calendars. It is raised clear of the cell's lower edge and uses accent ink, or on-accent ink inside today's filled circle. The selected-date heading adds the event count when nonzero. Selection updates the right pane without changing today's header or taskbar date.

Event rows are 92 logical pixels high. Each card has 12px horizontal padding: a 9pt accent time or all-day label begins 7px below the card top, a 10pt bold title begins at 27px and wraps within a 34px two-line area, and 9pt source-calendar metadata begins at 64px. Long titles end with an ellipsis; the tooltip exposes full time, title and calendar. Click or Enter opens a usable Google Calendar event link and hides the flyout; unavailable links use secondary title ink. Times follow the Windows time zone. Refresh preserves the selected card and scroll position when its event remains present.

Connection state updates the event area and settings actions without rearranging the two panes:

| State | Event area and status | Actions |
| --- | --- | --- |
| Disconnected | Invitation to connect Google calendars; month grid remains usable | Войти в Google in settings |
| Loading | Loading message, with any retained snapshot still available | Duplicate refresh disabled |
| Connected | Selected day's events, or an explicit no-meetings message for an empty day | Сменить аккаунт, Обновить, Отключить |
| Offline | Retained events with a no-connection message and last-update time | Existing connection actions remain available |
| Sign-in required | Sign-in or permission message; any retained snapshot is identified by its update time | Attention dot on gear; Войти в Google and Отключить; refresh disabled |
| Browser authorization pending | Instruction to finish sign-in in the browser | Отмена входа; refresh and disconnect disabled |

Refreshing stays asynchronous. Switching month or account must not allow a late response to replace the current view. Disconnecting clears the displayed account data immediately before attempting remote revocation; its transient status reports progress or a failure to finish. Cached data must never appear as a newly confirmed empty calendar.

### Inline Google setup

Подключение replaces the calendar/settings content inside the same flyout. Client ID and password-masked Client secret fields occupy the left and right columns. A guide link, storage explanation and inline error text accompany neutral Назад and accent-filled Сохранить actions. Leaving the secret blank retains the stored secret for the same Client ID. The editor clears its fields on explicit back or successful save, while merely hiding the flyout preserves unfinished input. Repeated setup or sign-in commands reveal that existing editor. Background commands remain invisible.

Account authorization opens in the default browser. Reopening settings during authorization exposes pending status and Отмена входа. Setup instructions live in GOOGLE_SETUP.md; the older standalone setup-dialog captures describe a superseded surface.

### Reminder accent

Honor each Google event's popup-notification timing, including inherited calendar defaults and explicit overrides. A due reminder adds a small badge and a restrained breathing outline over the pinned date, with a 1.8-second cycle. The overlay passes clicks through, never activates, and adds no taskbar button. It does not rewrite the ICO or invalidate Explorer's icon cache.

Pending reminders persist in current-user DPAPI-encrypted state, including across restart, until the user clicks the date to acknowledge them. A later configured reminder can trigger again. Reduced-motion settings or High Contrast produce a static accent instead of animation; High Contrast uses the system highlight color. Проверить напоминание previews the same attention treatment until acknowledged.

### Verification evidence

All 201 native self-test checks pass, covering calendar state, keyboard controls, action dispatch, single-window navigation, tool-window styles, working-area placement and requested DPI geometry. Synthetic HTTP and UI tests cover multi-calendar loading, permission and refresh failures, cached offline data, account changes and event-link dispatch. Reminder tests cover notification defaults/overrides, timing, encrypted pending-state persistence, acknowledgement, click-through styles and absence of ICO rewrites.

Current light/dark captures at requested 96, 120 and 144 DPI are artifacts/google-events-{light,dark}-{96,120,144}.png, artifacts/calendar-settings-{light,dark}-{96,120,144}.png and artifacts/calendar-client-{light,dark}-{96,120,144}.png. They show the two-pane event view, inline settings and inline editor; fixture events are synthetic and editor fields are blank. artifacts/reminder-animation-frames.png illustrates the accent cycle. Requested-DPI renders do not establish real mixed-monitor transitions.

Installed checks in artifacts/VerifyCurrentUi.ps1 confirm one hidden startup process, retained pin and enabled startup, a real account's selected-day event retrieval, and the 860 × 475 flyout at the user's actual 120 DPI. Calendar, settings, editor and repeated setup use the same window handle without an app-window style. The real reminder preview in artifacts/installed-reminder-accent.png is centered on the pinned date; native checks confirm click-through/non-activating styles, unchanged foreground window and ICO timestamp, and a date click hiding the accent while opening the calendar. Future Google event delivery at its real wall-clock reminder time, mixed-monitor transitions and pinned-icon rollover after real midnight remain unverified.

## Do's and Don'ts

- **Do** preserve the established icon geometry, palette, large glyphs, and executable identity.
- **Do** keep theme contrast, distinct today/selection states, keyboard focus, and readable scaled labels.
- **Do** keep meetings to the right of the calendar with clearly separated cards and source-calendar labels.
- **Do** keep connection, startup and exit controls behind the gear inside the same flyout.
- **Do** keep background startup invisible and the popup transient.
- **Do** distinguish an empty day from loading, offline data and a request to sign in again.
- **Do** preserve pending reminder attention until a date click and respect Windows motion preferences.
- **Don't** add a separate taskbar button or running-app indicator for the popup.
- **Don't** animate by rewriting the pinned date icon or rebuild separate modal setup windows.
- **Don't** treat rendered DPI samples as proof of real mixed-monitor behavior or real-midnight rollover.
- **Don't** present synthetic events or authorization tests as a verified real-account connection.
