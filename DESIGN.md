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
  weather-sun: "#FFB020"
  weather-light-sun-edge: "#BF680D"
  weather-dark-sun-edge: "#FFBE41"
  weather-light-cloud: "#3C91E6"
  weather-dark-cloud: "#66AAFF"
  weather-light-cloud-edge: "#2166B4"
  weather-dark-cloud-edge: "#87C1FF"
  weather-light-moon: "#D59522"
  weather-dark-moon: "#FFE1A0"
  weather-light-rain: "#006FD6"
  weather-dark-rain: "#5DB5FF"
  weather-light-snow: "#087CB9"
  weather-dark-snow: "#66D0FF"
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

Use Segoe UI throughout. The icon's bold glyphs fit their actual contours rather than ordinary text padding. The popup hierarchy is today's date or page heading (calendar-heading), displayed month and Встречи heading (calendar-month), day numbers and settings actions (calendar-body), and weekday, selected-date and status labels (calendar-weekday). Cards use bold calendar-event-title text beneath an accent-colored calendar-event-detail time; their source calendar uses the same detail size in secondary ink. Preferences and the taskbar meeting tip use 9.5pt Segoe UI. Russian is the default UI language; saved English switches interface labels, accessible names and date culture, including two-letter taskbar weekdays; event titles retain their source text unless private mode masks them.

## Layout

The popup's natural size is 688 × 380 logical pixels at 96 DPI. This approved two-pane arrangement replaces the former tall layout. A fine divider at x=344 separates the calendar from the agenda. Both panes use 16px outer insets; the agenda starts at x=360 and is 312px wide. The seven-column, Monday-first calendar retains six week rows, now at a compact 32px pitch.

Today's date and weekday sit above the calendar. Today and the month arrows share the month-heading row. The right pane has a 184px-wide Встречи heading at (360,16), a 32 × 32px calendar-symbol button at (560,16), a selected-date/count subtitle and an independently scrollable event list at (360,86), sized 312 × 278 logical pixels. The gear at (600,16) opens settings in the same footprint; the separate close button at (640,16) hides the flyout while the background host keeps running. The Day/Week toggle sits at (580,46); the next-meeting label at (24,328), sized 296 × 18px with a 12px gap below the grid and 8px extra left inset, and contextual 5/10-minute snooze actions at y=348 use the space below the month grid. Connection actions, sync status, the additional browser launch action, preferences, startup, reminder preview and Exit live behind that gear; the calendar view has no footer-control strip.

Calendar, settings, preferences and OAuth editing reuse the same native window. Calendar, settings and OAuth keep the 688 × 380 logical footprint; preferences resize that HWND to a compact 720 × 460 logical pixels and restore the calendar size when leaving. A fixed title/version header and Back/Save footer surround six 113 × 32px tab buttons: General, Reminders, Sound, Payday, Calendars and Weather. The content viewport is (20,112,680,276), with two 336px columns separated by 8px where needed. General pairs general controls with manual pinned-icon guidance; Sound includes quiet hours; Calendars includes the Google connection action. The normal 100/125/150% layouts fit without body scrolling. A constrained working area retains viewport scrolling as recovery. Use 9.5pt Segoe UI body and 12pt Segoe UI Semibold section labels. Bounds, fonts and paint follow actual DPI; anchor near the date control and clamp to the monitor work area.

When enabled, a 112 × 44px payday badge at (216,12) sits beside today's heading; heading/weekday widths become 194px. A locally authored gold money bag accompanies a two-line countdown in 12px Segoe UI Semibold and 10.5px secondary text. The badge is informational and exposes the next payday date in accessible text.

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

The gear opens a two-column settings page in place. Its subtitle shows WinCalendar · followed by the running assembly's major.minor.build version (0.6.4 in the current build); the calendar's weekday subtitle remains unchanged. Actions are flat, left-aligned rows with neutral hover fills; sign-in when needed and disconnect use accent ink. A small accent dot on the gear signals refresh failure or renewed-sign-in requirements. Открыть Google Calendar hides the flyout before opening the default browser. Startup, reminder preview and Exit remain available in settings, also reached through Settings.cmd or the Start menu settings shortcut. This 0.6.4 label change preserves geometry and styling; all 323 checks pass, and the implementer and fresh reviewer opened the six current light/dark settings captures at 96/120/144 DPI and confirmed the version is unclipped, with a ship verdict.

### Google events

A small dot below a day number signals remaining events from the enabled readable calendars. Completed events are excluded from dots, agenda counts and day/week cards together. It is raised clear of the cell's lower edge and uses accent ink, or on-accent ink inside today's filled circle. The selected-date heading adds the event count when nonzero. Selection updates the right pane without changing today's header or taskbar date.

Event rows are 92 logical pixels high. Each card has 12px horizontal padding: a 9pt accent time or all-day label begins 7px below the card top, a 10pt bold title begins at 27px and wraps within a 34px two-line area, and 9pt source-calendar metadata begins at 64px beside a 5px source-color dot. An ongoing or future event with a valid Meet action uses a 96 × 24px accent-filled rounded button at the upper right, with on-accent text and a 5px corner radius, leaving the time label clear. Conflicting timed meetings add the accent label Пересечение on the metadata line. Long titles end with an ellipsis. A delayed tooltip exposes full time, title and calendar without animation, focus loss or hover reset. Movement within the same card does not redraw the list or restart the tooltip. Only changed hover rows are invalidated, and each row is painted through its own graphics buffer to avoid flicker. Hover and mouse activation share the painted rounded-card boundary; row gaps and outer corners are inactive. Leaving the card, scrolling, changing data or hiding the list clears its tooltip and cursor. For ongoing and future events, click or Enter opens a usable Google Calendar event link and hides the flyout; unavailable links use secondary title ink. Times follow the Windows time zone. Refresh preserves the selected card and scroll position when its event remains present.

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

Show only events whose end is strictly later than the local clock, retaining both ongoing and future meetings. Remove completed events entirely from the day and seven-day agendas, counts and date dots. Refresh at the next enabled event end from the one-second background tick; no API response is needed. Existing live-clock link guards remain defensive implementation checks, rather than a visible completed-card state.

For a confirmed, healthy connected snapshot with no remaining meetings in today's day view, show exactly На сегодня митов больше нет, приятного вечера. Keep loading, errors, disconnection/sign-in and hidden-calendar explanations ahead of this evening message. Week view uses its own empty-week message, and other selected dates retain their empty-day wording.

Wheel movement accumulates precision deltas to a full 120-unit notch. The normal Windows three-line setting advances one card per notch; larger settings scale the card stride, the page setting advances visible rows, and disabled wheel scrolling remains disabled. Clamp the scroll position at both ends and keep hit testing aligned with the painted cards. Batch scrollbar and wheel redraws while retaining per-card buffered painting. When the pointer is over the agenda, route wheel messages from other controls in this same flyout to the agenda without changing keyboard focus; leave outside wheel messages alone.

### Meeting tools and privacy

For an ongoing or future event, a valid HTTPS Google Meet link from conference data or location enables Войти в Meet and Alt+Enter on the selected card. The card body and Enter retain the Google Calendar event action. Both routes hide the flyout before opening the browser; Google events remain read-only. The header calendar-symbol button remains available independently of any card, with the accessible name Открыть Google Calendar в браузере, and uses the existing GoogleRequested browser action, dismissing the flyout while the background host continues.

Неделя switches the agenda to seven consecutive days starting at the selected date, including dates beyond the visible month grid; День restores one day. Week cards prefix time with their local day. Mark overlaps of timed events, excluding adjacent intervals, all-day events and copies with the same iCal UID and instance start.

The next-meeting label uses the enabled background agenda independently of the browsed month. Ongoing timed meetings say Сейчас; a meeting later today shows a rounded-up minute countdown, and a later date shows its date and time. Ended and all-day entries are excluded. Update this label without repainting the cards.

Private mode replaces titles with Занято and source labels with Календарь in cards, delayed tooltips and accessible row text, and masks the next-meeting label and taskbar tip. Preference calendar names become numbered generic labels. Times, conflict labels, source dots and actions remain usable. Saving applies immediately and clears stale tooltip content; source data is unchanged.

### Local preferences

Параметры opens six compact tabs: Основное / General, Напоминания / Reminders, Звук / Sound, Зарплата / Payday and Календари / Calendars, Погода / Weather. General contains language, weather-widget mode, startup, privacy and next-meeting hover, with pinned-icon guidance alongside. Reminders contains event/5-minute/2-minute timing, animation strength and preview. Sound includes volume, sound selection and quiet hours. Payday keeps its local schedule and preview. Calendars includes source selection/colors and the Google connection action; Weather contains the independent weather switch, manual city search/selection and a Windows taskbar-settings link. Save applies the draft and returns to the calendar; Back leaves active preferences unchanged. Store preferences using current-user DPAPI. Calendar selection/colors still reset on successful account replacement or disconnection. The footer and page title/version remain fixed; scrolling is reserved for constrained work areas, with precision-wheel routing respecting open dropdowns.

The calendar list includes readable calendars even when empty. Checkboxes filter date dots, cards, the next meeting and reminder evaluation together. Source dots use Google colors unless a local red, green, blue, violet, amber or pink override is chosen. These identify sources rather than adding global brand accents; colors and choices never write to Google.

Payday is enabled by default with two monthly dates, 15 and 1. Choose one or two payments and day values 1–31, or hide the badge. Count local calendar days from actual today, including today's payment as Сегодня/Today. Clamp a date beyond a short month's length to its final day; choose the next non-past date and do not move payments for weekends. The preview shows the next payment date.

### Taskbar meeting tip

One compact tooltip replaces the widget's native ToolTip. Observe the full widget rectangle, or pinned-icon area in fallback mode, every 40ms; a 650ms dwell shows the next timed meeting when that option/data is available, otherwise the localized full date. Use Segoe UI 9.5pt, the event surface and primary ink, with 8px horizontal and 6px vertical padding. Measure content to at most 300 logical pixels and two lines, with a 120px minimum width. Fix placement above the widget's text start (base left +46px), or the pinned icon's left edge, with a 6px gap, clamp to the monitor work area, and never follow the pointer within the button. The non-activating, click-through tool window has no class shadow or tooltip animation. Leaving the area or opening the calendar hides it; acknowledgement explicitly disables observation until the coordinator enables it again, so it cannot reopen behind the popup. Private mode masks meeting titles. It adds no app-window indicator and never changes the pinned ICO.

### Inline Google setup

Подключение replaces the calendar/settings content inside the same flyout. Client ID and password-masked Client secret fields occupy the left and right columns. A guide link, storage explanation and inline error text accompany neutral Назад and accent-filled Сохранить actions. Leaving the secret blank retains the stored secret for the same Client ID. The editor clears its fields on explicit back or successful save, while merely hiding the flyout preserves unfinished input. Repeated setup or sign-in commands reveal that existing editor. Background commands remain invisible.

Account authorization opens in the default browser. Reopening settings during authorization exposes pending status and Отмена входа. Setup instructions live in GOOGLE_SETUP.md; the older standalone setup-dialog captures describe a superseded surface.

### Reminder accent

Honor each Google event's popup-notification timing, including inherited calendar defaults and explicit overrides. In pinned-icon fallback mode a due reminder adds a visibly breathing outline, with a 1.8-second cycle and no badge dot or ripple. The contour expands up to 3 logical pixels on each side, increasing its opacity and stroke width; the white date remains readable. Keep this overlay above Explorer's taskbar on every frame without activating it. It passes clicks through, adds no taskbar button, and does not rewrite the ICO or invalidate Explorer's icon cache.

Three original synthesized PCM sounds — Мягкий перезвон, Кристалл and Тёплый тон — are selectable with local 0–100% volume and mute. Play once when attention becomes active; animation frames do not replay audio. Quiet hours silence automatic audio while retaining the visual accent. Same-day and overnight schedules work; equal endpoints mean silence all day. The explicit Прослушать action previews the selected sound and volume even during quiet hours or when automatic audio is muted.

Pending reminders persist in current-user DPAPI-encrypted state across restart until the date command acknowledges them. Contextual Отложить на 5 мин and Отложить на 10 мин actions defer the active batch, or the most recently acknowledged batch within 15 minutes. Persist its deadline and entries locally; new due reminders remain independent and may activate attention during that interval. At the deadline the deferred batch returns. Google events and their notification settings remain unchanged. Disabling a calendar removes its pending, acknowledged and deferred entries.

Pulse intensity offers Деликатная, Обычная and Выразительная, scaling the existing contour by 0.65, 1 or 1.25. The cycle remains 1.8 seconds and keeps the date readable. Reduced motion and High Contrast use a static accent; High Contrast uses the system highlight. Проверить напоминание previews the same attention treatment until acknowledged.

### Weather-adjacent date prototype — branch only

Version 0.8.0 on codex/weather-date-prototype adds an opt-in wide date button beside Windows weather. Stable main remains at 0.6.4 (d534870), and the incumbent pinned icon is preserved as a backup. This is an additional product mode, not a replacement for the icon or calendar visual system.

The authored native widget is 160 × 44 logical pixels: a full-color calendar at (12,9), with 25 × 27px warm-white rounded paper, a red 8px header, silver rounded binding strokes and a two-row, three-column grid with one red cell; the icon has no day numeral, so the main date text is the only date/number; text starts at x=46. The first line is localized d MMMM in 14px Segoe UI Semibold (native Regular style of that face) at y=6, and the second is dddd in 12px secondary ink at y=22, bringing the two lines closer while retaining x=46 and their existing widths. Draw directly at the target DPI rather than stretching the existing square icon. Use the system taskbar light/dark preference and native High Contrast palette. A 6px rounded shape inset 1px uses alpha 1 at rest, making its interior clickable while visually transparent; hover uses neutral ink at alpha 22. Transparent exterior corners are outside that painted hit area. The non-activating layered tool window owns clicks. In widget mode reminder attention now animates only the small calendar icon; the overlay retains reminder/chime ownership but hides its own window. Pinned fallback retains its separate click-through outline.

Locate weather using only exact known shell AutomationIds, without reading application titles or accessible names. Search the horizontal primary taskbar every two seconds and normally check exposure every 500ms. Retention eligibility requires a last valid placement younger than three seconds, checked on the two-second locator cadence rather than enforcing a hard three-second visibility maximum; missing weather retains it only while the bar is unchanged and no button overlaps. Confirmed crowding hides immediately. Missing-widget transitions can therefore remain visible until an expired poll, roughly four seconds; placement is not a strict two-second guarantee. Place beside weather in available space; hide when missing, crowded, hidden, auto-hidden off screen or covered by a full-screen foreground window. Do not reserve Explorer space, inject into Explorer or change taskbar settings. In the historical 0.7.0 120-DPI native probe the wide bounds are (204,1022,200,55); a 44-logical-pixel anchor over its left calendar area places the existing pulse at (203,1019,60,60). These coordinates are observed placement, not global layout tokens.

Click acknowledges attention and opens the existing calendar; another click closes it. Suppress background painting and raise the widget without activation immediately and through a queued callback on calendar visibility/activation and clicks. For one second after a click, use 50ms settling checks, then return to 500ms. Google, OAuth, the calendar, reminders, sounds and local snooze reuse the incumbent behavior. EnableWeatherPrototype.cmd / --weather-on and DisableWeatherPrototype.cmd / --weather-off persist a nonsecret local opt-in flag; a clean install defaults off. Disabling hides the widget immediately and returns attention to the preserved pinned-icon anchor without requiring new Google consent.

The 0.7.1 attention animation is macOS Dock-inspired, not an exact operating-system clone. Only the calendar vector moves, fully opaque: a 2.6-second cycle uses ballistic arcs 4t(1−t), first during phase 0–0.28, then at 42% amplitude during 0.28–0.47, then 14% during 0.47–0.59, followed by a pause. The first hop is 16/24/30 logical pixels for delicate/normal/expressive. There is no fading or disappearance. Add 28 transparent logical pixels above the active canvas (160 × 72px total), compensate the native presentation Y by the same amount, and keep the date, weekday and base 160 × 44px hit region fixed. Use 33ms animation ticks only while visible with active motion; hidden mode returns to 500ms. Reduced motion uses a static outline without headroom. Strength changes apply immediately on Save.

Reminder timing offers event settings (default), five minutes before or two minutes before. Fixed modes apply locally to timed meetings even when Google popup rules are disabled; all-day events keep Google timing/time-zone semantics. Changing timing preserves pending and snoozed batches and resets the evaluation boundary so older cached deadlines do not replay retroactively. Google events and read-only scopes remain unchanged. Existing 5/10-minute local snooze and audio/quiet-hour behavior remain independent.

The preferences page saves Russian/English and weather-widget mode. Language applies immediately to interface labels, dates, the widget and rebuilt pinned icon; source meeting/calendar names and OAuth input values are unchanged. The flag file remains the source of widget-mode truth. Widget mode suppresses the running-app button even when the old pin is removed or after restart. With widget off and no pin, retain the legacy bootstrap button. The app's shortcut button selects the shortcut in Explorer; the user manages the pinned app icon through Windows context menus. This concerns the pinned icon, not the notification tray. Microsoft's pin-request APIs require confirmation and differ by Windows version; this implementation does not invoke pin/unpin APIs or shell hacks. See [Microsoft taskbar pinning guidance](https://learn.microsoft.com/en-us/windows/apps/develop/windows-integration/pin-to-taskbar).

The earlier 0.7.0 branch passes all 331 checks. Twelve weather-widget-{light,dark}-{rest,hover}-{96,120,144}.png artifacts are own-renderer fixtures on nominal taskbar backgrounds, regenerated and independently reopened by the implementer and fresh reviewer after the final Semibold font and closer-line spacing changes. The latest review is ship with no material fixes, collision or clipping across both themes, all three DPIs and rest/hover states. The installed build matches this final renderer. They are not desktop captures. The preceding wide native probe, before the final renderer-only font/spacing changes and with the same unchanged window logic, uses own-window messages, rather than physical mouse input or desktop screenshots, and confirms no app-window button, non-activation, correctly shared pulse anchor, acknowledgement/open and repeat-click close, ending with the calendar hidden. StableAfterOpenAndClose=True records 50ms visibility polls for 2.5 seconds after opening and 2.5 seconds after closing, with a hide-counter baseline taken before the click. This confirms own-window visible state and no recorded hide transitions, not pixel-level z-order or a guarantee against operating-system flicker. Startup persistence was verified by an Explorer startup of an earlier square prototype; restart of this current wide version has not yet been repeated. Computer-use capture failed because of a sandbox URI error. Actual desktop appearance, full-screen/autohide transitions and mixed-monitor behavior remain unverified. The fresh full-color/stability review is ship with no material fixes; no fixture or own-window probe establishes a physical-mouse or desktop-visual check.

The preceding unified-page 0.7.1 build passed all 366 checks, including reminder timing/boundary persistence, live RU→EN→RU controls, resolved Segoe UI Semibold font, fixed global text pixels with active headroom, unified preference layout/scrolling and payday rules. Its 31 reviewed fixtures include artifacts/settings-unified-{ru,en}-{light,dark}-{96,120,144}.png (12), artifacts/payday-calendar-{light,dark}-{96,120,144}.png (6), artifacts/english-core-{light,dark}-{96,120,144}.png (6), artifacts/widget-blink-{light,dark}-{96,120,144}.png (6) and artifacts/widget-compact-tooltip.png. The motion filename is retained, but its content now shows opaque Dock-inspired motion, not blinking/fading. A fresh reviewer opened all 31 and found no clipping or overlap; final visual and source-flow verdict ship, with no blockers; the reminder-timing review also ships.

The installed own-window probe recorded in artifacts/weather-installed.txt at 22:30:54 on 12 November 2026 passes: base date bounds (204,1022,200,55), IconAnimationAdvanced=True, StoppedAfterClick=True, StableAfterOpenAndClose=True, UnifiedPreferencesSameWindow=True and FinalCalendarVisible=False. It verified animation, acknowledgement and same-HWND expanded unified preferences in that preceding build. This is native message/visible-state evidence, not physical-mouse or pixel-artifact proof. No Google event was created for this scope; computer-use sandbox URI capture remains unavailable. Earlier 0.7.0 evidence above and stable 0.6 evidence below retain their historical scope.

The current compact-tab source replaces that unified-page layout while preserving language, saved preferences and reminder behavior. Its twelve settings contact sheets are artifacts/settings-tabs-{ru,en}-{light,dark}-{96,120,144}.png; each contains all five pages vertically. The final compact build passes all 366 checks after the tab-keyboard fix. A fresh reviewer opened all twelve five-page contact sheets and reviewed keyboard routing and explicit tab order; verdict ship. The first installed compact probe passes animation advancement, click acknowledgement, open/close stability, shared preference HWND and final hidden state. The exact final keyboard-fixed binary passes the full native probe after a normal Explorer restart, including UnifiedPreferencesSameWindow=True and persisted widget mode across restart. A follow-up after the launch script exited reports InstalledRunning=1 and InstalledMatchesBuild=True. An earlier installation attempt failed the second-click close assertion, and a subsequent probe found no host; the successful normal restart does not establish that earlier failure's root cause. This remains a diagnostic installation/startup limitation, rather than a claim that every launch path is stable.

### Own weather — current 0.8.0 extension

The combined primary-taskbar widget keeps the incumbent 160px date control on the left and adds weather on the right, for 320 × 44 logical pixels. A fine divider at x=166 separates the halves. Draw authored sun/moon/cloud/rain/snow/thunder symbols in (179,9,30,30); temperature uses (218,6,96,20) and city/status (218,22,96,16). Reuse the existing Segoe UI hierarchy. Weather illustrations use the extracted vivid weather-* tokens: amber sun/moon, saturated blue clouds with outlines, and clear rain/snow strokes; High Contrast replaces these with system ink. These condition colors do not replace the incumbent calendar/icon palette. The date icon's opaque Dock-inspired reminder motion and fixed base hit area are unchanged; weather content remains fixed. Weather off returns the date-only width to160px, placed to the right of native Windows weather once the user enables it. Until native weather is available, retain the safe standalone-left date fallback. Changing modes clears the old placement; a layout revision rejects a late old-width lookup so the previous width cannot stretch the new mode.

Own weather defaults enabled, with no saved city and a Choose city/Выбрать город prompt. The user searches for a city and selects a result including region/country; Save persists that choice in current-user DPAPI preferences. No default city is guessed, and no geolocation or API key is used. A revision boundary prevents a late result for the old city from replacing the new city. Missing city, loading, no data and stale data have distinct visible states.

Open-Meteo supplies current temperature/feels-like/humidity/wind and three daily forecasts, Celsius and m/s. Refresh every 15 minutes, retry failures after five minutes, with a 12-second timeout and bounded response size. Current-user DPAPI cache entries are tied to coordinates. Failed refresh or age beyond 45 minutes marks a snapshot stale; show it for at most 24 hours. Cache state never establishes a fresh observation.

Clicking weather opens a 688 × 380px details page in the same HWND, with condition, feels-like, wind, humidity, update/stale status and three-day high/low forecast. Its four footer actions are Back (20,328,80,34), Choose city (116,328,160,34), Yandex Weather (292,328,196,34) and Data: Open-Meteo (504,328,164,34). Back returns to the calendar and Choose city opens the sixth preference tab. Yandex is a user-clicked browser destination for the selected coordinates and escaped city/country name, disabled without a valid selected city; data remains Open-Meteo. No Yandex API or API key is integrated. The separate Open-Meteo action preserves provider attribution. Weather clicks do not acknowledge a meeting reminder; date clicks acknowledge and select the calendar immediately, even when the last hidden page was weather details. Track the selected page explicitly, rather than relying on inherited child visibility; a repeat date click closes the calendar.

When the Windows weather button is absent, place the independent combined widget at a safe left inset on the horizontal primary taskbar, hiding if occupied. While Windows weather remains visible, retain the previous adjacent placement. The app opens Windows taskbar settings only when the own-weather mode changes on Save while the widget is enabled, or when the user presses its settings link. Show an inline mode hint. The user toggles Windows Widgets manually: off for own weather, on for native weather beside the date. The app never automatically writes the protected setting, patches Explorer or restarts it. An authorized one-off TaskbarDa change was denied by Windows with UnauthorizedAccessException; the DWORD remains 1. This was an operating-system permission failure, not an automatic approval-review rejection. The user subsequently confirmed disabling Widgets manually; a read-only check now reports TaskbarDa=0 DWORD.

An earlier 0.8.0 build passed all384self-test checks. Installed background update succeeds, the installed binary matches the final build and one background process is running. A fresh reviewer opened all 30 current fixtures and reviewed the source; verdict ship with no blockers. After the user disabled Windows Widgets and a normal Explorer launch, the exact 0.8.0 binary passes the full native probe: base bounds (11,1022,400,55), animation advancement, acknowledgement, open/close stability, weather details and preferences in the same HWND, SystemWeatherVisible=False and final calendar hidden. Widget mode also survives normal restart. An earlier initial probe failed click-open before the normal restart passed; its root cause remains unproven. That reviewed matrix is under artifacts/weather-080-232923/ and comprises twelve settings-tabs-{ru,en}-{light,dark}-{96,120,144}.png sheets with six pages each, six combined-weather-{light,dark}-{96,120,144}.png condition/RU-EN sheets, six weather-details-{light,dark}-{96,120,144}.png sheets and six weather-search-{light,dark}-{96,120,144}.png sheets (30 paths total). City-search results retain native list scrolling instead of redirecting their wheel messages to the preference viewport; English loading text and accessible forecast details are included. Independent public requests passed without saving a user city, including five results for Ереван (first: Ереван, Армения) and eight for Yerevan. These provider responses did not establish the app's HTTPS behavior: the actual C# client subsequently failed SSL/TLS. A weather-client-only TLS1.2 handler fix retains normal certificate validation and leaves Google/global security settings unchanged; the final build and all384checks pass. The actual C# read-only probe passes Ереван search (five results, first Ереван, Армения), current weather and three-day forecast; narrow TLS/diagnostics review is ship. The rerun writes artifacts/weather-080-234221/, but the reviewed30UIfixtures remain artifacts/weather-080-232923/: UI code/geometry is unchanged, and no fresh inspection of the regenerated copies is claimed. The installed TLS binary was verified running once and byte-identical to the final build. A later native click-open probe failed; the earlier full native pass remains scoped to its recorded run. No city was auto-chosen or saved; the user must search, select and Save. A later diagnostic isolated one current click-rejection cause: the presented layered bitmap included reminder headroom while WinForms ClientRectangle retained the shorter size, so the click could be rejected before acknowledgement. The window now validates against the actual successfully presented frame size. A native expanded-headroom click regression passed in a 385-check intermediate build; this does not establish the cause of earlier 0.7.1 failures. The final vivid-symbol/Yandex-link build succeeds and all387checks pass in artifacts/weather-080-235611/, including the native expanded-headroom click and two Yandex-link tests. The twelve changed combined-weather/weather-details fixtures received fresh review and ship; the eighteen settings/search fixtures retain the preceding review because that UI source is unchanged. Subsequent route/mode changes add explicit hidden-weather-to-calendar switching and a six-sheet weather-modes-{light,dark}-{96,120,144}.png matrix showing own weather on/off in RU/EN. The final source passes all389checks in artifacts/weather-080-000929/. All six mode sheets received fresh review and ship; the prior30-fixture review remains scoped to twelve vivid/Yandex combined/details sheets in artifacts/weather-080-235611/ and eighteen settings/search sheets in artifacts/weather-080-232923/. The final installed build passes the full native probe: Date=(11,1022,400,55); IconAnimationAdvanced, StoppedAfterClick, StableAfterOpenAndClose, WeatherDetailsSameWindow and UnifiedPreferencesSameWindow are True; SystemWeatherVisible and FinalCalendarVisible are False. A prior probe passed click/details but asserted the compact preference height while preferences were already open; the diagnostic now returns to calendar with Back before preview. This setup correction is separate from the product fix that validates clicks against the actual layered frame. Earlier failed attempts remain historical; the exact final installed run passes both paths. All 0.7.x and stable evidence above/below remains historical to its stated scope.

### Verification evidence

All 323 native self-test checks pass in artifacts/self-test.txt. Coverage includes the existing calendar, link dispatch, hover/no-flicker tooltip behavior, single-window navigation, styles, working-area placement, requested DPI geometry, synthetic multi-calendar responses and refresh failures. Feature tests cover Meet routing, seven-day ranges, conflict exclusions, source filters/colors, privacy and accessible text, preference persistence, sounds/quiet hours, pulse levels and the taskbar tip. Clock-controlled reminder tests cover 5/10-minute snooze deadlines, restart, independent new notifications and acknowledgement; a separate real five-minute snooze is recorded below, while a real ten-minute wait remains unverified.

The twelve current 0.6.3 captures artifacts/remaining-meetings-{light,dark}-{96,120,144}.png and artifacts/remaining-evening-{light,dark}-{96,120,144}.png were opened by the implementer and fresh reviewer for this exact remaining-only agenda, evening message, calendar icon and spacing scope. The fresh remaining-agenda finish review verdict is ship with no material fixes. Six new checks verify ongoing/future counts, removal at the exact end without network refresh, the exact evening text, clearing the last date dot, week filtering and error precedence.

The full self-test also regenerates the baseline 27 feature-matrix files, six agenda-past-future files and six agenda-scroll-end files. Those are additional current test outputs; the prior full feature review was for the 0.6 baseline and is not a fresh full-matrix review of 0.6.3. Earlier filenames do not imply that completed cards remain published UI. Existing native wheel, focus, link, preferences and reminder checks continue passing. Requested-DPI renders do not establish real mixed-monitor transitions; earlier OAuth-editor and real-reminder captures retain their historical provenance.

The installed native probe artifacts/VerifyCurrentUi.ps1 passes: one hidden startup process; a real today row; the same flyout HWND for calendar, settings, preferences and client editor; no app-window style or running indicator; a correctly located, non-activating pinned reminder preview; unchanged ICO timestamp; and the date command stopping preview. The final application state is hidden. The current artifacts/VerifyInstalledAgenda.ps1 probe exits successfully for installed version 0.6.3.0: four week rows, wheel movement from top index 0 to 1 and back to 0, the same window throughout, a return to day view with the exact evening message visible, and a final hidden flyout. It logs no personal event text. The current 0.6.3 source passes all 323 checks. A read-only connected check returned six readable calendars, two events in a two-day range, two Meet links and two timed events with popup rules. These earlier connected-read observations are aggregate only and required no new consent. The subsequent real snooze check used one separately authorized test event, with no guests, Meet link or invitations.

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
