namespace BezierSurfaces.App;

/// tekst okna pomocy, zgodny z tym, co jest w panelu
static class HelpText
{
    public const string Body = """
        Instrukcja

        Lewy przycisk w obszarze rysowania obraca scenę.
        Ruch w prawo kręci wokół pionowej osi ekranu (y).
        Ruch w górę albo w dół kręci wokół poziomej osi ekranu (x).
        Prawy przycisk kręci wokół osi prostopadłej do ekranu (z).
        Kąt nie jest obcinany. Każdy ruch dokłada się do aktualnej orientacji.
        Przycisk „Widok z góry” wstawia macierz identyczności.

        Punkty na pomarańczowej krzywej to punkty kontrolne światła.
        Przeciągnięcie takiego punktu zmienia krzywą na płaszczyźnie stałego z.
        Suwak z przesuwa całą krzywą. Pole „Animuj światło” rusza punkt po krzywej.
        Kliknięcie powierzchni wybiera ją do ustawienia koloru albo tekstury.
        Pole „Mapa normalnych” ugina normalną według obrazu, bez ruszania siatki.

        Skróty
        F1 — ta pomoc
        Spacja — zatrzymaj albo wznów animację światła

        Jak ruch myszy staje się obrotem

        Orientacja sceny jest macierzą 3×3, nie trzema kątami.
        Przy ruchu myszy liczymy tylko przyrost od poprzedniej pozycji kursora.
        Lewy przycisk: alfa = dy * 0,01 radiana, beta = dx * 0,01 radiana.
        Przyrost to Rx(alfa) * Ry(beta).
        Prawy przycisk: gamma = dx * 0,01, przyrost to Rz(gamma).
        Nowa orientacja: Rnowa = Rprzyrost * Raktualna.
        Następny ruch bierze już Rnowa, a nie pozycję z początku przeciągnięcia.

        Punkty powierzchni mnożymy przez tę macierz wokół (0, 0, 0).
        Wektory styczne i normalne mnożymy tak samo, jako kierunki.
        Po obrocie normalna jest normalizowana.
        Krzywa światła nie jest mnożona przez macierz sceny.
        Zostaje w układzie obserwatora, więc obrót zmienia oświetlenie obiektu.
        Na ekran idą współrzędne x i y po obrocie.
        Środek obszaru rysowania to (0, 0), a oś y rośnie w górę.

        Kolor piksela

        i = kd · il · io · max(n·l, 0) + ks · il · io · max(v·r, 0)^m
        Nie ma składnika ambient. v = (0, 0, 1).
        Bliższy piksel wygrywa większym z.
        """;
}
