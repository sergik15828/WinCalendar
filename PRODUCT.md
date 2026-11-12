# WinCalendar

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop; Windows 11.

## Stack

C# 5, the Windows .NET Framework desktop runtime, Windows Forms as the background host, Win32 shell integration. No third-party packages or SDK installation.

## Users and purpose

The user wants to see today's weekday and day number continuously in the taskbar, like the date on the macOS Dock calendar icon. It should sit first among left-aligned application icons. Clicking the icon now opens a small local month calendar with a button to Google Calendar.

## Capabilities and constraints

- Current local day number and a two-letter Russian weekday directly on the application icon.
- Clicking or explicitly launching the application toggles a compact month calendar near the pointer. Background startup stays invisible.
- Once pinned, hide the running-app button; retain only the pinned date icon and background updater.
- Fill the existing icon slot with larger weekday and day glyphs; do not change global Windows scale settings.
- Keep a minimized native host alive for the taskbar button and automatic date updates.
- The user pins and reorders the application using Windows itself.
- Calendar supports month navigation, Monday-first weeks, today and selected-day highlights, and a Today button. Selection does not change the taskbar date.
- A button in the panel opens Google Calendar in the default browser. Optional Google authorization reads the primary and additional readable calendars of one account; hidden/deleted and free-busy-only entries are skipped. Date dots and a selected-day list expose meetings, their source calendar and browser links. Browser display checkboxes do not limit app loading.
- Use Desktop OAuth, PKCE, loopback callback, calendar.events.readonly and calendar.calendarlist.readonly. An older event-only grant requires renewed consent without re-entering the Desktop client. Store client credentials and refresh session in Windows Credential Manager, access tokens in memory, and event snapshots with current-user DPAPI encryption. Users create their own Google Cloud Desktop client.
- Read calendar-list and event pagination, keep event IDs distinct per source calendar, and invalidate the former primary-only cache. A missing permission or failed refresh must not be presented as a verified empty day.
- Render loading, empty, offline with cached events and update time, and renewed-sign-in states. Refresh asynchronously; stale month or account responses cannot replace the current view.
- Escape, outside click, or another icon click closes the panel. Its native tool-window style must not add a running-app indicator.
- The 688×380 logical panel places the month grid on the left and selected-day meetings on the right. Separate event cards show time, a two-line title and source calendar; long agendas scroll.
- A top-right gear opens connection state, refresh, account actions, Google Calendar, reminder preview, startup and exit in the same native window. No bottom status strip. Settings.cmd and the Start menu settings shortcut open this same page.
- Google client fields are another page of the same reusable tool window. No nested modal dialogs; repeated setup reveals the existing editor and preserves unsaved input when hidden.
- Read the event's popup reminder overrides or calendar defaults; disabled and email-only reminders do not animate. A subtle 1.8-second accent over the pinned icon persists until icon activation, with encrypted pending state across restarts. It must not steal focus, rewrite the icon cache or add a running-app indicator. Windows reduced motion and high contrast use a static accent.
- Reminder synchronization runs every two minutes independently of the month the user browses. All-day reminder times follow the source calendar's time zone. Reminder state and snapshots use current-user DPAPI.
- Background startup must never open the calendar panel or browser.
- Date and month grid work offline without an account. Google data requires a network connection for refresh; retained snapshots remain available offline. No event editing or invitations.
- Card hover, tooltip and pointer activation share the visible rounded bounds; gaps are inactive. Delayed tooltips preserve flyout focus. A separate header close button hides the flyout without exiting the background updater.
- The Windows Asterisk system sound plays once at pulse activation, including preview; animation frames do not repeat it.
- Full exit stops date updates; startup is an explicit user choice.

## Brand commitments

The macOS reference concerns the date icon. The user now prefers a large two-letter weekday instead of the month above the day number. Preserve readability at taskbar icon sizes.
