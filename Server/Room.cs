using System;
using System.Collections.Generic;
using System.Linq;

namespace VoxelTanksServer
{
    public class Room
    {
        public bool IsOpen = true;
        public int MaxPlayers;
        private int PlayersPerTeam;
        public List<Team?> Teams;
        public Map Map;

        public int PlayersCount => Players.Count;

        public Dictionary<int, Client?> Players = new();

        public List<CachedPlayer?> CachedPlayers = new();

        public Room(int maxPlayers)
        {
            MaxPlayers = maxPlayers;
            PlayersPerTeam = 1;
            Map = Server.Maps[new Random().Next(Server.Maps.Count)];

            SpawnPoint[] firstTeamSpawns = new SpawnPoint[Map.FirstTeamSpawns.Count];
            SpawnPoint[] secondTeamSpawns = new SpawnPoint[Map.SecondTeamSpawns.Count];
            Map.FirstTeamSpawns.CopyTo(firstTeamSpawns);
            Map.SecondTeamSpawns.CopyTo(secondTeamSpawns);
            
            Teams = new List<Team?>
            {
                new(1, firstTeamSpawns.ToList()), new(2, secondTeamSpawns.ToList())
            };
            Server.Rooms.Add(this);
        }

        public void BalanceTeams()
        {
            foreach (var client in Players.Values)
            {
                int randomTeam = 0;
                Team? playerTeam;
                
                do
                {
                    randomTeam = new Random().Next(1, 3);
                    playerTeam = Teams.Find(team => team != null && team.ID == randomTeam);
                } while (playerTeam != null && playerTeam.Players.Count == PlayersPerTeam);
                
                playerTeam?.Players.Add(client);
                client.Team = playerTeam;

                var openPoints = playerTeam.SpawnPoints.FindAll(point => point.IsOpen);
                var point = openPoints[new Random().Next(openPoints.Count)];
                point.IsOpen = false;

                client.Position = point.Position;
                client.Rotation = point.Rotation;
            }

            ServerSend.LoadScene(this, Map.Name);
        }
    }
}