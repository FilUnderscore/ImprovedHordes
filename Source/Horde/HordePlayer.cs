using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Horde
{
    public sealed class HordePlayer
    {
        //const int STORED_HISTORY = 7;
        
        public readonly EntityPlayer playerEntityInstance;
        //public readonly Dictionary<int, GameStage> gamestageTrend = new Dictionary<int, GameStage>();

        public HordePlayer(EntityPlayer playerEntityInstance)
        {
            this.playerEntityInstance = playerEntityInstance;
        }

        /*
        public int GetAverageGamestage()
        {
            int gamestageDifference = 0;

            foreach(var trend in gamestageTrend.Values)
                gamestageDifference += trend.GetDifference();

            if(gamestageTrend.Count > 0)
                gamestageDifference /= gamestageTrend.Count;

            int gamestage = playerEntityInstance.gameStage + gamestageDifference;

            return gamestage;
        }

        public void Tick(ulong worldTime)
        {
            int day = GameUtils.WorldTimeToDays(worldTime);

            if(!gamestageTrend.ContainsKey(day))
            {
                int gamestage = playerEntityInstance.gameStage;

                gamestageTrend.Add(day, new GameStage(gamestage));

                if (gamestageTrend.ContainsKey(day - 1))
                {
                    gamestageTrend[day - 1].SetEndGamestage(gamestage);
                }

                for(int i = day - STORED_HISTORY * 2; i < day - STORED_HISTORY; i++)
                {
                    if(gamestageTrend.ContainsKey(i))
                        gamestageTrend.Remove(i);
                }
            }
        }

        public class GameStage
        {
            public int startGamestage;
            public int endGamestage;

            public GameStage(int startGamestage)
            {
                this.startGamestage = startGamestage;
                this.endGamestage = startGamestage;
            }

            public void SetEndGamestage(int endGamestage)
            {
                this.endGamestage = endGamestage;
            }

            public int GetDifference()
            {
                return endGamestage - startGamestage;
            }
        }
        */
    }
}