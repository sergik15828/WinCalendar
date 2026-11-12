# Подключение Google Calendar

Настройка выполняется один раз в своём Google Cloud проекте. WinCalendar читает основной календарь выбранного аккаунта; встречи не создаёт и не изменяет. Авторизация приложения независима от подключения Google в Codex.

## 1. Создать проект и включить API

1. Откройте [Google Cloud Console](https://console.cloud.google.com/projectcreate) под своим Google-аккаунтом.
2. Создайте проект, например `WinCalendar Personal`, и выберите его в верхней панели.
3. Откройте [Google Calendar API](https://console.cloud.google.com/apis/library/calendar-json.googleapis.com) и нажмите **Enable / Включить**.

## 2. Настроить вход Google

1. Откройте [Google Auth Platform](https://console.cloud.google.com/auth/overview). Если предложен мастер — нажмите **Get started**.
2. В **Branding** укажите имя приложения `WinCalendar` и свой адрес как контактный адрес поддержки и разработчика.
3. В **Audience** выберите **External** для обычного Google-аккаунта. В разделе **Test users** добавьте адрес аккаунта, чей календарь будете подключать. Пока используйте статус **Testing**.
4. В **Data Access → Add or remove scopes** добавьте только `https://www.googleapis.com/auth/calendar.events.readonly`.

В режиме Testing разрешение с таким доступом обычно истекает через 7 дней: потребуется повторный вход. Для постоянного использования можно перевести приложение в Production; публичное распространение и требования Google к проверке OAuth — отдельная настройка. [Правила срока действия токенов](https://developers.google.com/identity/protocols/oauth2#expiration).

## 3. Создать OAuth-клиент

1. В [Clients](https://console.cloud.google.com/auth/clients) нажмите **Create client**.
2. Тип приложения — **Desktop app**, имя — `WinCalendar Windows`.
3. Сохраните **Client ID** и **Client secret** у себя. Не присылайте значения секретов, скачанный JSON или токены в чат и не добавляйте их в репозиторий.

Настраивать домен, публичный сервер и redirect URI для Desktop app не требуется: приложение принимает ответ браузера на временном адресе `127.0.0.1`.

## 4. Подключить WinCalendar

1. Нажмите на дату в панели задач → **Войти в Google**.
2. При первом подключении откроется окно настройки клиента. Вставьте **Client ID** и, если Google выдал его, **Client secret** прямо в это окно. Секретное поле скрывает текст.
3. После сохранения откроется системный браузер. Выберите аккаунт, добавленный в Test users, и разрешите чтение событий календаря.
4. После страницы «Ответ Google получен» снова откройте дату на панели задач. Приложение завершит обмен разрешения и загрузит встречи.

Дни со встречами отмечены точкой. Выберите день: под сеткой появятся время и названия событий. Нажатие на событие откроет его в Google Calendar. Длинные списки прокручиваются; Enter открывает выбранную встречу. Время встреч отображается в часовом поясе Windows.

## Хранение и отключение

OAuth-клиент и refresh token хранятся в Диспетчере учётных данных Windows в собственных записях WinCalendar. Access token остаётся только в памяти. Приложение не записывает их в обычные файлы и логи.

Кеш событий шифруется Windows DPAPI для текущего пользователя и хранится в `%LOCALAPPDATA%\WinCalendar\GoogleCache`. При отсутствии сети панель показывает последний доступный список и время его обновления. У другого аккаунта отдельный кеш. **Отключить** удаляет локальную сессию и кеш, а также пытается отозвать доступ в Google. Если в этот момент нет сети, завершить отзыв можно в [настройках доступа Google](https://myaccount.google.com/connections).

Если Google сообщает `access_denied`, проверьте Test users и выбранный аккаунт. При `invalid_client` проверьте тип Desktop app, Client ID и Client secret. Если вход истёк, повторите **Войти в Google**. Настроить другого OAuth-клиента можно через меню WinCalendar Settings.

Реализация следует [OAuth для Desktop apps](https://developers.google.com/identity/protocols/oauth2/native-app) с PKCE и loopback callback и использует [events.list](https://developers.google.com/workspace/calendar/api/v3/reference/events/list) с раскрытием повторяющихся событий и обработкой страниц.
