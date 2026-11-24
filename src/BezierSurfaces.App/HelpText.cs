namespace BezierSurfaces.App;

/// tekst okna pomocy, zgodny z tym, co jest w panelu
static class HelpText
{
    public const string Body = """
        Instrukcja

        Lewy przycisk i przeciągnięcie w obszarze rysowania obraca scenę.
        Ruch w prawo kręci wokół pionowej osi ekranu (y).
        Ruch w górę albo w dół kręci wokół poziomej osi ekranu (x).
        Prawy przycisk kręci wokół osi prostopadłej do ekranu (z).
        Kąt nie jest obcinany. Każdy ruch dokłada się do aktualnej orientacji.
        Zakładka „Widok” zawiera suwak podziału i trzy warstwy rysunku.
        Przycisk „Widok z góry” ustawia orientację bez obrotu.

        Pomarańczowe uchwyty sterują torem lampy. Przeciągnij uchwyt lewym
        przyciskiem, żeby zmienić krzywą na płaszczyźnie stałego z.
        Suwak z przesuwa całą krzywą. Pole „Animuj światło” rusza punkt po krzywej.
        Kliknięcie powierzchni wybiera ją w zakładce „Materiał”.
        Można też wybrać powierzchnię z listy i ustawić osobny kolor lub obraz.
        Pole „Mapa normalnych” ugina normalną według obrazu, bez ruszania siatki.

        Skróty
        F1 — ta pomoc
        Spacja — zatrzymaj albo wznów animację światła

        Jak ruch myszy staje się obrotem

        Orientacja sceny jest macierzą 3×3. Po ruchu o co najmniej 3 piksele
        zaczynamy obrót od pozycji wciśnięcia. Dalej liczymy przyrost od
        poprzedniej pozycji kursora.
        Lewy przycisk: alfa = dy * 0,01 radiana, beta = dx * 0,01 radiana.
        Przyrost to Rx(alfa) * Ry(beta).
        Prawy przycisk: gamma = dx * 0,01, przyrost to Rz(gamma).
        Nowa orientacja: Rnowa = Rprzyrost * Raktualna.
        Po złożeniu przywracamy jednostkowe prostopadłe osie macierzy,
        żeby kolejne obroty zachowywały długości. Następny ruch używa Rnowa.

        Punkty powierzchni mnożymy przez tę macierz wokół (0, 0, 0).
        Wektory styczne i normalne mnożymy tak samo, jako kierunki.
        Po obrocie normalna jest normalizowana.
        Krzywa światła nie jest mnożona przez macierz sceny.
        Zostaje w układzie obserwatora, więc obrót zmienia oświetlenie obiektu.
        Na ekran idą współrzędne x i y po obrocie.
        Środek obszaru rysowania to (0, 0), a oś y rośnie w górę.

        Kolor piksela

        Rozproszenie jest proporcjonalne do |n·l|, więc działa po obu stronach.
        Połysk liczymy po tej samej stronie płata co lampa, gdy v·r > 0.
        Sumę składników mnożymy przez kolor światła i materiału dla każdego kanału.
        Wszelkie nasycenie kanałów odbywa się przy konwersji do 0–255.
        Bliższy piksel wygrywa większym z.
        """;
}
