namespace WebOfPlanets
{
    // Globalno stanje igre (vidi GameManager); u vlastitoj datoteci umjesto
    // skriveno u GameManager.cs — enum koriste i UI i gameplay sustavi.
    public enum GameState { Playing, Paused, GameOver, Victory }
}
