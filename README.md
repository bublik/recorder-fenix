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
├── SDRSharp.AudioRecorder.dll          — оригінальний DLL v1.3.10.0
├── SDRSharp.Common.dll                 — залежність SDR# (rev 1921)
├── SDRSharp.Radio.dll                  — залежність SDR# (rev 1921)
├── SDRSharp.PluginsCom.dll
├── Audio_Recorder.pdf                  — оригінальна документація
└── changelog.txt
```

---

## Як зібрати нову версію

Плагін цілить у **`net9.0-windows`** (`UseWindowsForms`), бо DLL сучасних збірок SDR#
(rev 1921 у корені репозиторію) самі під **.NET 9**. Старіші ревізії README згадували
`.NET Framework 4.6` — це **застаріло**: проти net9-збірок SDR# плагін має бути теж net9,
інакше отримаєте `CS1705 ... uses System.Runtime Version=9.0.0.0 which has a higher version`.

> ℹ️ **Збірка на Linux/macOS** працює, але потрібен прапор `-p:EnableWindowsTargeting=true`
> (на не-Windows таргет `*-windows` інакше не резолвиться). На **Windows** прапор не потрібен.

### Вимоги

- **.NET SDK 9.0 або новіший** ([dotnet.microsoft.com/download](https://dotnet.microsoft.com/download))
  — на Windows підійде Visual Studio 2022 з компонентом `.NET desktop development`.
  (SDK 8.0 НЕ підійде — не вміє таргетити `net9.0`.)
- **SDR#** — потрібні три DLL: `SDRSharp.Common.dll`, `SDRSharp.Radio.dll`,
  `SDRSharp.PluginsCom.dll`. Вони вже лежать у корені репозиторію (rev 1921);
  за потреби замініть їх на DLL зі своєї версії SDR# (і, якщо вона старіша/інша за .NET,
  узгодьте `TargetFramework` у `.csproj`).

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

Результат: `src/SDRSharp.AudioRecorder/bin/Release/net9.0-windows/SDRSharp.AudioRecorder.dll`

### Крок 3 — Перевірити збірку

Очікувано **`0 Warning(s), 0 Error(s)`**. Якщо бачите `NETSDK1045 ... does not support
targeting .NET 9.0` — встановлено застарілий SDK (8.0 чи нижче), потрібен **.NET SDK 9.0+**.
Якщо `CS1705 ... System.Runtime Version=9.0.0.0 higher version` — таргет проєкту не збігається
з .NET-версією DLL SDR#; узгодьте `TargetFramework` у `.csproj`.

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
