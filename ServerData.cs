namespace SteamQueryCLI
{
    public class ServerData
    {
        public string name;
        public string ip;
        public ushort port;
        
        public ServerData(string name, string ip, ushort port)
        {
            this.name = name;
            this.ip = ip;
            this.port = port;
        }
    }
}