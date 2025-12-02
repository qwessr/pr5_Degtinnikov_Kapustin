using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Server.Classes;

namespace Server
{
    public class Program
    {
        static IPAddress ServerIpAddress;
        static int ServerPort;
        static int MaxClients;
        static int TokenDuration;
        static List<Client> ActiveClients = new List<Client>();

        static void Main(string[] args)
        {
            CreateDatabase();
            OnSettings();

            Thread listenerThread = new Thread(ListenForClients);
            listenerThread.Start();

            Thread disconnectThread = new Thread(CheckDisconnectedClients);
            disconnectThread.Start();

            while (true)
            {
                SetCommand();
            }
        }

        static void CreateDatabase()
        {
            using var db = new DbContexted();
            db.Database.EnsureCreated();
        }

        static void CheckDisconnectedClients()
        {
            while (true)
            {
                lock (ActiveClients)
                {
                    for (int i = ActiveClients.Count - 1; i >= 0; i--)
                    {
                        int clientDuration = (int)DateTime.Now.Subtract(ActiveClients[i].DateConnect).TotalSeconds;

                        if (clientDuration > TokenDuration)
                        {
                            Console.WriteLine($"Client {ActiveClients[i].Token} disconnected (timeout)");
                            ActiveClients.RemoveAt(i);
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public static void GetStatus()
        {
            Console.WriteLine($"\nActive clients: {ActiveClients.Count}/{MaxClients}");

            if (ActiveClients.Count > 0)
            {
                Console.WriteLine("Connected clients:");
                foreach (Client client in ActiveClients)
                {
                    int duration = (int)DateTime.Now.Subtract(client.DateConnect).TotalSeconds;
                    Console.WriteLine($"  Token: {client.Token}, Login: {client.Login}, Duration: {duration}s");
                }
            }
        }

        public static void SetCommand()
        {
            Console.Write("Server> ");
            string Command = Console.ReadLine();

            if (Command == "/config")
            {
                File.Delete(Directory.GetCurrentDirectory() + "/.config");
                OnSettings();
            }
            else if (Command.StartsWith("/disconnect")) DisconnectClient(Command);
            else if (Command == "/status") GetStatus();
            else if (Command == "/help") Help();
            else if (Command == "/ban") BanUser();
            else if (Command == "/unban") UnbanUser();
            else if (Command == "/users") ShowAllUsers();
        }

        static void ShowAllUsers()
        {
            using var db = new DbContexted();
            var users = db.Users.ToList();

            Console.WriteLine("\nRegistered users:");
            foreach (var user in users)
            {
                Console.WriteLine($"  ID: {user.Id}, Login: {user.Login}");
            }
        }

        public static void UnbanUser()
        {
            Console.Write("Enter login to unban: ");
            string login = Console.ReadLine();

            using var db = new DbContexted();
            var blackListRecord = db.BlackLists.FirstOrDefault(x => x.Login == login);

            if (blackListRecord != null)
            {
                db.BlackLists.Remove(blackListRecord);
                db.SaveChanges();
                Console.WriteLine($"User {login} has been unbanned");
            }
            else
            {
                Console.WriteLine("User is not banned");
            }
        }

        public static void BanUser()
        {
            Console.Write("Enter login to ban: ");
            string login = Console.ReadLine();

            using var db = new DbContexted();

            if (!db.Users.Any(x => x.Login == login))
            {
                Console.WriteLine("User does not exist");
                return;
            }

            if (!db.BlackLists.Any(x => x.Login == login))
            {
                db.BlackLists.Add(new BlackList { Login = login });
                db.SaveChanges();

                lock (ActiveClients)
                {
                    var activeClient = ActiveClients.FirstOrDefault(c => c.Login == login);
                    if (activeClient != null)
                    {
                        ActiveClients.Remove(activeClient);
                        Console.WriteLine($"Active session for {login} terminated");
                    }
                }

                Console.WriteLine($"User {login} added to blacklist");
            }
            else
            {
                Console.WriteLine("User is already banned");
            }
        }

        static void DisconnectClient(string command)
        {
            string token = command.Replace("/disconnect", "").Trim();

            lock (ActiveClients)
            {
                Client disconnectClient = ActiveClients.FirstOrDefault(x => x.Token == token);

                if (disconnectClient != null)
                {
                    ActiveClients.Remove(disconnectClient);
                    Console.WriteLine($"Client {token} disconnected");
                }
            }
        }

        public static void Help()
        {
            Console.WriteLine("\nServer commands:");
            Console.WriteLine("  /config - change server settings");
            Console.WriteLine("  /disconnect [token] - disconnect client");
            Console.WriteLine("  /status - show server status");
            Console.WriteLine("  /ban - add user to blacklist");
            Console.WriteLine("  /unban - remove user from blacklist");
            Console.WriteLine("  /users - show all users");
            Console.WriteLine("  /help - show this help\n");
        }

        public static void OnSettings()
        {
            string configPath = Directory.GetCurrentDirectory() + "/.config";

            if (File.Exists(configPath))
            {
                using StreamReader reader = new StreamReader(configPath);
                ServerIpAddress = IPAddress.Parse(reader.ReadLine());
                ServerPort = int.Parse(reader.ReadLine());
                MaxClients = int.Parse(reader.ReadLine());
                TokenDuration = int.Parse(reader.ReadLine());
                reader.Close();

                Console.WriteLine($"Loaded settings: {ServerIpAddress}:{ServerPort}, Max clients: {MaxClients}, Token lifetime: {TokenDuration}s");
            }
            else
            {
                Console.Write("Server IP [127.0.0.1]: ");
                string ip = Console.ReadLine();
                ServerIpAddress = string.IsNullOrEmpty(ip) ? IPAddress.Parse("127.0.0.1") : IPAddress.Parse(ip);

                Console.Write("Server port [5000]: ");
                string port = Console.ReadLine();
                ServerPort = string.IsNullOrEmpty(port) ? 5000 : int.Parse(port);

                Console.Write("Max clients [10]: ");
                string max = Console.ReadLine();
                MaxClients = string.IsNullOrEmpty(max) ? 10 : int.Parse(max);

                Console.Write("Token lifetime (seconds) [3600]: ");
                string duration = Console.ReadLine();
                TokenDuration = string.IsNullOrEmpty(duration) ? 3600 : int.Parse(duration);

                using StreamWriter writer = new StreamWriter(configPath);
                writer.WriteLine(ServerIpAddress);
                writer.WriteLine(ServerPort);
                writer.WriteLine(MaxClients);
                writer.WriteLine(TokenDuration);
                writer.Close();

                Console.WriteLine("Settings saved");
            }
        }

        static string ProcessClientCommand(string command)
        {
            if (command.StartsWith("/connect"))
            {
                string[] parts = command.Split(' ');
                if (parts.Length != 3) return "/auth_fail";

                string login = parts[1];
                string password = parts[2];

                using var db = new DbContexted();

                if (db.BlackLists.Any(x => x.Login == login))
                    return "/banned";

                var user = db.Users.FirstOrDefault(x => x.Login == login && x.Password == password);
                if (user == null)
                    return "/auth_fail";

                lock (ActiveClients)
                {
                    if (ActiveClients.Count >= MaxClients)
                    {
                        Console.WriteLine("Server is full");
                        return "/limit";
                    }

                    Client newClient = new Client(login);
                    ActiveClients.Add(newClient);
                    Console.WriteLine($"New client connected: {newClient.Token} ({login})");
                    return newClient.Token;
                }
            }
            else if (command.StartsWith("/register"))
            {
                string[] parts = command.Split(' ');
                if (parts.Length != 3) return "/register_fail";

                string login = parts[1];
                string password = parts[2];

                using var db = new DbContexted();

                if (db.Users.Any(x => x.Login == login))
                    return "/register_exists";

                db.Users.Add(new Users { Login = login, Password = password });
                db.SaveChanges();

                Console.WriteLine($"New user registered: {login}");
                return "/register_success";
            }
            else
            {
                lock (ActiveClients)
                {
                    Client client = ActiveClients.FirstOrDefault(x => x.Token == command);
                    return client != null ? "/connected" : "/disconnect";
                }
            }
        }

        public static void ListenForClients()
        {
            IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(endPoint);
            listener.Listen(10);

            Console.WriteLine($"Server listening on {ServerIpAddress}:{ServerPort}");

            while (true)
            {
                Socket handler = listener.Accept();

                Thread clientThread = new Thread(() =>
                {
                    try
                    {
                        byte[] buffer = new byte[1024];
                        int bytesReceived = handler.Receive(buffer);
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                        string response = ProcessClientCommand(message);
                        handler.Send(Encoding.UTF8.GetBytes(response));
                    }
                    finally
                    {
                        handler.Close();
                    }
                });
                clientThread.Start();
            }
        }
    }
}