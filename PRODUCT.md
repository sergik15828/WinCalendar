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
- A button in the panel opens Google Calendar in the default browser. Optional Google authorization reads one account's primary calendar; date dots and a selected-day list expose meetings and their browser links.
- Use Desktop OAuth, PKCE, loopback callback, and calendar.events.readonly. Store client credentials and refresh session in Windows Credential Manager, access tokens in memory, and event snapshots with current-user DPAPI encryption. Users create their own Google Cloud Desktop client.
- Render loading, empty, offline with cached events and update time, and renewed-sign-in states. Refresh asynchronously; stale month or account responses cannot replace the current view.
- Escape, outside click, or another icon click closes the panel. Its native tool-window style must not add a running-app indicator.
- Startup and Exit remain available through Settings.cmd and the Start menu settings shortcut.
- Background startup must never open the calendar panel or browser.
- Date and month grid work offline without an account. Google data requires a network connection for refresh; retained snapshots remain available offline. No event editing or invitations.
- Full exit stops date updates; startup is an explicit user choice.

## Brand commitments

The macOS reference concerns the date icon. The user now prefers a large two-letter weekday instead of the month above the day number. Preserve readability at taskbar icon sizes.
