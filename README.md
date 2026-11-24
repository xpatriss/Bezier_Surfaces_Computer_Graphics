# Powierzchnie Béziera

Dwa płaty Béziera trzeciego stopnia z pliku `data/surfaces.txt`. Program przybliża płaty trójkątami, ustala widoczność w buforze głębokości i liczy oświetlenie w pikselach.

## Uruchomienie

Wymagania to Windows i SDK .NET 10.

```powershell
dotnet run --project src/BezierSurfaces.App
```

Rozwiązanie to `BezierSurfaces.slnx`. `Core` liczy, `App` pokazuje okno. Dodatkowego NuGeta nie ma.

## Sterowanie

- Lewy przycisk przechyla bryłę, prawy kręci ją w płaszczyźnie ekranu.
- Zakładka „Widok” zawiera suwak gęstości i trzy warstwy sceny. „Widok z góry” ustawia orientację bez obrotu.
- Pomarańczowe uchwyty zmieniają tor lampy. W zakładce „Światło” suwak `z` zmienia wysokość toru. „Animuj światło” albo spacja przełącza ruch lampy.
- Kliknięcie widocznego płata wybiera go w zakładce „Materiał”. Każdy płat ma własny kolor i teksturę.
- „Mapa normalnych” ugina normalną według obrazu. Startowa mapa to spoiny cegieł.
- F1 albo „Pomoc” otwiera instrukcję.

Rozproszenie zależy od `|n·l|`. Refleks liczymy, gdy lampa i obserwator są po tej samej stronie oświetlanego fragmentu, a `v·r > 0`. Obie składowe mnożymy przez kolor materiału i lampy. Kanały nasycamy przy końcowym przeliczeniu do 0–255. Bliższy piksel ma większe `z`.

## Sprawdzenia algorytmów

```powershell
dotnet run --project tests/BezierSurfaces.Checks
```
