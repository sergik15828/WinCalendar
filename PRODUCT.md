# WinCalendar

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop; Windows 11.

## Stack

C# 5, the Windows .NET Framework desktop runtime, Windows Forms as the background host, Win32 shell integration. No third-party packages or SDK installation.

## Users and purpose

The user wants to see today's weekday and day number continuously in the taskbar, like the date on the macOS Dock calendar icon. It should sit first among left-aligned application icons. The user explicitly clarified that a calendar window is not needed.

## Capabilities and constraints

- Current local day number and a two-letter Russian weekday directly on the application icon.
- No calendar window on launch, click, restore, or repeated launch.
- Once pinned, hide the running-app button; retain only the pinned date icon and background updater.
- Fill the existing icon slot with larger weekday and day glyphs; do not change global Windows scale settings.
- Keep a minimized native host alive for the taskbar button and automatic date updates.
- The user pins and reorders the application using Windows itself.
- Startup and Exit in the small menu opened by clicking the date.
- Offline, no accounts or calendar service integration.
- Full exit stops date updates; startup is an explicit user choice.

## Brand commitments

The macOS reference concerns the date icon. The user now prefers a large two-letter weekday instead of the month above the day number. Preserve readability at taskbar icon sizes.
