---
name: WinCalendar
description: Today's taskbar date, a compact calendar, and local controls for read-only Google meetings.
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
  calendar-preferences:
    fontFamily: "Segoe UI"
    fontSize: "9.5pt"
    fontWeight: 400
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
- A visible breathing reminder persists until acknowledged; local snooze defers its batch.
- Meet, the next meeting, seven-day agendas and local attention preferences extend the same visual system.

## Colors

The frontmatter preserves the icon palette and adds the implemented calendar theme tokens. The icon retains its opaque paper and transparent surroundings in both Windows themes.

The light popup uses the weekday-strip red for today, selection outlines, event dots, event times, the Today label and actions needing attention. White weekday-text becomes the foreground on accent fills. Its neutral surface, primary ink, secondary labels, muted adjacent-month dates, hover fill, weekend ink, and divider use the calendar-light tokens. White event surfaces distinguish individual cards from the surrounding panel.

The dark popup uses calendar-dark-accent with dark-surface ink on filled controls. Slightly lighter event surfaces separate cards from the main panel; borders use the rule token. Other roles use the corresponding calendar-dark tokens. Windows High Contrast replaces the popup palette with system window, text, highlight, highlight-text, and control colors.

## Typography

Use Segoe UI throughout. The icon's bold glyphs fit their actual contours rather than ordinary text padding. The popup hierarchy is today's date or page heading (calendar-heading), displayed month and Встречи heading (calendar-month), day numbers and settings actions (calendar-body), and weekday, selected-date and status labels (calendar-weekday). Cards use bold calendar-event-title text beneath an accent-colored calendar-event-detail time; their source calendar uses the same detail size in secondary ink. Preferences and the taskbar meeting tip use 9.5pt Segoe UI. Calendar labels and full accessible date names are Russian; event titles retain their source text unless private mode masks them.

## Layout

The popup's natural size is 688 × 380 logical pixels at 96 DPI. This approved two-pane arrangement replaces the former tall layout. A fine divider at x=344 separates the calendar from the agenda. Both panes use 16px outer insets; the agenda starts at x=360 and is 312px wide. The seven-column, Monday-first calendar retains six week rows, now at a compact 32px pitch.

Today's date and weekday sit above the calendar. Today and the month arrows share the month-heading row. The right pane has a Встречи heading, a selected-date/count subtitle and an independently scrollable event list at (360,86), sized 312 × 278 logical pixels. The gear at (600,16) opens settings in the same footprint; the separate close button at (640,16) hides the flyout while the background host keeps running. The Day/Week toggle sits at (580,46); the next-meeting label at (16,322) and contextual 5/10-minute snooze actions at y=348 use the space below the month grid. Connection actions, sync status, browser launch, preferences, startup, reminder preview and Exit live behind that gear; the calendar view has no footer-control strip.

Calendar, settings, three preference pages and OAuth editor share the same native window and natural size. Preferences have a heading, three 208px tabs on a 224px pitch at y=58, a two-column content area at (16,104), and Back/Save actions at y=330. Bounds, fonts and paint scale together with its actual DPI: requested 120 and 144 DPI dimensions are 860 × 475 and 1032 × 570 physical pixels. Anchor the flyout to the pinned date when located, otherwise near the pointer, and constrain it to the monitor's working area. On a smaller work area, retain legible text and allow scrolling.

## Elevation & Depth

The native borderless Form supplies the flyout's shell shadow and requests Windows rounded corners. A fine vertical rule separates the two panes. Event cards use a distinct surface and thin border without shadows. Hover fill, today's solid circle, the selected date's outline and visible focus cues communicate interaction without stacking decorative depth.

## Shapes

Keep the icon's softly rounded paper (icon radius). The calendar uses a native flyout, flat settings actions, simple chevrons, circular day highlights and raised event dots. Event cards have gently rounded corners (calendar-event-card radius) and remain separate within the borderless scrolling list. A selected or hovered card uses neutral hover fill. The gear remains distinct from the separate close button on both calendar and settings pages; keyboard focus remains visible on all custom controls.

## Components

### Date icon

Render the icon independently at 16, 20, 24, 32, 40, 48, 64 and 128 pixels. Coordinates below refer to the 64px drawing plane.

- White paper at (1,1), 62x62px, corner radius 6px, 1px neutral outline.
- Red strip at (0,0), 64x27px, clipped to the paper shape.
- Fit the actual glyph contours of the two-letter Russian weekday into (5,3,54,20), in white Segoe UI Bold. This removes the normal font padding.
- Fit the day glyph contours into (4,30,56,30), in near-black Segoe UI Bold.
- Keep the background transparent and the icon's own paper opaque so it works on light and dark taskbars.
- The full local date remains the taskbar title for accessibility and native hover text; an optional separate next-meeting tip is described below.

Update the icon at local midnight, after a time change, after sleep, and on repeated launch. Retry transient write failures. Keep one stable ICO path and notify Explorer of affected own shortcuts. Use the native executable identity and an ordinary Start menu shortcut. Do not override AppUserModelID or window relaunch properties. Keep the installed executable name WinDate.exe stable for startup and pinned shortcuts. Every shipping icon frame uses native 32-bit DIB.

### Calendar popup

The calendar is a reusable native Form with WS_EX_TOOLWINDOW and without WS_EX_APPWINDOW. Calendar, settings and OAuth editing share that one window handle; nested dropdowns and modal setup windows are obsolete. Outside activation or another date-icon command hides the flyout. Escape returns from settings or the editor before dismissing the calendar. Reopening the calendar returns selection and displayed month to today; an unfinished editor retains its input across hiding and reopening. The date header continues to show today while the user browses other dates.

Previous/next arrows switch months; Page Up/Page Down do the same. The grid always starts on Monday and includes muted adjacent-month dates. Today has a filled accent circle; another selected date has an accent outline. Hover adds a neutral circle, and weekends retain a distinct text color. Arrow keys move day selection; Home or the Сегодня button returns to today. Selecting a date does not change the taskbar icon.

The gear opens a two-column settings page in place. Actions are flat, left-aligned rows with neutral hover fills; sign-in when needed and disconnect use accent ink. A small accent dot on the gear signals refresh failure or renewed-sign-in requirements. Открыть Google Calendar hides the flyout before opening the default browser. Startup, reminder preview and Exit remain available in settings, also reached through Settings.cmd or the Start menu settings shortcut.

### Google events

A small dot below a day number signals events from the enabled readable calendars. It is raised clear of the cell's lower edge and uses accent ink, or on-accent ink inside today's filled circle. The selected-date heading adds the event count when nonzero. Selection updates the right pane without changing today's header or taskbar date.

Event rows are 92 logical pixels high. Each card has 12px horizontal padding: a 9pt accent time or all-day label begins 7px below the card top, a 10pt bold title begins at 27px and wraps within a 34px two-line area, and 9pt source-calendar metadata begins at 64px beside a 5px source-color dot. A valid Meet action occupies a 96 × 24px accent-filled rounded button at the upper right, with on-accent text and a 5px corner radius, leaving the time label clear. Conflicting timed meetings add the accent label Пересечение on the metadata line. Long titles end with an ellipsis. A delayed tooltip exposes full time, title and calendar without animation, focus loss or hover reset. Movement within the same card does not redraw the list or restart the tooltip. Only changed hover rows are invalidated, and each row is painted through its own graphics buffer to avoid flicker. Hover and mouse activation share the painted rounded-card boundary; row gaps and outer corners are inactive. Leaving the card, scrolling, changing data or hiding the list clears its tooltip and cursor. Click or Enter opens a usable Google Calendar event link and hides the flyout; unavailable links use secondary title ink. Times follow the Windows time zone. Refresh preserves the selected card and scroll position when its event remains present.

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

Completed cards become visually quieter when the event end is at or before the local clock. Blend their ink toward the panel surface at 68% foreground, card fill and border at 65%, and source-color dots at 55%; use an 8% accent fill for their Meet action and muted readable text. Keep event and Meet actions clickable and keyboard accessible, and add Завершено to the row's accessible text and tooltip. High Contrast retains system primary ink. A local one-second update invalidates only rows whose completed state changed; it does not wait for a Google refresh.

Wheel movement accumulates precision deltas to a full 120-unit notch. The normal Windows three-line setting advances one card per notch; larger settings scale the card stride, the page setting advances visible rows, and disabled wheel scrolling remains disabled. Clamp the scroll position at both ends and keep hit testing aligned with the painted cards. Batch scrollbar and wheel redraws while retaining per-card buffered painting. When the pointer is over the agenda, route wheel messages from other controls in this same flyout to the agenda without changing keyboard focus; leave outside wheel messages alone.

### Meeting tools and privacy

A valid HTTPS Google Meet link from conference data or location enables Войти в Meet and Alt+Enter on the selected card. The card body and Enter retain the Google Calendar event action. Both routes hide the flyout before opening the browser; Google events remain read-only.

Неделя switches the agenda to seven consecutive days starting at the selected date, including dates beyond the visible month grid; День restores one day. Week cards prefix time with their local day. Mark overlaps of timed events, excluding adjacent intervals, all-day events and copies with the same iCal UID and instance start.

The next-meeting label uses the enabled background agenda independently of the browsed month. Ongoing timed meetings say Сейчас; a meeting later today shows a rounded-up minute countdown, and a later date shows its date and time. Ended and all-day entries are excluded. Update this label without repainting the cards.

Private mode replaces titles with Занято and source labels with Календарь in cards, delayed tooltips and accessible row text, and masks the next-meeting label and taskbar tip. Preference calendar names become numbered generic labels. Times, conflict labels, source dots and actions remain usable. Saving applies immediately and clears stale tooltip content; source data is unchanged.

### Local preferences

Параметры opens three tabs inside the same flyout: Календари, Звук и тишина, Вид и приватность. Use the established neutral surface, hover fill, native checkboxes/combos, visible focus and an accent Save button. Save applies the draft; Back leaves active preferences unchanged. Store preferences separately using current-user DPAPI, and reset account-specific calendar selections and colors on successful account replacement or disconnection.

The calendar list includes readable calendars even when they contain no events. Checkboxes filter date dots, cards, the next meeting and reminder evaluation together. Source dots use Google colors unless a local red, green, blue, violet, amber or pink override is chosen; these colors identify sources rather than introduce new global brand accents. Обновить список refreshes the catalog. Color overrides never write to Google.

### Taskbar meeting tip

When enabled, pointer dwell over the located pinned date for 650ms shows the next timed meeting in a separate non-activating, click-through tool window. Use Segoe UI 9.5pt, the event surface and primary ink, with 12px horizontal and 8px vertical padding. Its nominal 360 × 64px bounds scale at actual DPI, clamp to the monitor work area, and stay above the taskbar without covering the pin. Leaving the pin, opening the calendar, disabling the option or losing a usable next meeting hides it. Private mode masks its title. It adds no app-window indicator and never changes the pinned ICO.

### Inline Google setup

Подключение replaces the calendar/settings content inside the same flyout. Client ID and password-masked Client secret fields occupy the left and right columns. A guide link, storage explanation and inline error text accompany neutral Назад and accent-filled Сохранить actions. Leaving the secret blank retains the stored secret for the same Client ID. The editor clears its fields on explicit back or successful save, while merely hiding the flyout preserves unfinished input. Repeated setup or sign-in commands reveal that existing editor. Background commands remain invisible.

Account authorization opens in the default browser. Reopening settings during authorization exposes pending status and Отмена входа. Setup instructions live in GOOGLE_SETUP.md; the older standalone setup-dialog captures describe a superseded surface.

### Reminder accent

Honor each Google event's popup-notification timing, including inherited calendar defaults and explicit overrides. A due reminder adds a badge, ripple and visibly breathing outline over the pinned date, with a 1.8-second cycle. The contour expands up to 3 logical pixels on each side, increasing its opacity and stroke width; the badge grows with it while the white date remains readable. Keep this overlay above Explorer's taskbar on every frame without activating it. It passes clicks through, adds no taskbar button, and does not rewrite the ICO or invalidate Explorer's icon cache.

Three original synthesized PCM sounds — Мягкий перезвон, Кристалл and Тёплый тон — are selectable with local 0–100% volume and mute. Play once when attention becomes active; animation frames do not replay audio. Quiet hours silence automatic audio while retaining the visual accent. Same-day and overnight schedules work; equal endpoints mean silence all day. The explicit Прослушать action previews the selected sound and volume even during quiet hours or when automatic audio is muted.

Pending reminders persist in current-user DPAPI-encrypted state across restart until the date command acknowledges them. Contextual Отложить на 5 мин and Отложить на 10 мин actions defer the active batch, or the most recently acknowledged batch within 15 minutes. Persist its deadline and entries locally; new due reminders remain independent and may activate attention during that interval. At the deadline the deferred batch returns. Google events and their notification settings remain unchanged. Disabling a calendar removes its pending, acknowledged and deferred entries.

Pulse intensity offers Деликатная, Обычная and Выразительная, scaling the existing contour by 0.65, 1 or 1.25. The cycle remains 1.8 seconds and keeps the date readable. Reduced motion and High Contrast use a static accent; High Contrast uses the system highlight. Проверить напоминание previews the same attention treatment until acknowledged.

### Verification evidence

All 313 native self-test checks pass in artifacts/self-test.txt. Coverage includes the existing calendar, link dispatch, hover/no-flicker tooltip behavior, single-window navigation, styles, working-area placement, requested DPI geometry, synthetic multi-calendar responses and refresh failures. Feature tests cover Meet routing, seven-day ranges, conflict exclusions, source filters/colors, privacy and accessible text, preference persistence, sounds/quiet hours, pulse levels and the taskbar tip. Clock-controlled reminder tests cover 5/10-minute snooze deadlines, restart, independent new notifications and acknowledgement; a separate real five-minute snooze is recorded below, while a real ten-minute wait remains unverified.

The 27 current synthetic captures have been opened by the implementer and a fresh reviewer: artifacts/features-agenda-{light,dark}-{96,120,144}.png; artifacts/features-settings-{0,1,2}-{light,dark}-{96,120,144}.png; artifacts/features-private.png; artifacts/features-week.png; artifacts/features-taskbar-tooltip.png. They show the retained two-pane identity, all preference pages, private agenda, seven-day view and taskbar-tip design. Requested-DPI renders do not establish real mixed-monitor transitions. The twelve 0.6.1 captures artifacts/agenda-past-future-{light,dark}-{96,120,144}.png and artifacts/agenda-scroll-end-{light,dark}-{96,120,144}.png were also opened by the implementer and fresh reviewer. They show readable completed/future cards, the stronger Meet action and correctly aligned rows at the scroll end. Native tests cover precision-wheel accumulation, stride, clamping, off-agenda routing, retained focus and a completed-state transition without network refresh. Existing OAuth-editor and reminder-animation captures remain earlier baseline evidence.

The installed native probe artifacts/VerifyCurrentUi.ps1 passes: one hidden startup process; a real today row; the same flyout HWND for calendar, settings, preferences and client editor; no app-window style or running indicator; a correctly located, non-activating pinned reminder preview; unchanged ICO timestamp; and the date command stopping preview. The final application state is hidden. The subsequent artifacts/VerifyInstalledAgenda.ps1 probe confirms installed version 0.6.1.0, six week rows, wheel movement from top index 0 to 1 and back to 0, the same window throughout, and a return to day view. It logs no personal event text. The final rebuilt 0.6.1 source passes all 313 checks; the fresh agenda finish review verdict is ship with no material fixes. A read-only connected check returned six readable calendars, two events in a two-day range, two Meet links and two timed events with popup rules. These earlier connected-read observations are aggregate only and required no new consent. The subsequent real snooze check used one separately authorized test event, with no guests, Meet link or invitations.

TaskbarMeetingTip is exercised as a real Form with controlled pin/pointer/clock inputs: show without activation, stable placement and hide off the pin. The user has not confirmed real pinned-date hover in this run. Real ten-minute snooze waiting, mixed-monitor transitions and actual-midnight pin rollover remain unverified.

A real five-minute snooze passed on 12 November 2026 without replacing the installed app during its timer. Initial attention appeared at 18:59:00.484; acknowledgement and snooze saved a deadline of 19:04:14.133 at 18:59:14.133. Attention returned at 19:04:14.541, 408ms after the deadline; the user confirmed it and dismissed it at 19:04:17.631. A subsequent Google read retained the original 19:00 start and one-minute popup rule, confirming that local snooze did not modify that event. This real check covers five minutes; restart, independent new reminders and the ten-minute deadline remain clock-controlled test evidence.

Earlier real-reminder evidence remains historical: a configured 16:03 Google reminder fired at 16:03:01 local time on 12 November 2026, survived restart and event end, and stopped after the user's click at 16:10:39. The user confirmed that stronger pulse and disappearance; artifacts/real-accent-0.png through artifacts/real-accent-5.png record it. That observation does not substitute for the new snooze or tooltip checks.

## Do's and Don'ts

- **Do** preserve the established icon geometry, palette, large glyphs, and executable identity.
- **Do** keep theme contrast, distinct today/selection states, keyboard focus, and readable scaled labels.
- **Do** keep meetings to the right of the calendar with clearly separated cards and source-calendar labels.
- **Do** keep connection, startup and exit controls behind the gear inside the same flyout.
- **Do** keep background startup invisible and the popup transient.
- **Do** distinguish an empty day from loading, offline data and a request to sign in again.
- **Do** preserve pending attention until acknowledgement or explicit local snooze, and respect Windows motion/contrast preferences.
- **Do** apply calendar filters and private labels consistently to visible, tooltip, accessible and background meeting surfaces.
- **Do** keep quiet hours visual: suppress scheduled audio while retaining the reminder accent.
- **Don't** add a separate taskbar button or running-app indicator for the popup.
- **Don't** animate by rewriting the pinned date icon or rebuild separate modal setup windows.
- **Don't** treat rendered DPI samples as proof of real mixed-monitor behavior or real-midnight rollover.
- **Don't** present synthetic events or authorization tests as a verified real-account connection.
