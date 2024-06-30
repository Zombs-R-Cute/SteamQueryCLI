using System;
using System.Collections.Generic;
using System.Timers;
using SteamQuery;

namespace SteamQueryCLI
{
    public class Server
    {
        public delegate void Callback(ServerState serverState);

        public static async void GetServerData(ServerData serverData, Callback callback)
        {
            var timer = new Timer();
            List<PlayerData> players = new List<PlayerData>();
            var serverState = new ServerState(serverData.name, players, "Timed out", "0/0");
            timer.Elapsed += (sender, args) => callback(serverState);

            timer.Interval = 3000;
            timer.AutoReset = false;
            timer.Start();


            using var server = new GameServer(serverData.ip, serverData.port);
            try
            {
                await server.PerformQueryAsync();
            }
            catch (Exception e)
            {
                timer.Dispose();
                // send the saved name
                serverState.State = "Offline or Rebooting";
                callback(serverState);
                return;
            }

            timer.Dispose();


            int index = 1;
            foreach (var player in server.Players)
            {
                players.Add(new PlayerData(index++, player.Name, player.Duration));
            }
            serverState.State = "Online";
            serverState.ServerName = server.Information.ServerName;
            serverState.PlayersToMax = $"{server.Players.Count}/{server.Information.MaxPlayers}";
            serverState.Server = serverData;
            foreach (var rule in server.Rules)
            {
                if(rule.Name.Equals("rocketplugins"))
                    serverState.ServerPlugins = rule.Value;
            }
            callback(serverState);
        }
    }
}