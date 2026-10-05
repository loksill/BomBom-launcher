# BomBom Launcher

[English version](Readme.md)

discord: 

**BomBom** — это форк [helix-launcher](https://github.com/banumbas/helix-launcher) с прикрученным функционалом Marsey.

На данный момент BomBom находится в стадии активной разработки и тестирования.

Наша цель — создание лаунчера с максимумом возможностей без каких-либо ограничений.

# Лицензия

Исходный код лаунчера Space Station 14, унаследованный от upstream-проекта, сохраняет условия лицензии MIT - см. [LICENSE.txt](LICENSE.txt) и GNU Affero General Public License v3.0 (AGPL-3.0-only) - см. [LICENSE-AGPL-3.0.txt](LICENSE-AGPL-3.0.txt).

Все изменения, созданные в рамках этого форка, распространяются по лицензии GNU Affero General Public License v3.0 (AGPL-3.0-only) - см. [LICENSE-AGPL-3.0.txt](LICENSE-AGPL-3.0.txt).

# Возможности

* Ресурспаки
* Плагины (Harmony-патчи и Subversion)
* Отображение текущего режима игры, карты и пинга прямо в лаунчере
* Переработанное меню с возможность кастомизации
* Кастомный Discord RPC
* Система конфигов

# Ресурспаки

Ресурспаки заменяют файлы игры по пути к ним во время запуска.

Каталог пакетов:
* `%AppData%/Space Station 14/launcher/resource_packs/<Имя пакета>` в Windows по умолчанию
* `~/.local/share/Space Station 14/launcher/resource_packs/<Имя пакета>` в Linux по умолчанию (если задан, используется `$XDG_DATA_HOME`)

Минимальная структура ресурспака:

```text
resource_packs/
  MyPack/
    meta.json
    Resources/
      Textures/
      Locale/
```

Минимальный `meta.json`:

```json
{
  "name": "Имя",
  "description": "Описание",
  "target": ""
}
```

Записи:
* Файлы переопределяются их относительным путем внутри `Resources/`.
* `target` необязательно. Оставьте это поле пустым, чтобы применить пакет к любому форку.
* Если вы переопределяете файлы в каталоге `.rsi`, сохраняйте правильный `.rsi/meta.json` рядом с измененными текстурами.
* Только `Audio/`, `Fonts/`, `Locale/`, `Shaders/`, и `Textures/` монтируются в пак.

# Сборка

Требования:
* [.NET SDK 10.0](https://dotnet.microsoft.com/download)
* Git (используются сабмодули)
* Python 3 (нужен для пакетной сборки)
* Доступ в интернет (при первой сборке качается .NET Runtime)

Клонирование и сборка:

```bash
git clone --recursive https://github.com/loksill/BomBom-launcher.git
cd BomBom-launcher
dotnet restore
dotnet build --configuration Release
```

Запуск лаунчера:

```bash
dotnet run --project SS14.Launcher/SS14.Launcher.csproj
```

Запуск тестов:

```bash
dotnet test
```

## Пакетная сборка в исполняемые файлы

Скрипт `publish.py` одной командой собирает релизные (Release) исполняемые файлы для указанных платформ, скачивает под них .NET Runtime, раскладывает всё по каталогам и упаковывает результат в zip-архивы. `build_release.py` — обёртка над ним, которая перед упаковкой сама готовит bootstrap (см. «Сборка Windows-пакета на Linux/macOS»).

### Одной командой

```bash
./build_release.py                      # Windows + Linux + macOS
./build_release.py windows              # только Windows
./build_release.py windows linux --x64-only
```

Флаги `build_release.py`:
* `--x64-only` — пропустить сборки для arm64;
* `--rebuild-bootstrap` — пересобрать bootstrap, даже если копия уже есть;
* `--no-bootstrap` — не трогать bootstrap (нужен exe из корня репозитория);
* `--bootstrap-only` — только подготовить bootstrap, без упаковки.

В конце печатаются собранные архивы с размерами.

### Ручной запуск publish.py

Сборка всех платформ разом:

```bash
./publish.py windows linux osx
```

Только нужные платформы (допустимы `windows`, `linux`, `osx` в любой комбинации):

```bash
./publish.py linux
./publish.py windows osx
```

Только x64, без arm64 (собирается быстрее):

```bash
./publish.py windows linux --x64-only
```

Результат:
* Архивы создаются в корне репозитория: `SS14.Launcher_Windows.zip`, `SS14.Launcher_Linux.zip`, `SS14.Launcher_macOS.zip`.
* Промежуточные файлы раскладываются в `bin/publish/<Платформа>/`.

Перед каждой сборкой скрипт удаляет каталоги `bin` всех проектов, поэтому каждый запуск идёт с чистого состояния. Первый запуск дополнительно качает .NET Runtime (десятки мегабайт) в `Dependencies/dotnet/` — дальше он переиспользуется.

### Структура пакета

На примере Linux:

```text
SS14.Launcher_Linux.zip
├── SS14.Launcher        # скрипт-обёртка: поднимает DOTNET_ROOT и запускает бинарник
├── SS14.desktop         # ярлык для рабочего стола Linux
├── bin_x64/
│   ├── SS14.Launcher    # исполняемый файл лаунчера
│   ├── loader/          # SS14.Loader (загрузчик игры)
│   └── BomBom/Mods/     # пустая папка под моды
└── dotnet_x64/          # скачанный .NET Runtime
```

Если не передан `--x64-only`, рядом появляются `bin_arm64/` и `dotnet_arm64/`. Для Windows в корень архива кладутся `Space Station 14 Launcher.exe` (bootstrap) и `console.bat`, для macOS — `Space Station 14 Launcher.app`.

Запуск собранного пакета:
* Windows: распаковать архив и запустить `Space Station 14 Launcher.exe`.
* Linux: распаковать архив, выполнить `chmod +x SS14.Launcher`, запустить `./SS14.Launcher`.

### Сборка Windows-пакета на Linux/macOS

Bootstrap — самостоятельный `Space Station 14 Launcher.exe` (NativeAOT, `net10.0-windows`) — на Windows собирает сам `publish.py`. На Linux/macOS нативную AOT-сборку сделать нельзя (`Cross-OS native compilation is not supported`), поэтому есть два пути:

1. **Автоматически** — `./build_release.py windows` сам кросс-собирает bootstrap (IL, self-contained, single-file) и кладёт его в `Dependencies/bootstrap/`. Работает везде, где есть .NET SDK, но exe выходит крупным (~36 МиБ вместо ~1 МиБ у AOT-варианта) — учитывайте это в размере пакета.
2. **Вручную** — соберите AOT-bootstrap на Windows и положите `Space Station 14 Launcher.exe` в корень репозитория (приоритетнее кэша; так поступает GitHub Actions в `.github/workflows/publish-release.yml`, собирая его на отдельном Windows-раннере):

```bash
# на Windows
dotnet publish SS14.Launcher.Bootstrap/SS14.Launcher.Bootstrap.csproj -c Release -r win-x64
```

Bootstrap ищется в таком порядке: `Space Station 14 Launcher.exe` в корне репозитория → `Dependencies/bootstrap/` → свежая сборка (только на Windows). Если нигде не найден, сборка остановится с сообщением `Bootstrap executable not found`.

Для Windows-бинарников скрипт дополнительно выставляет PE-подсистему в GUI (`exe_set_subsystem.py`), чтобы при запуске не открывалось консольное окно.

### Сборка одного исполняемого файла без упаковки

Если пакет не нужен, достаточно обычного `dotnet publish` (флаг `/p:FullRelease=True` отключает режим разработки):

```bash
dotnet publish SS14.Launcher/SS14.Launcher.csproj -c Release -r win-x64 --self-contained false /p:FullRelease=True
```

Готовый `SS14.Launcher.exe` окажется в `SS14.Launcher/bin/Release/net10.0/win-x64/publish/`. В качестве `-r` подходят `win-x64`, `linux-x64`, `osx-x64` и их arm64-варианты.

Такой бинарник запускается на машине с установленным .NET Runtime 10.0. Для раздачи без внешних зависимостей используйте `publish.py` — он кладёт runtime внутрь архива.
