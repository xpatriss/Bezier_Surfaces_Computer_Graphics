using System.Globalization;
using BezierSurfaces.Core.Model;

namespace BezierSurfaces.Core.Files;

/// czyta z pliku tekstowego siatki punktów kontrolnych
public static class SurfaceFileReader
{
    /// składa kolejne grupy 16 punktów w osobne siatki
    public static List<ControlGrid> Read(string path)
    {
        var values = new List<float>();
        int lineNumber = 0;
        foreach (string raw in File.ReadLines(path))
        {
            lineNumber++;
            string line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                throw new FormatException($"linia {lineNumber} nie ma trzech liczb");
            }

            values.Add(Parse(parts[0], lineNumber));
            values.Add(Parse(parts[1], lineNumber));
            values.Add(Parse(parts[2], lineNumber));
        }

        if (values.Count < 16 * 3 * 2 || values.Count % (16 * 3) != 0)
        {
            throw new FormatException("plik musi zawierać całe powierzchnie po 16 punktów i co najmniej dwie");
        }

        var grids = new List<ControlGrid>();
        for (int offset = 0; offset < values.Count; offset += 48)
        {
            var grid = new ControlGrid();
            int cursor = offset;
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    grid[row, column] = new System.Numerics.Vector3(
                        values[cursor],
                        values[cursor + 1],
                        values[cursor + 2]);
                    cursor += 3;
                }
            }

            grids.Add(grid);
        }

        return grids;
    }

    /// zamienia tekst z wiersza pliku na liczbę
    private static float Parse(string text, int lineNumber)
    {
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            throw new FormatException($"linia {lineNumber} ma niepoprawną liczbę");
        }

        return value;
    }
}
