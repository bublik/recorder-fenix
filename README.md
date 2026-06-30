# SDRSharp Audio Recorder Plugin — Fenix Fork

Форк плагіна **Audio Recorder** для SDR# (by Vasili / thewraith).  
Містить декомпільований вихідний код версії `1.3.10.0` з виправленими критичними помилками.
Нова версія з фіксами 1.3.11.

---
## Що виправлено у v1.3.12.0
 - добавили Github Action Build and Release

## Що виправлено у v1.3.11.0

### Критичні (можуть призвести до краша або пошкодження файлів)

| # | Файл | Проблема | Наслідки |
|---|------|----------|----------|
| 1 | `SimpleRecorder.cs` | `_diskWriterRunning` без `volatile` | Потік запису міг ніколи не отримати сигнал зупинки від UI — зависання при Stop |
| 2 | `SimpleRecorder.cs` | `_diskWriterPause` без `volatile` | Аналогічно — пауза могла не спрацьовувати |
| 3 | `SimpleRecorder.cs` | Use-after-free в drain-циклі `DiskWriterThread` | При одночасному `Dispose()` — звернення до звільненої пам'яті → краш |
| 4 | `SimpleRecorder.cs` + `SimpleWavWriter.cs` | WAV-заголовок не записувався при зупинці через `IsStreamFull` | Файл технічно пошкоджений до виклику `StopRecording()` |
| 5 | `SimpleRecorder.cs` | `_wavWriter` не закривався якщо `Join()` кидав виключення | Відкритий файл без коректного заголовку |

### Середні (некоректна поведінка)

| # | Файл | Проблема | Наслідки |
|---|------|----------|----------|
| 6 | `SimpleWavWriter.cs` | `UpdateLength()` при кожному аудіо-буфері | 2× Seek + 2× Write на кожен блок → надмірне навантаження на диск |
| 7 | `SimpleWavWriter.cs` | `StereoToMono` копіював тільки лівий канал | Правий канал повністю втрачався у PCM8Mono і PCM16Mono записах |
| 8 | `AudioRecorderPanel.cs` | Неправильна тривалість запису `_writeLength` | PCM16Stereo: у 2×, Float32: у 4×, PCM16Mono: у 2× завищена тривалість — хибний час у назві файлу і статистиці |
| 9 | `AudioRecorderPanel.cs` | Умова нового файлу при зміні частоти | Новий файл створювався навіть коли опція `NewFileFrequencyEnable` була вимкнена |

### Незначні

| # | Файл | Проблема | Наслідки |
|---|------|----------|----------|
| 10 | `AudioRecorderPanel.cs` | `_fileIndexer` не скидався між сесіями | Лічильник конфліктних імен файлів ріс між сесіями |

---

## Структура репозиторію

```
recorder-fenix/
├── src/
│   ├── SDRSharp.AudioRecorder.csproj   — проєктний файл
│   └── SDRSharp.AudioRecorder/
│       ├── AudioRecorderPanel.cs       — UI-панель і основна логіка
│       ├── AudioRecorderPlugin.cs      — точка входу плагіна
│       ├── DialogConfigure.cs          — вікно налаштувань
│       ├── MemoryEntry.cs              — запис з менеджера частот
│       ├── RecordingAudioProcessor.cs  — аудіо-процесор
│       ├── SamplesAvailableEventArgs.cs
│       ├── SettingsPersister.cs        — збереження налаштувань
│       ├── SimpleRecorder.cs           — логіка запису і потік DiskWriter
│       ├── SimpleWavWriter.cs          — запис WAV-файлів
│       ├── WavFormatHeader.cs
│       └── WavSampleFormat.cs
├── SDRSharp.AudioRecorder.dll          — оригінальний DLL v1.3.10.0
├── SDRSharp.Common.dll                 — залежність SDR# (rev 1921)
├── SDRSharp.Radio.dll                  — залежність SDR# (rev 1921)
├── SDRSharp.PluginsCom.dll
├── Audio_Recorder.pdf                  — оригінальна документація
└── changelog.txt
```

---

## Як зібрати нову версію

Плагін цілить у `.NET Framework 4.6`. Проєкт використовує SDK-стиль із пакетом
`Microsoft.NETFramework.ReferenceAssemblies.net46`, тому збирається як на **Windows**,
так і на **Linux/macOS** — потрібен лише .NET SDK (6.0+), без Visual Studio.

> ⚠️ **Важливо:** проєкт навмисно НЕ використовує `Microsoft.NET.Sdk.WindowsDesktop`
> і `UseWindowsForms`. Цей SDK на не-Windows (а також у деяких конфігураціях) підставляє
> у вихідний DLL збірки з **.NET Core / .NET 9** (наприклад `System.IO.FileSystem.DriveInfo
> v9.0.0.0`). SDR# працює на .NET Framework, де таких збірок немає, і плагін падає з
> `Could not load file or assembly ... Version=9.0.0.0`. Не повертайте WindowsDesktop SDK.

### Вимоги

- **.NET SDK 6.0 або новіший** ([dotnet.microsoft.com/download](https://dotnet.microsoft.com/download))
  — на Windows підійде Visual Studio 2022 з компонентом `.NET desktop development`.
- **SDR#** — потрібні три DLL: `SDRSharp.Common.dll`, `SDRSharp.Radio.dll`,
  `SDRSharp.PluginsCom.dll`. Вони вже лежать у корені репозиторію (rev 1921);
  за потреби замініть їх на DLL зі своєї версії SDR#.

### Крок 1 — Очистити старі артефакти

Якщо раніше збирали зі старим csproj — приберіть кеш:

```bash
cd src
rm -rf obj bin          # Windows: rmdir /s /q obj bin
```

### Крок 2 — Зібрати

```bash
cd src
dotnet build SDRSharp.AudioRecorder.csproj -c Release
```

Результат: `src/bin/Release/net46/SDRSharp.AudioRecorder.dll`

### Крок 3 — Перевірити залежності (рекомендовано)

У зібраному DLL **не повинно бути** жодної збірки з `Version=9.0.0.0` чи
`System.IO.FileSystem.DriveInfo`. Перевірити можна через ILSpy, `monodis --assemblyref`
або на Windows:

```cmd
dumpbin /dependents bin\Release\net46\SDRSharp.AudioRecorder.dll
```

`DriveInfo` має резолвитися з `mscorlib` (net46).

### Крок 4 — Встановити плагін

1. Скопіюйте `SDRSharp.AudioRecorder.dll` в папку SDR#.
2. Для SDR# **до v1800** — додайте в `Plugins.xml`:
   ```xml
   <add key="AudioRecorder" value="SDRSharp.AudioRecorder.AudioRecorderPlugin,SDRSharp.AudioRecorder" />
   ```
3. Для SDR# **v1800+** — скопіюйте DLL в папку `Plugins\`, редагувати XML не потрібно.
4. Перезапустіть SDR#.

---

## Джерело

Оригінальний плагін: **Audio Recorder** by Vasili (TSSDR) & Ian Gilmour, updated by thewraith.  
Остання публічна версія: `1.3.10.0` (грудень 2023).  
Декомпіляція: ILSpy v9.0 на Linux.
