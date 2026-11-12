---
name: WinCalendar
description: Today's date continuously visible on a Windows 11 taskbar icon.
colors:
  weekday-strip: "#C42B36"
  weekday-text: "#FFFFFF"
  paper: "#FCFCFC"
  day-text: "#18181B"
  icon-border: "#D7D7D7"
typography:
  icon-weekday:
    fontFamily: "Segoe UI"
    inkHeight: "20px"
    fontWeight: 700
  icon-day:
    fontFamily: "Segoe UI"
    inkHeight: "30px"
    fontWeight: 700
rounded:
  icon: "6px"
components:
  date-icon:
    backgroundColor: "{colors.paper}"
    textColor: "{colors.day-text}"
    rounded: "{rounded.icon}"
---

# WinCalendar design

The product's entire visible surface is the date icon in the Windows taskbar. There is no calendar window. Launch, click, and repeated launch keep the native taskbar host minimized. Clicking the date opens a small native menu for startup and exit. A pinned launcher stays visible while the native host removes its own taskbar button, eliminating the running-app underline.

## Date icon

Render the icon independently at 16, 20, 24, 32, 40, 48, 64 and 128 pixels. Coordinates below refer to the 64px drawing plane.

- White paper at (1,1), 62x62px, corner radius 6px, 1px neutral outline.
- Red strip at (0,0), 64x27px, clipped to the paper shape.
- Fit the actual glyph contours of the two-letter Russian weekday into (5,3,54,20), in white Segoe UI Bold. This removes the normal font padding.
- Fit the day glyph contours into (4,30,56,30), in near-black Segoe UI Bold.
- Keep the background transparent and the icon's own paper opaque so it works on light and dark taskbars.
- The full local date appears as the taskbar title for accessibility and hover text.

## Behavior and verification

Update the icon at local midnight, after a time change, after sleep, and on repeated launch. Retry transient write failures. Keep one stable ICO path and notify Explorer of affected own shortcuts. Use the native executable identity and an ordinary Start menu shortcut. Do not override AppUserModelID or window relaunch properties. Keep the installed executable name WinDate.exe stable for startup and pinned shortcuts. Every shipping icon frame uses native 32-bit DIB.

Native tests verify no content, minimized startup, blocked restore, repeat-launch refresh, date boundaries, icon replacement and Exit. A real taskbar screenshot confirms the weekday/day icon is visible. Launch through the real Explorer broker; a temporary PowerShell parent reproduced a generic taskbar icon after it exited. Actual pinned-icon rollover after real midnight remains unverified. The earlier calendar window and its render previews have been removed.
