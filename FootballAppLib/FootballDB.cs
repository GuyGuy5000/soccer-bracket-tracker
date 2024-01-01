using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballAppLib
{
    /// <summary>
    /// Created By: Nadav Hilu
    /// Purpose: Data layer classes to represent all tables in FifaDB.
    /// Last Modified: 2023-06-12
    /// </summary>
    public class TEAM
    {
        public int teamID { get; set; }
        public string teamName { get; set; }
        public int teamWins { get; set; }
        public int teamLoses { get; set; }
        public int teamDraws { get; set; }
        public int teamGoalDiff { get; set; }
        public int teamPoints { get; set; }
        public string teamGroup { get; set; }


        public TEAM()
        {

        }

        public TEAM(string teamName, int teamWins, int teamLoses, int teamDraws, int teamGoalDiff, int teamPoints, string teamGroup)
        {
            this.teamName = teamName;
            this.teamWins = teamWins;
            this.teamLoses = teamLoses;
            this.teamDraws = teamDraws;
            this.teamGoalDiff = teamGoalDiff;
            this.teamPoints = teamPoints;
            this.teamGroup = teamGroup;
        }

        public override string ToString()
        {
            return $"{teamName}";
        }

        public string TeamInfo()
        {
            return $"({teamGroup}) {teamName} - W: {teamWins} | L: {teamLoses} | D: {teamDraws} | GD: {teamGoalDiff} | Pts: {teamPoints}";
        }
    }

    public class MATCH
    {
        public int matchID { get; set; }
        public string matchTOne { get; set; }
        public string matchTTwo { get; set; }
        public int matchTOneScore { get; set; }
        public int matchTTwoScore { get; set; }
        public string matchPlayersScored { get; set; }

        public MATCH()
        {

        }

        public MATCH(string matchTOne, string matchTTwo, int matchTOneScore, int matchTTwoScore, string matchPlayersScored)
        {
            this.matchTOne = matchTOne;
            this.matchTTwo = matchTTwo;
            this.matchTOneScore = matchTOneScore;
            this.matchTTwoScore = matchTTwoScore;
            this.matchPlayersScored = matchPlayersScored;
        }

        public override string ToString()
        {
            return $"{matchTOne} vs. {matchTTwo}: [{matchTOneScore} - {matchTTwoScore}]";
        }
    }


    public class MATCH_TEAM
    {
        public int teamOneID { get; set; }
        public int teamTwoID { get; set; }
        public int matchID { get; set; }
    }
}
