# Powierzchnie Béziera

Dwie płaty sześcienne z pliku `data/surfaces.txt`. Program tnie je na trójkąty, wypełnia z z-buforem i liczy światło w każdym pikselu.

## Uruchomienie

```powershell
dotnet run --project src/BezierSurfaces.App
```

Rozwiązanie to `BezierSurfaces.slnx`. `Core` liczy, `App` pokazuje okno. Dodatkowego NuGeta nie ma.

## Sterowanie

- Lewy przycisk przechyla bryłę, prawy kręci ją w płaszczyźnie ekranu.
- „Widok z góry” wraca do macierzy identyczności.
- Pomarańczowe kwadraty kształtują krzywą światła. Suwak `z` podnosi całą krzywą. „Animuj światło” albo spacja rusza lampę.
- Kliknięcie płachty wybiera ją na liście. Można dać jej kolor albo teksturę.
- „Mapa normalnych” ugina normalną według obrazu. Startowa mapa to spoiny cegieł.
- F1 albo „Pomoc” otwiera instrukcję.

Wzór koloru, bez składnika ambient:

```text
i = kd · il · io · max(n·l, 0) + ks · il · io · max(v·r, 0)^m
```

Bliższy piksel ma większe `z`.
