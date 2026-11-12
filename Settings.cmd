@echo off
if exist "%APPDATA%\Microsoft\Windows\Start Menu\Programs\WinCalendar\WinCalendar Settings.lnk" (
  explorer.exe "%APPDATA%\Microsoft\Windows\Start Menu\Programs\WinCalendar\WinCalendar Settings.lnk"
) else (
  echo Run Start.cmd first.
  pause
)
