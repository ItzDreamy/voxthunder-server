using System;
using System.Collections.Generic;
using System.Numerics;
using Serilog;
using Serilog.Core;

namespace VoxelTanksServer
{
    public class Room
    {
        public bool IsOpen = true;
        public int MaxPlayers;
        public int PlayersPerTeam;
        public List<Team?> Teams;
        public Map Map;

        public int PlayersCount => Players.Count;

        public Dictionary<int, Client?> Players = new Dictionary<int, Client?>();

        public List<CachedPlayer?> CachedPlayers = new List<CachedPlayer?>();

        public Room(int maxPlayers)
        {
            MaxPlayers = maxPlayers;
            PlayersPerTeam = 1;
            Map = Server.Maps[new Random().Next(Server.Maps.Count)];
            Teams = new List<Team?>
            {
                new Team(1), new Team(2)
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
                
                //client.Player = new Player(client.Id, client.Username, new Vector3(0, 0, 0), client.SelectedTank, this);
                
                Log.Information($"Player team: {randomTeam}");
                playerTeam.Players.Add(client);
                client.Team = playerTeam;
            }

            ServerSend.LoadScene(this, Map.Name);
        }
    }
}