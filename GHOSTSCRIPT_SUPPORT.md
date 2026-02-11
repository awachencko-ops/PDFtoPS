# Ghostscript support matrix for PDFtoPS

## Обнаружение Ghostscript
Приложение ищет `gswin64c.exe` / `gswin32c.exe` в следующем порядке:
1. Переменная окружения `GHOSTSCRIPT_PATH`
2. Portable рядом с приложением:
   - `<app>/gswin64c.exe`
   - `<app>/gswin32c.exe`
   - `<app>/gs/bin/gswin64c.exe`
   - `<app>/gs/bin/gswin32c.exe`
3. Стандартные установки Windows:
   - `C:\Program Files\gs\<version>\bin\gswin64c.exe`
   - `C:\Program Files (x86)\gs\<version>\bin\gswin32c.exe`

Это позволяет использовать как системную установку, так и portable-режим (локальная папка `gs` рядом с `.exe`).

## Рекомендованные версии Ghostscript
Для стабильной работы рекомендуются версии:
- **10.0+** (основной целевой диапазон)
- **9.56+** (минимально рекомендуемая ветка)

## Примечания по эксплуатации
- Для проблемных файлов включены таймаут, ретраи и проверка факта создания выходного `.ps`.
- Все ошибки и диагностическая информация пишутся в `logs/yyyyMMdd.log`.
- Если Ghostscript не найден на старте, приложение пишет предупреждение health-check в лог.
