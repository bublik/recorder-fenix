using System;

namespace SDRSharp.AudioRecorder;

// Гейт VOX з гістерезисом: відкривається при рівні >= OpenDb, закривається при рівні < CloseDb.
// Чиста логіка без залежностей від SDR#/WinForms — покривається юніт-тестами.
public sealed class VoxGate
{
	public const double FloorDb = -100.0;

	public double OpenDb { get; set; } = -30.0;

	public double CloseDb { get; set; } = -40.0;

	public bool IsOpen { get; private set; }

	public double LastLevelDb { get; private set; } = FloorDb;

	// Поріг закриття не може бути вищим за поріг відкриття: інакше рівень між ними
	// відкривав би й закривав гейт на кожному тіку («тріпотіння» запису).
	// Захищає і від некоректних значень, вписаних у конфіг вручну.
	public double EffectiveCloseDb => Math.Min(CloseDb, OpenDb);

	public static bool IsValidThresholds(double openDb, double closeDb)
	{
		return closeDb <= openDb;
	}

	public bool Update(double levelDb)
	{
		LastLevelDb = levelDb;
		if (IsOpen)
		{
			if (levelDb < EffectiveCloseDb)
			{
				IsOpen = false;
			}
		}
		else if (levelDb >= OpenDb)
		{
			IsOpen = true;
		}
		return IsOpen;
	}

	public void Reset()
	{
		IsOpen = false;
	}
}
