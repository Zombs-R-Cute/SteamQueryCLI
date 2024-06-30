using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SteamQueryCLI
{
    public class ServerQueryCLI
    {
        private string _fileName = "servers.json";
        private int _serverQueryCount;
        private HashSet<ServerData> _servers;
        private string _search = "";
        private List<string> _searchResults = new List<string>();
        private static List<ServerState> _serverResponses = new List<ServerState>();
        private bool plugins;
        
        public ServerQueryCLI(string[] args)
        {
            LoadServers();
            var count = _servers.Count;
            ProcessArgs(args);
            if (_servers.Count != count)
                SaveServers();

            if (_servers.Count == 0)
                return;

            LoopServerInquiries();

            WaitForServerResponses();

            if (_search.Length == 0)
                return;

            Console.WriteLine($"\n\n** Search results for '{_search}': **");
            if (_searchResults.Count == 0)
                Console.WriteLine($"{_search} is not online.");
            foreach (var result in _searchResults)
            {
                Console.WriteLine(result);
            }
        }

        private void LoopServerInquiries()
        {
            foreach (var server in _servers)
            {
                Server.GetServerData(server, ServerCallback);
                _serverQueryCount++;
            }
        }

        public void LoadServers()
        {
            if (!File.Exists(_fileName))
            {
                _servers = new HashSet<ServerData>();
                return;
            }

            var text = File.ReadAllText(_fileName);
            _servers = JsonConvert.DeserializeObject<HashSet<ServerData>>(text);
        }

        private void SaveServers()
        {
            var output = JsonConvert.SerializeObject(_servers);
            File.WriteAllText(_fileName, output);
        }


        private void ProcessArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("usage:\n" +
                                  "ServerQueryCLI.exe /+ /- /name\n" +
                                  "/+ add a server /+127.0.0.1:27000:Name\n" +
                                  "/- remove server /-127.0.0.1:2700\n" +
                                  "/partialPlayerName lists names and servers of a Player's partial name\n \n \n");
                return;
            }

            var c = args[0].Substring(0, 1);
            if (c == "/")
            {
                c = args[0].Substring(1, 1);
            }
            else
            {
                return;
            }

           
            if (c == "+" || c == "-")
            {
                var data = args[0].Substring(2).Split(':');
                var ip = data[0];
                var port = UInt16.Parse(data[1]);
                string name = "";
                if (data.Length == 3)
                {
                    name = data[2];
                }

                if (data.Length < 3)
                    return;

                foreach (var server in _servers)
                {
                    if (server.ip.Equals(ip) && server.port.Equals(port))
                    {
                        if (c == "-")
                            _servers.Remove(server);
                        return;
                    }
                }

                _servers.Add(new ServerData(name, ip, port));
                return;
            }

            if (c == "R")
            {
                plugins = true;
                return;
            }            
            _search = args[0].Substring(1).ToLower();
        }


        private void WaitForServerResponses()
        {
            Console.WriteLine("\n\n");
            while (_serverQueryCount > 0)
            {
                lock (_serverResponses)
                {
                    for (int i = 0; i < _serverResponses.Count; i++)
                    {
                        var serverState = _serverResponses[0];
                        _serverResponses.RemoveAt(0);
                        PrintServer(serverState);
                    }
                }
            }
        }


        private void PrintServer(ServerState serverState)
        {
            Console.WriteLine($"{serverState.ServerName} -{serverState.State}- {serverState.PlayersToMax}");
            // Console.WriteLine(serverState.State);
            if (serverState.players.Count == 0 && serverState.State.Equals("Online"))
            {
                Console.WriteLine("*** No Players online ***");
            }

            foreach (var player in serverState.players)
            {
                if (_search != "" && player.name.ToLower().Contains(_search))
                    _searchResults.Add($"{player.name} in {serverState.ServerName}");

                Console.WriteLine($"{player.number} - {player.name} - {player.time}");
            }

            if(plugins)
                Console.WriteLine(serverState.ServerPlugins);
            Console.WriteLine("\n\n");
        }


        public void ServerCallback(ServerState serverState)
        {
            lock (_serverResponses)
            {
                _serverResponses.Add(serverState);
                _serverQueryCount--;
            }
        }
    }
}