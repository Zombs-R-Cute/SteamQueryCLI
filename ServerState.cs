using System.Collections.Generic;

namespace SteamQueryCLI
{
    public class ServerState
    {
        public string ServerName;
        public List<PlayerData> players;
        public string State;
        public string PlayersToMax;
        public string ServerPlugins;
        public ServerData Server;
        
        public ServerState(string serverName, List<PlayerData> players, string state, string playersToMax)
        {
            ServerName = serverName;
            this.players = players;
            State = state;
            this.PlayersToMax = playersToMax;
        }
        
    }
}