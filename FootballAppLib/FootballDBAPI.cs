using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballAppLib
{
    /// <summary>
    /// Created By: Nadav Hilu
    /// Purpose: Class containing methods for interfacing with a custom made FIFA DB.
    /// Last Modified: 2023-06-27
    /// </summary>
    public class FOOTBALLDBAPI
    {
        private IDbConnection db;

        #region CreationScript
        private static string dbCreationScript1 =
            @"
            IF NOT EXISTS (SELECT name FROM master.sys.databases WHERE name = N'FOOTBALLDB')
            BEGIN
	            CREATE DATABASE [FOOTBALLDB]

	            ALTER DATABASE [FOOTBALLDB]
	            SET RECOVERY SIMPLE

            END";

        private static string dbCreationScript2 =
            @"CREATE TABLE [TEAM]
	        (
		        teamID		 INT IDENTITY(1,1),
		        teamName	 VARCHAR (50) NOT NULL,
		        teamWins	 INT DEFAULT 0,
		        teamLoses	 INT DEFAULT 0,
		        teamDraws	 INT DEFAULT 0,
		        teamGoalDiff INT DEFAULT 0,
		        teamPoints	 INT DEFAULT 0,
		        teamGroup	 CHAR(1) NOT NULL,
		        CONSTRAINT pk_teamID_team PRIMARY KEY(teamID)
	        )
	        CREATE TABLE [MATCH]
	        (
		        matchID				INT IDENTITY(1,1),
		        matchTOne			VARCHAR(50) NOT NULL,
		        matchTTwo			VARCHAR(50) NOT NULL,
		        matchTOneScore		INT,
		        matchTTwoScore		INT,
		        matchPlayersScored	VARCHAR(1000),
		        CONSTRAINT pk_matchID_match PRIMARY KEY(matchID)
	        )
	        CREATE TABLE [MATCH_TEAM]
	        (
		        teamOneID	INT,
		        teamTwoID	INT,
		        matchID		INT,
		        CONSTRAINT fk_teamoneID_matchteam FOREIGN KEY(teamoneID) REFERENCES [TEAM](teamID),
		        CONSTRAINT fk_teamtwoID_matchteam FOREIGN KEY(teamtwoID) REFERENCES [TEAM](teamID),
		        CONSTRAINT fk_matchID_matchteam FOREIGN KEY(matchID) REFERENCES [MATCH](matchID)
	        )";

        private static string dbCreationScript3 =
            @"CREATE OR ALTER PROCEDURE [usp_selectAll_team]
            AS
            BEGIN
	            SET NOCOUNT ON
	            SELECT * FROM [TEAM]
            END";

        private static string dbCreationScript4 =
            @"CREATE OR ALTER PROCEDURE [usp_selectAll_match]
            AS
            BEGIN
	            SET NOCOUNT ON
	            SELECT * FROM [MATCH]
            END";
        private static string dbCreationScript5 =
            @"CREATE OR ALTER PROCEDURE [usp_innerjoin_matchteam]
            AS
            BEGIN
	            SET NOCOUNT ON
	            SELECT * FROM [TEAM] T JOIN [MATCH_TEAM] MT
	            ON T.teamID = MT.teamoneID OR T.teamID = MT.teamtwoID INNER JOIN [MATCH] M
	            ON MT.matchID = M.matchID
            END";

        private static string dbCreationScript6 =
            @"CREATE OR ALTER PROCEDURE [usp_insert_team]
	            @teamName		VARCHAR(50),
	            @teamWins		INT,
	            @teamLoses		INT,
	            @teamDraws		INT,
	            @teamGoalDiff	INT,
	            @teamPoints		INT,
	            @teamGroup		CHAR(1),
	            @teamID			INT	OUTPUT
            AS
            BEGIN
	            INSERT INTO TEAM (teamName, teamWins, teamLoses, teamDraws, teamGoalDiff, teamPoints, teamGroup)
	            VALUES (@teamName, @teamWins, @teamLoses, @teamDraws, @teamGoalDiff, @teamPoints, @teamGroup)
	            SELECT @teamID = teamID FROM TEAM WHERE @@ROWCOUNT = 1
	            RETURN @teamID
            END";

        private static string dbCreationScript7 =
            @"CREATE OR ALTER PROCEDURE [usp_insert_match]
	            @matchTOne			VARCHAR(50),
	            @matchTTwo			VARCHAR(50),
	            @matchTOneScore		INT,
	            @matchTTwoScore		INT,
	            @matchPlayersScored VARCHAR(1000),
	            @matchID			INT OUTPUT
            AS
            BEGIN
	            INSERT INTO MATCH (matchTOne, matchTTwo, matchTOneScore, matchTTwoScore, matchPlayersScored)
	            VALUES (@matchTOne, @matchTTwo, @matchTOneScore, @matchTTwoScore, @matchPlayersScored)
	            SELECT @matchID = @@ROWCOUNT
	            RETURN @matchID
            END";

        private static string dbCreationScript8 =
            @"CREATE OR ALTER PROCEDURE [usp_update_team]
	            @teamID			INT,
	            @teamName		VARCHAR(50),
	            @teamWins		INT,
	            @teamLoses		INT,
	            @teamDraws		INT,
	            @teamGoalDiff	INT,
	            @teamPoints		INT,
	            @teamGroup		CHAR(1),
	            @succeeded		INT OUTPUT
            AS
            BEGIN
	            SET NOCOUNT OFF
	            UPDATE [TEAM]
	            SET 	teamName = @teamName,
		            teamWins = @teamWins,
		            teamLoses = @teamLoses,
		            teamDraws = @teamDraws,
		            teamGoalDiff = @teamGoalDiff,
		            teamPoints = @teamPoints,
		            teamGroup = @teamGroup
	            WHERE teamID = @teamID
	            SELECT @succeeded = @@ROWCOUNT
	            RETURN @succeeded
            END";

        private static string dbCreationScript9 =
            @"CREATE OR ALTER PROCEDURE [usp_update_match]
	            @matchID		INT,
	            @matchTOne		VARCHAR(50),
	            @matchTTwo		VARCHAR(50),
	            @matchTOneScore INT,
	            @matchTTwoScore INT,
                @matchPlayersScored VARCHAR (1000),
	            @succeeded		INT OUTPUT
            AS
            BEGIN
	            SET NOCOUNT OFF

	            UPDATE [MATCH]
	            SET matchTOne = @matchTOne,
		            matchTTwo = @matchTTwo,
		            matchTOneScore = @matchTOneScore,
		            matchTTwoScore = matchTTwoScore
	            WHERE matchID = @matchID
	            SELECT @succeeded = @@ROWCOUNT
	            RETURN @succeeded
            END";

        private static string dbCreationScript10 =
            @"CREATE OR ALTER PROCEDURE [usp_delete_team]
	            @teamID INT,
	            @succeeded INT OUTPUT
            AS
            BEGIN
	            SET NOCOUNT OFF
	            DELETE FROM [TEAM]
	            WHERE teamID = @teamID
	            SELECT @succeeded = @@ROWCOUNT
	            RETURN @succeeded
            END";

        private static string dbCreationScript11 =
            @"CREATE OR ALTER PROCEDURE [usp_delete_match]
	            @matchID INT,
	            @succeeded INT OUTPUT
            AS
            BEGIN
	            SET NOCOUNT OFF
	            DELETE FROM [MATCH]
	            WHERE matchID = @matchID
	            SELECT @succeeded = @@ROWCOUNT
	            RETURN @succeeded
            END";
        #endregion
        public FOOTBALLDBAPI(string connStr)
        {
            db = new SqlConnection(connStr);
            int tries = 0;
            while (tries < 5)
            {
                try
                {
                    db.Query(dbCreationScript1);
                    db.Close();
                    db = new SqlConnection(@"Data Source=.\SQLEXPRESS;Initial Catalog=FOOTBALLDB;Integrated Security=True");
                    dynamic n = db.Query(@"IF OBJECT_ID (N'TEAM', N'U') IS NOT NULL 
                                            SELECT 1 AS res ELSE SELECT -1 AS res;").SingleOrDefault();
                    if (n.res == -1)
                    {
                        db.Query(dbCreationScript2);
                        db.Query(dbCreationScript3);
                        db.Query(dbCreationScript4);
                        db.Query(dbCreationScript5);
                        db.Query(dbCreationScript6);
                        db.Query(dbCreationScript7);
                        db.Query(dbCreationScript8);
                        db.Query(dbCreationScript9);
                        db.Query(dbCreationScript10);
                        db.Query(dbCreationScript11);
                    }
                    tries = 5;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    tries++;
                }
            }
        }

        public List<TEAM> GetTeams()
        {
            string sql = "usp_selectAll_team";

            try
            {
                List<TEAM> list = db.Query<TEAM>(sql).ToList();
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<MATCH> GetMatches()
        {
            string sql = "usp_selectAll_match";

            try
            {
                List<MATCH> list = db.Query<MATCH>(sql).ToList();
                return list;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<dynamic> JoinTeamMatch()
        {
            string sql = "usp_innerjoin_matchteam";

            return db.Query<dynamic>(sql).ToList();

        }

        public TEAM InsertTeam(TEAM team)
        {
            string sql = $"usp_insert_team";
            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@teamName", team.teamName);
            dynamicParameters.Add("@teamWins", team.teamWins);
            dynamicParameters.Add("@teamLoses", team.teamLoses);
            dynamicParameters.Add("@teamDraws", team.teamDraws);
            dynamicParameters.Add("@teamGoalDiff", team.teamGoalDiff);
            dynamicParameters.Add("@teamPoints", team.teamPoints);
            dynamicParameters.Add("@teamGroup", team.teamGroup);
            dynamicParameters.Add("@teamID", team.teamID, direction: ParameterDirection.Output);

            db.Execute(sql, dynamicParameters, commandType: CommandType.StoredProcedure);
            team.teamID = dynamicParameters.Get<int>("@teamID");

            return team;
        }

        public MATCH InsertMatch(MATCH match)
        {
            string sql = "usp_insert_match";
            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@matchTOne", match.matchTOne);
            dynamicParameters.Add("@matchTTwo", match.matchTTwo);
            dynamicParameters.Add("@matchTOneScore", match.matchTOneScore);
            dynamicParameters.Add("@matchTTwoScore", match.matchTTwoScore);
            dynamicParameters.Add("@matchPlayersScored", match.matchPlayersScored);
            dynamicParameters.Add("@matchID", match.matchID);

            db.Execute(sql, dynamicParameters, commandType: CommandType.StoredProcedure);
            match.matchID = dynamicParameters.Get<int>("@matchID");

            return match;

        }

        public int UpdateTeam(TEAM team)
        {
            int succeeded = 0;
            string sql = $"usp_update_team";
            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@teamID", team.teamID);
            dynamicParameters.Add("@teamName", team.teamName);
            dynamicParameters.Add("@teamWins", team.teamWins);
            dynamicParameters.Add("@teamLoses", team.teamLoses);
            dynamicParameters.Add("@teamDraws", team.teamDraws);
            dynamicParameters.Add("@teamGoalDiff", team.teamGoalDiff);
            dynamicParameters.Add("@teamPoints", team.teamPoints);
            dynamicParameters.Add("@teamGroup", team.teamGroup);
            dynamicParameters.Add("@succeeded", succeeded, direction: ParameterDirection.Output);

            succeeded = db.Execute(sql, dynamicParameters, commandType: CommandType.StoredProcedure);
            return succeeded;
        }


        public int UpdateMatch(MATCH match)
        {
            int succeeded = 0;
            string sql = "usp_update_match";
            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@matchID", match.matchID);
            dynamicParameters.Add("@matchTOne", match.matchTOne);
            dynamicParameters.Add("@matchTTwo", match.matchTTwo);
            dynamicParameters.Add("@matchTOneScore", match.matchTOneScore);
            dynamicParameters.Add("@matchTTwoScore", match.matchTTwoScore);
            dynamicParameters.Add("@matchPlayersScored", match.matchPlayersScored);
            dynamicParameters.Add("@succeeded", succeeded, direction: ParameterDirection.Output);

            succeeded = db.Execute(sql, dynamicParameters, commandType: CommandType.StoredProcedure);
            return succeeded;
        }

        public int DeleteTeam(TEAM team)
        {
            int succeeded = 0;
            string sql = "usp_delete_team";
            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@teamID", team.teamID);
            dynamicParameters.Add("@succeeded", succeeded, direction: ParameterDirection.Output);

            succeeded = db.Execute(sql, dynamicParameters, commandType: CommandType.StoredProcedure);
            return succeeded;

        }

        public int DeleteMatch(MATCH match)
        {
            int succeeded = 0;
            string sql = "usp_delete_match";
            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@matchID", match.matchID);
            dynamicParameters.Add("@succeeded", succeeded, direction: ParameterDirection.Output);

            succeeded = db.Execute(sql, dynamicParameters, commandType: CommandType.StoredProcedure);
            return succeeded;

        }
    }
}
