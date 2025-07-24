using System;

namespace Infastructure.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public string PlayerName;
        public string LevelName;

        public int Rating;
        public int NumberOfCoins;
        public int NumberOfLifes;

        public PlayerProgress(string playerName, int rating, string levelName)
        {
            PlayerName = playerName;
            Rating = rating;
            LevelName = levelName;
        }

        public void CleanUp()
        {
            Rating = 0;
            NumberOfCoins = 0;
            NumberOfLifes = 3;
        }
    }


    [Serializable]
    public class PlayerProgressLeaderBoard
    {
        public string PlayerName;
        public int Rating;
        public string LevelName;

        public PlayerProgressLeaderBoard(string playerName, int rating, string levelName)
        {
            PlayerName = playerName;
            Rating = rating;
            LevelName = levelName;
        }
    }
}