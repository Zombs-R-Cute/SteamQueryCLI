using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace SteamQueryCLI
{
    public class ServerQueryCLI
    {
        private string _fileName = "servers.json";
        private int _serverQueryCount;
        private HashSet<ServerData> _servers;
        private Dictionary<string, List<string>> _searchResults = new Dictionary<string, List<string>>();
        private static List<ServerState> _serverResponses = new List<ServerState>();
        private bool _listPlugins;
        private bool _displayIpPort;
        private bool _help;
        
        public ServerQueryCLI(string[] args)
        {
            LoadServers();
            var count = _servers.Count;
            ProcessArgs(args);
            if(_help)
                return;
            if (_servers.Count != count)
                SaveServers();

            if (_servers.Count == 0)
                return;

            LoopServerInquiries();

            WaitForServerResponses();

            PrintSearchResults();
        }

        private void PrintSearchResults()
        {
            if (_searchResults.Count == 0)
                return;

            foreach (var s in _searchResults.Keys)
            {
                Console.WriteLine($"\n\n** Search results for '{s}': **");
                if (_searchResults[s].Count == 0)
                    Console.WriteLine($"{s} is not online.");
                foreach (var result in _searchResults[s])
                {
                    Console.WriteLine(result);
                }
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
            if (args.Length == 0 && !File.Exists(_fileName))
            {
                PrintHelp();
                return;
            }

            string c = "";
            bool searchArg = false;
            foreach (var arg in args)
            {
                if (searchArg)
                {
                    if (arg.Length < 3)
                    {
                        Console.WriteLine("You must supply at least 3 letters for a search.\n");
                        continue;
                    }

                    var lc = arg.ToLower();
                    _searchResults.Add(lc, new List<string>());

                    // _search.Add(arg.ToLower());
                    searchArg = false;
                    continue;
                }

                c = arg.Substring(0, 1);
                if (c == "/")
                {
                    c = arg.Substring(1, 1).ToLower();
                }
                else
                    continue;

                switch (c)
                {
                    case "+":
                    case "-":
                        var data = arg.Substring(2).Split(':');
                        var ip = data[0];
                        var port = UInt16.Parse(data[1]);
                        string name = "";
                        if (data.Length == 3)
                        {
                            name = data[2];
                        }

                        if (data.Length < 3)
                            continue;

                        if (c == "-")
                        {
                            RemoveServer(ip, port);
                            continue;
                        }

                        _servers.Add(new ServerData(name, ip, port));
                        continue;

                    case "i":
                        _displayIpPort = true;
                        continue;
                    
                    case "r":
                        _listPlugins = true;
                        continue;

                    case "s":
                        searchArg = true;
                        break;
                    
                    case "?":
                        PrintHelp();
                        _help = true;
                        return;
                }
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("usage:\n" +
                              "ServerQueryCLI.exe /+ /- /name\n" +
                              "/+ add a server /+127.0.0.1:27000:Name\n" +
                              "/- remove server /-127.0.0.1:2700\n" +
                              "/i display ip:port of each server\n" +
                              "/r display all rocketmod plugins\n" +
                              "/s partialPlayerName lists names and servers of a Player's \n" +
                              "   partial name (must have 3 letters)\n" +
                              "/? displays this help" +
                              "\n \n \n");
            return;
        }

        private void RemoveServer(string ip, ushort port)
        {
            foreach (var server in _servers)
            {
                if (server.ip.Equals(ip) && server.port.Equals(port))
                {
                    _servers.Remove(server);
                    return;
                }
            }
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
            if(_displayIpPort)
                Console.WriteLine($"{serverState.Server.ip}:{serverState.Server.port}");
            if (serverState.players.Count == 0 && serverState.State.Equals("Online"))
            {
                Console.WriteLine("*** No Players online ***");
            }

            foreach (var player in serverState.players)
            {
                foreach (var s in _searchResults.Keys)
                {
                    if (player.name.ToLower().Contains(s))
                    {
                        _searchResults[s].Add($"{player.name} in {serverState.ServerName} for {player.time}");
                    }
                }

                Console.WriteLine($"{player.number} - {player.name} - {player.time}");
            }

            if (_listPlugins)
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