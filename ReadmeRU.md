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
* Python 3 (только для пакетирования релизных сборок)

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

Создание релизных архивов:

```bash
./publish.py windows linux osx
```

Архивы попадают в `bin/publish/` (`SS14.Launcher_Windows.zip` и т.д.). Флаг `--x64-only` позволяет пропустить сборки для arm64.
