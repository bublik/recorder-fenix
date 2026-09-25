# SDRSharp Audio Recorder Plugin — Fenix Fork

Форк плагіна **Audio Recorder** для SDR# (by Vasili / thewraith).  
Містить декомпільований вихідний код версії `1.3.10.0` з виправленими критичними помилками.
Версія `1.3.11.0` — фікси; версія `1.3.13.0` — додано режим **VOX** (гейтинг запису за рівнем
аудіо), який працює для SSB (USB/LSB)/CW, де штатний squelch SDR# недоступний.

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

## Що додано у v1.3.12.0
 - додано GitHub Action: автоматична збірка і реліз (x86 та x64).

## Що додано у v1.3.13.0 — режим VOX

Штатний squelch SDR# амплітудно-шумовий і **вимкнений для SSB (USB/LSB) та CW**. Через це опція
`Use squelch` у цих режимах фактично «завжди відкрита», і `Don't write pause` писав безперервно
замість нарізки на окремі повідомлення.

Додано **окремий режим VOX** — гейтинг за рівнем самого демодульованого аудіо, що працює для
будь-якої модуляції (зокрема SSB/CW/AM):

- `RecordingAudioProcessor` безперервно міряє пік аудіо в `Process()` (незалежно від стану
  запису/паузи) і віддає його як `Decibels` (піко-холд зі скиданням при зчитуванні) / `LastDecibels`.
- **Гістерезис** двома порогами: `open ≥ dB` відкриває гейт, `close < dB` — закриває.
  Типові значення: open −30 dB, close −40 dB. Комбінується з hang-time `Continue recording`.
- VOX — **окремий режим** і має пріоритет над squelch у `SignalIsActive()`; поки VOX активний,
  чекбокси `squelch` і `mute` вимкнені.
- Аудіопроцесор лишається ввімкненим, поки запис озброєно, тож VOX «бачить» початок передачі ще
  до першого `RecordStart()` (і між файлами).
- Нові налаштування: `AudioRecorder.UseAudioVox`, `AudioRecorder.VoxOpenDb`, `AudioRecorder.VoxCloseDb`.
- Метадані в імені файлу (частота, тривалість) — без змін, за наявними правилами `FileName`.

### Як користуватись

1. **Configure → Recorder options** → увімкнути `Don't write pause` і `VOX — audio level (SSB/CW/AM)`.
2. Виставити пороги (почати з open ≈ −30, close ≈ −40 dB), орієнтуючись на живий рівень `VOX dB`
   у debug-рядку (подвійний клік по індикатору запису вмикає debug).
3. `Record` — пишуться лише повідомлення, окремими файлами.

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
├── ref/
│   ├── net8/                           — DLL SDR# rev 1921 під .NET 8 (тільки для компіляції)
│   ├── sdk-net9/                       — SDK SDR# rev 1921 (.NET 9), для довідки, у збірці не використовується
│   └── original-1.3.10/                — оригінальний SDRSharp.AudioRecorder.dll v1.3.10.0
├── Audio_Recorder.pdf                  — оригінальна документація
└── changelog.txt
```

---

## Як зібрати нову версію

Плагін цілить у **`net8.0-windows`** (`UseWindowsForms`). Одна й та сама DLL працює в обох
збірках SDR# — `SDRSharp.dotnet8.exe` і `SDRSharp.dotnet9.exe` (рантайм .NET 9 вантажить net8-збірки).

Компіляція йде проти DLL з `ref/net8/` — це `SDRSharp.Common/Radio/PluginsCom.dll` rev 1921,
витягнуті з single-file бандла `SDRSharp.dotnet8.exe` (`sfextract`). Їхній публічний API збігається
з SDK (net9) та з хостом net9. Ці DLL потрібні **лише при компіляції** (`Private=false`) і в
дистрибутив не потрапляють: під час роботи плагін використовує DLL самого SDR#.
SDK-DLL з `ref/sdk-net9/` зібрані під .NET 9, тому з net8-таргетом дають `CS1705`.

> ℹ️ **Збірка на Linux/macOS** працює, але потрібен прапор `-p:EnableWindowsTargeting=true`
> (на не-Windows таргет `*-windows` інакше не резолвиться). На **Windows** прапор не потрібен.

### Вимоги

- **.NET SDK 8.0 або новіший** ([dotnet.microsoft.com/download](https://dotnet.microsoft.com/download))
  — на Windows підійде Visual Studio 2022 з компонентом `.NET desktop development`.
- **SDR#** — потрібні три DLL під .NET 8: `SDRSharp.Common.dll`, `SDRSharp.Radio.dll`,
  `SDRSharp.PluginsCom.dll`. Вони вже лежать у `ref/net8/` (rev 1921). Щоб оновити їх під
  нову ревізію SDR#:
  ```bash
  dotnet tool install -g sfextract
  sfextract "C:\Program Files\sdrsharp\SDRSharp.dotnet8.exe" -o extracted
  # скопіюйте extracted/SDRSharp.{Common,Radio,PluginsCom}.dll у ref/net8/
  ```

### Крок 1 — Очистити старі артефакти

Якщо раніше збирали зі старим csproj — приберіть кеш:

```bash
cd src/SDRSharp.AudioRecorder
rm -rf obj bin          # Windows: rmdir /s /q obj bin
```

### Крок 2 — Зібрати

```bash
cd src/SDRSharp.AudioRecorder

# Windows:
dotnet build SDRSharp.AudioRecorder.csproj -c Release

# Linux/macOS (потрібен прапор крос-таргетингу):
dotnet build SDRSharp.AudioRecorder.csproj -c Release -p:EnableWindowsTargeting=true
```

Результат: `src/SDRSharp.AudioRecorder/bin/<Platform>/Release/net8.0-windows/SDRSharp.AudioRecorder.dll`

### Крок 3 — Перевірити збірку

Очікувано **`0 Warning(s), 0 Error(s)`**. Якщо `CS1705 ... System.Runtime Version=9.0.0.0 higher
version` — у `ref/net8/` потрапили DLL з net9-збірки SDR# (або з SDK); візьміть їх із
`SDRSharp.dotnet8.exe`.

### Крок 4 — Встановити плагін

1. Скопіюйте **тільки** `SDRSharp.AudioRecorder.dll` в папку SDR#. `SDRSharp.Common/Radio/PluginsCom.dll`
   класти поруч **не треба**: вони вже є в SDR#, а дублікати дають `Assembly with same name is already loaded`
   у `PluginError.log`.
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
