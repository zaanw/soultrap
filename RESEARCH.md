# Исследование bootstrapper-проектов

Исходники загружены с GitHub 2026-11-03. Это исследование исходного кода, не испытание Roblox на Windows. Первоначальная версия реализована самостоятельно. В 0.2.0 адаптированы MIT-макеты загрузочного диалога Voidstrap; лицензии сохранены в legal/. Курсоры, логотипы, шрифты и бинарные файлы upstream не перенесены. Подробности: SETTINGS-COMPATIBILITY.md.

| Проект | Проверенный репозиторий / commit | Технологии | Лицензия |
|---|---|---|---|
| Bloxstrap | https://github.com/bloxstraplabs/bloxstrap / `02db76006741f039b79ac7547b8054d7f3ccfc05` | C#, .NET 6 Windows, WPF, собственный fork WPF UI | [MIT](https://github.com/bloxstraplabs/bloxstrap/blob/main/LICENSE), © 2022 pizzaboxer |
| Fishstrap | https://github.com/fishstrap/fishstrap / `c4e8ffbddfb625ac54d77525f59f009881d4ae28` | fork Bloxstrap, C#, .NET 6 Windows, WPF | [MIT](https://github.com/fishstrap/fishstrap/blob/main/LICENSE), © 2025 returnrqt |
| Voidstrap | https://github.com/KloBraticc/Voidstrap / `c6e3904646b457a601c07ce18b41b4010e732032` | разделённые desktop/platform/core проекты; Windows entry point .NET 10; Avalonia 12.1.2 в новом shared desktop; legacy WPF/LibreWPF в дереве исходников | [MIT для собственного кода](https://github.com/KloBraticc/Voidstrap/blob/main/LICENSE.VOIDSTRAP), © 2026 Bratic; отдельные notices Bloxstrap/Fishstrap |

Официальность Fishstrap подтверждена предупреждением README о двух официальных источниках: этот репозиторий и fishstrap.app. Voidstrap найден поиском GitHub; README взаимно связывает репозиторий, release и сайт voidstrapp.pages.dev. Репозиторий `voidstrap/voidstrap` не существует: он не используется как источник.

## Сравнение механизмов

| Область | Bloxstrap | Fishstrap | Voidstrap |
|---|---|---|---|
| Установка / запуск | `Bootstrapper.cs`, `LaunchHandler.cs`: загрузка Roblox, установка, передача запуска | сохраняет bootstrapper, добавляет Studio | `src/Voidstrap.App/Bootstrapper.cs`, platform abstractions; несколько платформ |
| Обновление / версии | version/package manifest, загрузка архивов, версия в локальном состоянии | channel switching, cache, manifest, Studio/Player distinction | manifest и платформенные пути; отдельная обработка сетевых/IO ошибок |
| Кастомизация | наложение файлов из Modifications, сравнение с установленными файлами | расширяет файловые моды, шрифты, Global Basic Settings | расширенные моды/настройки, платформенные ограничения |
| Курсоры | собственные ресурсы проекта From2006/From2013, подмена файлов content | наследует курсоры и mods | дополнительные cursor packs, включая Bibata с GPLv3 notice |
| FastFlags | `FastFlagManager.cs`: ClientSettings/ClientAppSettings.json; небольшой набор rendering presets | редактор JSON; README явно предупреждает об allowlist | отдельный FastFlagManager и модели; наличие редактора не доказывает поддержку флага клиентом |
| Настройки | JSON manager, state и settings, WPF UI | JSON настройки, дополнительные editors | persistable AppSettings и платформенные конфигурации |
| Ошибки | Logger, bootstrapper dialogs, исключения сетевых операций | логирование и статусы установки, обработка auto-update failures | явные ветки cancellation, insufficient space, network и cleanup errors |

Bloxstrap README сообщает, что активная разработка прекращена. README Fishstrap ссылается на ограничение Roblox: https://devforum.roblox.com/t/allowlist-for-local-client-configuration-via-fast-flags/3966569 . Прямой HTTPS-запрос к DevForum вернул CONNECT 403 из сетевой политики среды, поэтому содержимое первичного документа не проверено; нельзя считать список из чужого исходника вечным актуальным allowlist.

Проверенные файлы: README, LICENSE / LICENSE.VOIDSTRAP, global.json, csproj, Bootstrapper.cs, FastFlagManager.cs; в Voidstrap также desktop Windows csproj и лицензионная таблица README. Это выборочное исследование, не аудит безопасности всех трёх проектов.

## Решения Soultrap

* Собственная MVVM реализация, DI, .NET 8; адаптированные MIT-макеты загрузочного диалога перечислены в SETTINGS-COMPATIBILITY.md, а прочие ресурсы собственные.
* Обнаружение обычного Win32 Roblox и распространённых bootstrapper-каталогов; запуск через зарегистрированный roblox-player протокол / официальный bootstrapper. Установка и обновления принадлежат официальному Roblox, без собственного непроверенного загрузчика клиентских архивов.
* Только два PNG-файла курсора в известных content-путях. Проверка версии, отсутствие reparse points, журнал до записи, SHA-256, обратный откат, отказ при конфликте. Файловая совместимость не означает подтверждённый визуальный эффект в Roblox.
* Classic UI — локальный интерактивный предпросмотр. Исторические CoreGui, HUD и игровые меню не заявлены как применимые к актуальному клиенту.
* FastFlags: импорт/экспорт/валидация и категории работают локально. Применение заблокировано без проверенного актуального allowlist. Это намеренное ограничение, не обход Roblox.
* Не запускать код из mods. Разрешены только известные PNG назначения, manifest и ограниченный размер архива.
* Обновления Soultrap: GitHub release metadata, HTTPS; загрузка и проверка SHA-256 не равна авторству. Автозапуск обновления запрещён без закреплённого издателя Authenticode. Пока сертификата нет, установка вручную.
* Owner — повторная проверка Windows credentials текущего SID, DPAPI marker. Значок не даёт Roblox-привилегий. Windows Hello не заявлен реализованным.

## Лицензии ресурсов

У каждого upstream есть самостоятельные лицензии зависимостей. В частности, GPLv3 Bibata не становится MIT из-за лицензии Voidstrap. Soultrap использует только собственные геометрические cursor-art и стандартные системные шрифты; оригинальные Roblox assets не распространяются.
