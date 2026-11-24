using BezierSurfaces.Core.Lighting;

namespace BezierSurfaces.App;

/// czyta mapę normalnych i zamienia kolor sceny na kolor okna
static class BitmapImages
{
    /// ładuje png, jpg albo bmp i odkłada kanały wierszami od góry
    public static RgbImage LoadNormalMap(string path)
    {
        using var bitmap = new Bitmap(path);
        int width = bitmap.Width;
        int height = bitmap.Height;
        var rgb = new float[checked(width * height * 3)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                if (color.B < 128)
                {
                    throw new InvalidDataException("Niebieski kanał mapy normalnych musi mieścić się w zakresie 128–255.");
                }

                int index = (y * width + x) * 3;
                rgb[index] = color.R / 255f;
                rgb[index + 1] = color.G / 255f;
                rgb[index + 2] = color.B / 255f;
            }
        }

        return new RgbImage(width, height, rgb);
    }

    /// zamienia kolor 0–1 na kolor przycisku
    public static Color ToMedia(Vector3Color color)
    {
        return Color.FromArgb(LightingEquation.ToBgra(color));
    }
}
