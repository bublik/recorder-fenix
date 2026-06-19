# SDRSharp Audio Recorder Plugin — Fenix Fork

Форк плагіна **Audio Recorder** для SDR# (by Vasili / thewraith).  
Містить декомпільований вихідний код версії `1.3.10.0` з виправленими критичними помилками.

---

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
├── SDRSharp.PluginsCom.dll
├── Audio_Recorder.pdf                  — оригінальна документація
└── changelog.txt
```

---

## Як зібрати нову версію (Windows)

> **Збірка можлива тільки на Windows.** Плагін цілить у `.NET Framework 4.6` і використовує Windows Forms — ці технології не підтримуються на Linux/macOS.

### Вимоги

- **Windows 10/11**
- **Visual Studio 2022** (Community або вище) із компонентом:
  - `.NET desktop development`
- **SDR#** — встановлена версія (потрібні DLL з папки інсталяції)

### Крок 1 — Скопіювати залежності SDR#

Скопіюйте наступні файли з папки SDR# (наприклад `C:\SDRSharp\`) в папку `src/` проєкту:

```
SDRSharp.Common.dll
SDRSharp.Radio.dll
```

> Ці файли не включені в репозиторій — вони є частиною SDR# і мають різні версії залежно від збірки SDR#.

### Крок 2 — Відкрити проєкт

```
src/SDRSharp.AudioRecorder.csproj
```

Відкрийте у Visual Studio 2022 (подвійний клік або `File → Open → Project`).

### Крок 3 — Перевірити посилання

У Solution Explorer → References переконайтесь що `SDRSharp.Common` і `SDRSharp.Radio` не мають жовтого знаку питання. Якщо є — правою кнопкою → `Add Reference` → вкажіть шлях до скопійованих DLL.

### Крок 4 — Збілдити

```
Build → Build Solution   (Ctrl+Shift+B)
```

Або через командний рядок Developer Command Prompt:

```cmd
cd path\to\recorder-fenix\src
msbuild SDRSharp.AudioRecorder.csproj /p:Configuration=Release
```

Результат: `src\bin\Release\SDRSharp.AudioRecorder.dll`

### Крок 5 — Встановити плагін

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