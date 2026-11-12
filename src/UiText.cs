using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace WinCalendar
{
    internal static class UiText
    {
        internal static string Language = "ru";
        internal static bool English { get { return Language == "en"; } }
        internal static CultureInfo Culture { get { return CultureInfo.GetCultureInfo(English ? "en-US" : "ru-RU"); } }
        private static readonly Dictionary<string, string> EnglishText = new Dictionary<string, string>
        {
            { "Календарь", "Calendar" }, { "Календарь ", "Calendar " }, { "Календари", "Calendars" },
            { "Настройки", "Settings" }, { "Параметры", "Preferences" }, { "Закрыть календарь", "Close calendar" },
            { "Предыдущий месяц", "Previous month" }, { "Следующий месяц", "Next month" },
            { "Пн", "Mon" }, { "Вт", "Tue" }, { "Ср", "Wed" }, { "Чт", "Thu" }, { "Пт", "Fri" }, { "Сб", "Sat" }, { "Вс", "Sun" },
            { "Встречи", "Meetings" }, { "Встречи выбранного дня", "Meetings for the selected day" },
            { "Открыть Google Calendar в браузере", "Open Google Calendar in browser" },
            { "Неделя", "Week" }, { "День", "Day" }, { "Сегодня", "Today" }, { "Перейти к сегодняшней дате", "Go to today" },
            { "Ближайшая встреча появится после загрузки", "Next meeting appears after loading" },
            { "Отложить на 5 мин", "Snooze for 5 min" }, { "Отложить на 10 мин", "Snooze for 10 min" },
            { "Войти в Google", "Sign in to Google" }, { "Обновить встречи", "Refresh meetings" },
            { "Открыть Google Calendar", "Open Google Calendar" }, { "Проверить напоминание", "Preview reminder" },
            { "Настроить подключение", "Configure connection" }, { "Отключить Google", "Disconnect Google" },
            { "Календари, звук и вид", "Calendars, sound and appearance" },
            { "Напоминания — как в событии Google.\nПульсация остаётся до нажатия на дату.", "Reminders follow Google event settings.\nAttention stays until you click the date." },
            { "Запускать с Windows", "Start with Windows" }, { "Выйти", "Quit" }, { "Вернуться к календарю", "Back to calendar" },
            { "Календари скрыты", "Calendars hidden" }, { ", сегодня", ", today" }, { ", выбрано", ", selected" }, { ", есть встречи", ", has meetings" },
            { "Подключите Google, чтобы видеть встречи.", "Connect Google to see meetings." },
            { "Подключите календари Google,\nчтобы видеть встречи выбранного дня.", "Connect Google calendars to see\nmeetings for the selected day." },
            { "Войдите в Google, чтобы проверить встречи\nво всех доступных календарях.", "Sign in to Google to check meetings\nacross your available calendars." },
            { "Проверяем встречи…", "Checking meetings…" }, { "Не удалось проверить встречи.\n", "Could not check meetings.\n" },
            { "Показаны последние сохранённые данные.", "Showing the last saved data." }, { "Список встреч пока недоступен.", "Meetings are not available yet." },
            { "На этот день встреч нет.", "No meetings on this day." }, { "На эту неделю встреч нет.", "No meetings this week." },
            { "На сегодня митов больше нет, приятного вечера", "No more meetings today. Have a lovely evening!" },
            { "Календари скрыты.\nВыберите их в настройках.", "Calendars are hidden.\nChoose calendars in settings." },
            { "Данные: ", "Updated: " }, { "Отмена входа", "Cancel sign-in" }, { "Сменить аккаунт", "Switch account" },
            { "Хранилище Windows недоступно. Повторите запуск приложения.", "Windows credential storage is unavailable. Restart the app." },
            { "Не удалось загрузить список Google.", "Could not load Google calendars." }, { "Не удалось сохранить настройки.", "Could not save preferences." },
            { "Не удалось сохранить данные в хранилище Windows.", "Could not save to Windows credential storage." },
            { "Завершите вход в открывшемся браузере.", "Complete sign-in in your browser." }, { "Вход отменён. Можно повторить.", "Sign-in cancelled. You can try again." },
            { "Не удалось войти в Google. Проверьте сеть и настройки.", "Google sign-in failed. Check your connection and settings." },
            { "Отключаем Google…", "Disconnecting Google…" },
            { "Локальный доступ удалён. Отзыв в Google не выполнен: нет связи.", "Local access removed. Could not revoke access in Google while offline." },
            { "Не удалось удалить доступ. Повторите отключение.", "Could not remove access. Try disconnecting again." },
            { "Встречи обновлены; локальный кеш недоступен.", "Meetings updated; local cache unavailable." },
            { "Превышено время ожидания. Повторите обновление.", "Request timed out. Refresh again." },
            { "Нет связи с Google. Проверьте интернет.", "Cannot reach Google. Check your connection." },
            { "Не удалось загрузить встречи. Повторите обновление.", "Could not load meetings. Refresh again." },
            { "Обновление встреч…", "Refreshing meetings…" }, { "Обновление будет повторено позже.", "Refresh will be retried later." },
            { "Календари Google", "Google calendars" }, { "Google отключён. Локальные встречи удалены.", "Google disconnected. Local meetings removed." },
            { "Данные подключения слишком велики для хранилища Windows.", "Connection details exceed the Windows storage limit." },
            { " (не удалось обновить закреплённый значок)", " (could not update pinned icon)" },
            { "Весь день", "All day" }, { "Ранее", "Earlier" }, { "далее", "continues" }, { "Без названия", "Untitled" },
            { "Занято", "Busy" }, { "Пересечение", "Overlap" }, { "Войти в Meet", "Join Meet" },
            { ", Завершено", ", Ended" }, { ", Пересечение", ", Overlap" }, { ", Войти в Meet: Alt+Enter", ", Join Meet: Alt+Enter" },
            { "Ближайших встреч нет", "No upcoming meetings" }, { "Сейчас", "Now" }, { "Через ", "In " }, { " мин", " min" },
            { "Подключение", "Connection" }, { "Данные OAuth-клиента Google", "Google OAuth client details" },
            { "Как настроить Google Cloud", "Google Cloud setup guide" },
            { "Данные сохраняются в хранилище Windows.\nПустое поле секрета сохраняет прежний\nсекрет для того же Client ID.", "Stored in Windows Credential Manager.\nLeave the secret blank to keep the saved\nsecret for the same Client ID." },
            { "Назад", "Back" }, { "Сохранить", "Save" }, { "Нужен Client ID клиента типа Desktop app.", "Enter a Desktop app client ID." },
            { "Нужен Client ID OAuth-клиента типа Desktop app из Google Cloud.", "Enter a Google Cloud Desktop app OAuth client ID." },
            { "Сначала настройте OAuth-клиент Google.", "Configure your Google OAuth client first." },
            { "Google не выдал доступ для фоновой работы. Повторите вход и разрешите чтение календаря.", "Google did not grant background access. Sign in again and allow calendar reading." },
            { "Доступ Google истёк или отозван. Войдите снова.", "Google access expired or was revoked. Sign in again." },
            { "Google не принял OAuth-клиент. Проверьте Client ID и Client secret в настройках.", "Google rejected the OAuth client. Check Client ID and Client secret in settings." },
            { "Не удалось получить доступ Google. Повторите вход позже.", "Could not get Google access. Try signing in later." },
            { "Нужно разрешение на список календарей. Нажмите «Войти в Google» и подтвердите доступ.", "Calendar list permission is required. Choose Sign in to Google and allow access." },
            { "Google не вернул доступ. Повторите вход.", "Google did not grant access. Sign in again." },
            { "Войдите в Google, чтобы видеть встречи.", "Sign in to Google to see meetings." },
            { "Не удалось получить полный список календарей. Повторите обновление.", "Could not get all calendars. Refresh again." },
            { "Google не вернул доступных календарей. Проверьте аккаунт или войдите снова.", "Google returned no available calendars. Check your account or sign in again." },
            { "Google вернул неполный список. Повторите обновление.", "Google returned an incomplete list. Refresh again." },
            { "Доступ к Google отозван. Войдите снова.", "Google access was revoked. Sign in again." },
            { "Google запретил чтение одного из календарей. Проверьте разрешения аккаунта.", "Google denied access to a calendar. Check account permissions." },
            { "Google временно ограничил запросы. Обновление повторится позже.", "Google rate limit reached. Refresh will be retried later." },
            { "Google Calendar временно недоступен. Попробуйте позже.", "Google Calendar is temporarily unavailable. Try again later." },
            { "Не удалось обновить доступ Google. Войдите снова.", "Could not refresh Google access. Sign in again." },
            { "Вход отменён или доступ к календарю не разрешён.", "Sign-in cancelled or calendar access denied." },
            { "Не удалось завершить вход в браузере. Повторите попытку.", "Could not complete browser sign-in. Try again." },
            { "Ответ Google получен. Можно закрыть эту вкладку и открыть WinCalendar.", "Google response received. You can close this tab and open WinCalendar." },
            { "Запрос входа не подтверждён. Вернитесь в WinCalendar и повторите вход.", "Sign-in was not verified. Return to WinCalendar and sign in again." },
            { "Звук и тишина", "Sound and quiet hours" }, { "Вид и приватность", "Appearance and privacy" }, { "Отображаемые календари", "Visible calendars" },
            { "Цвет выбранного календаря", "Selected calendar color" }, { "Цвет Google", "Google color" },
            { "Красный", "Red" }, { "Зелёный", "Green" }, { "Синий", "Blue" }, { "Фиолетовый", "Purple" }, { "Янтарный", "Amber" }, { "Розовый", "Pink" },
            { "Цвет календаря", "Calendar color" },
            { "Флажки влияют на встречи, отметки\nдней и напоминания. Цвета меняются\nтолько в WinCalendar.", "Selections affect meetings, day dots\nand reminders. Colors change\nonly in WinCalendar." },
            { "Обновить список", "Refresh list" }, { "Звук напоминаний", "Reminder sound" }, { "Мелодия", "Chime" },
            { "Мягкий перезвон", "Soft chime" }, { "Кристалл", "Crystal" }, { "Тёплый тон", "Warm tone" },
            { "Мелодия напоминания", "Reminder chime" }, { "Громкость", "Volume" }, { "Громкость · ", "Volume · " },
            { "Громкость напоминания", "Reminder volume" }, { "Прослушать", "Play preview" },
            { "Не удалось воспроизвести звук.", "Could not play the sound." }, { "Тихие часы", "Quiet hours" },
            { "С", "From" }, { "До", "Until" }, { "Начало тихих часов", "Quiet hours start" }, { "Конец тихих часов", "Quiet hours end" },
            { "В это время звук выключен,\nпульсация остаётся. Одинаковое\nвремя — тишина весь день.\nПрослушивание работает вручную.", "Sound is muted during quiet hours.\nVisual reminders stay on. Equal times\nmute the whole day.\nManual preview still plays." },
            { "Приватный режим: скрывать названия встреч и календарей", "Privacy: hide meeting and calendar names" },
            { "Ближайшая встреча при наведении на дату в панели задач", "Show next meeting when hovering over the date" },
            { "Интенсивность пульсации", "Reminder intensity" }, { "Деликатная", "Subtle" }, { "Обычная", "Normal" }, { "Выразительная", "Strong" },
            { "Учитываются настройки анимации\nи высокой контрастности Windows.\nНапоминание остаётся до клика.", "Respects Windows animation\nand high contrast settings.\nReminders stay until clicked." },
            { "Неизвестный параметр запуска. Откройте WinDate.exe без параметров.", "Unknown launch option. Open WinDate.exe without arguments." },
            { "Не удалось запустить индикатор даты.\n\n", "Could not start the date indicator.\n\n" },
            { "Не удалось открыть браузер. Выберите приложение по умолчанию для HTTPS в параметрах Windows и повторите попытку.", "Could not open a browser. Set a default HTTPS app in Windows settings and try again." },
            { "Текущая дата в панели задач", "Current date on the taskbar" },
            { "Дата рядом с погодой — открыть WinCalendar", "Date beside weather — open WinCalendar" }, { " — открыть WinCalendar", " — open WinCalendar" },
            { "Звук", "Sound" }, { "Вид", "Appearance" }, { "Язык и размещение", "Language and position" },
            { "Язык интерфейса", "Interface language" }, { "Виджет рядом с погодой", "Widget beside weather" },
            { "Выбрано", "Selected" }, { "Основное", "General" }, { "Анимация значка", "Icon animation" }, { "Зарплата", "Payday" }, { "зарплата", "payday" }, { "до зарплаты", "until payday" },
            { "Подключение аккаунта и обновление встреч", "Account connection and meeting refresh" },
            { "Включить тихие часы", "Enable quiet hours" },
            { "Звук выключен, а иконка продолжает\nподпрыгивать. Одинаковое время —\nтишина весь день. Прослушивание\nработает вручную.", "Sound is muted; the icon still bounces.\nEqual times mute the whole day.\nManual sound preview still plays." },
            { "Показывать дни до зарплаты", "Show days until payday" }, { "Выплаты", "Payments" }, { "1 раз в месяц", "Once a month" }, { "2 раза в месяц", "Twice a month" },
            { "Первая дата", "First day" }, { "Вторая дата", "Second day" }, { "Следующая выплата: ", "Next payday: " },
            { "В коротком месяце — последний день.\nВыходные не сдвигают дату выплаты.", "Short months use their last day.\nWeekends do not shift the date." },
            { "События Google не меняются.\nВесь день — по настройкам Google.", "Google events stay unchanged.\nAll-day follows Google settings." },
            { "Язык и виджет", "Language / UI" }, { "Напоминания", "Reminders" }, { "Время напоминания", "Reminder time" },
            { "Из настроек события", "From event settings" }, { "За 5 минут", "5 minutes before" }, { "За 2 минуты", "2 minutes before" },
            { "Только в WinCalendar", "Only in WinCalendar" },
            { "Выбор не меняет события Google.\nФиксированное время — для встреч\nс указанным временем начала.\nСобытия на весь день — как в Google.\n\nОтсрочка 5/10 минут работает отдельно.", "Google events stay unchanged.\nFixed timing applies to meetings\nwith a scheduled start time.\nAll-day events follow Google settings.\n\nSnooze for 5/10 min stays separate." },
            { "Локальное напоминание за ", "Local reminder " }, { " мин до встречи.\nИконка подпрыгивает до нажатия на дату.", " min before the meeting.\nThe icon bounces until you click the date." },
            { "Напоминания — как в событии Google.\nИконка подпрыгивает до нажатия на дату.", "Reminders follow Google event settings.\nThe icon bounces until you click the date." },
            { "Не удалось открыть ярлык приложения.", "Could not open the app shortcut." },
            { "Закреплённый значок", "Pinned app icon" }, { "Показать ярлык приложения", "Show app shortcut" },
            { "Нажмите правой кнопкой значок WinCalendar\nв панели задач → Открепить от панели задач.\nДля закрепления откройте ярлык ниже.", "Right-click WinCalendar on the taskbar\nand choose Unpin from taskbar.\nUse the shortcut below to pin it again." },
            { "Скрытие виджета не выключает напоминания.\nОткрыть настройки можно через меню Пуск\n→ WinCalendar Settings.", "Hiding the widget keeps reminders enabled.\nOpen settings from Start\n→ WinCalendar Settings." }
        };
        internal static string Get(string russian)
        { string value; return English && EnglishText.TryGetValue(russian, out value) ? value : russian; }
        internal static void Apply(Control root)
        {
            foreach (Control control in CalendarPanel.Descendants(root))
            {
                if (!(control is TextBox) && !(control is ListBox)) control.Text = Convert(control.Text);
                if (control.AccessibleName != null) control.AccessibleName = Convert(control.AccessibleName);
                ComboBox combo = control as ComboBox;
                if (combo != null) for (int i = 0; i < combo.Items.Count; i++) if (combo.Items[i] is string) combo.Items[i] = Convert((string)combo.Items[i]);
            }
        }
        private static string Convert(string text)
        {
            if (English) return Get(text);
            foreach (var pair in EnglishText) if (pair.Value == text) return pair.Key;
            return text;
        }
    }
}
