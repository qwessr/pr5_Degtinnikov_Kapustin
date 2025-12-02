using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Classes;

namespace Server
{
    public class Program
    {
        static IPAddress ServerIpAddress;
        static int ServerPort;
        static int MaxClient;
        static int Duration;
        static List<Classes.Client> AllClients = new List<Classes.Client>();
        static void Main(string[] args)
        {
            OnSettings();

            Thread tListenel = new Thread(ConnectServer);
            tListenel.Start();

            Thread tDisconnect = new Thread(CheckDisconnectClient);
            tDisconnect.Start();
            while (true)
            {
                SetCommand();
            }
        }

        static void CheckDisconnectClient()
        {
            while (true)
            {
                for (int iClient = 0; iClient < AllClients.Count; iClient++)
                {
                    int ClientDuration = (int)DateTime.Now.Subtract(AllClients[iClient].DateConnect).TotalSeconds;

                    if (ClientDuration > Duration)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Client: {AllClients[iClient].Token} disconnect from server to timeout");

                        AllClients.RemoveAt(iClient);
                    }
                }
                Thread.Sleep(1000);
            }
        }
        public static void GetStatus()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Count Clients: {AllClients.Count}");
            foreach (Classes.Client Client in AllClients)
            {
                int Duration = (int)DateTime.Now.Subtract(Client.DateConnect).TotalSeconds;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Client: {Client.Token}, time connection: {Client.DateConnect.ToString("HH:mm:ss dd.MM")}, " +
                    $"duration: {Duration}"
                    );
            }


        }
        public static void SetCommand()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string Command = Console.ReadLine();

            if (Command == "/config")
            {
                File.Delete(Directory.GetCurrentDirectory() + "/.config");
                OnSettings();
            }
            else if (Command.Contains("/disconnect")) DisconnectServer(Command);
            else if (Command == "/status") GetStatus();
            else if (Command == "/help") Help();
            else if (Command == "/ban") Ban(Command);
            else if (Command == "/unblock") Unblock(Command);
        }

        public static void Unblock(string command)
        {
            Console.Write("Login: ");
            string login = Console.ReadLine();
            if (string.IsNullOrEmpty(login))
            {
                Console.WriteLine("Login not specified");
                return;
            }

            using var db = new DbContexted();

            if (!db.Users.Any(x => x.Login == login))
            {
                Console.WriteLine("User not found");
                return;
            }

            var blackListRecord = db.blackLists.FirstOrDefault(x => x.Login == login);

            if (blackListRecord == null)
            {
                Console.WriteLine("User is not banned");
                return;
            }

            int recordId = blackListRecord.Id;

            var recordToDelete = db.blackLists.Find(recordId);
            if (recordToDelete != null)
            {
                db.blackLists.Remove(recordToDelete);
                db.SaveChanges();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"User {login} unbanned");
                Console.ResetColor();
            }
        }

        public static void Ban(string command)
        {

            Console.Write("Login: ");
            string login = Console.ReadLine();
            if (string.IsNullOrEmpty(login))
            {
                Console.WriteLine("Login not specified");
                return;
            }

            using var db = new DbContexted();

            if (!db.Users.Any(x => x.Login == login))
            {
                Console.WriteLine("User does not exist");
                return;
            }

            if (db.blackLists.Any(x => x.Login == login))
            {
                Console.WriteLine("User already banned");
                return;
            }

            db.blackLists.Add(new BlackList { Login = login });
            db.SaveChanges();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"User {login} added to blacklist");
        }
        static void DisconnectServer(string сommand)
        {
            try
            {
                string Token = сommand.Replace("/disconnect", "").Trim();
                Classes.Client DisconnectClient = AllClients.Find(x => x.Token == Token);
                AllClients.Remove(DisconnectClient);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"Client: {Token} disconnect from server");
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + exp.Message);
            }

        }

        public static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Commands to the server: ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - set initial settings ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/disconnect");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - disconnect users from the server ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show list users ");
        }

        public static void OnSettings()
        {
            string Path = Directory.GetCurrentDirectory() + "/.config";
            string IpAddress = "";

            if (File.Exists(Path))
            {
                StreamReader streamReader = new StreamReader(Path);
                IpAddress = streamReader.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);
                ServerPort = int.Parse(streamReader.ReadLine());
                MaxClient = int.Parse(streamReader.ReadLine());
                Duration = int.Parse(streamReader.ReadLine());
                streamReader.Close();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(ServerPort.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Max count clients: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(MaxClient.ToString());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Token lifetime: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Duration.ToString());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please provide the IP address if the license server: ");
                Console.ForegroundColor = ConsoleColor.Green;
                IpAddress = Console.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Please specify the license server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                ServerPort = int.Parse(Console.ReadLine());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Pleace indicate the largest number of clients ");
                Console.ForegroundColor = ConsoleColor.Green;
                MaxClient = int.Parse(Console.ReadLine());

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Specify the token lifetime ");
                Console.ForegroundColor = ConsoleColor.Green;
                Duration = int.Parse(Console.ReadLine());

                StreamWriter streamWriter = new StreamWriter(Path);
                streamWriter.WriteLine(IpAddress);
                streamWriter.WriteLine(ServerPort.ToString());
                streamWriter.WriteLine(MaxClient.ToString());
                streamWriter.WriteLine(Duration.ToString());
                streamWriter.Close();
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("To change, write the command: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/config");
        }

        static string SetCommandClient(string Command)
        {


            if (Command.StartsWith("/connect"))
            {
                string[] parts = Command.Split(' ');
                if (parts.Length != 3) return "/auth_fail";

                string login = parts[1];
                string password = parts[2];

                using var db = new DbContexted();

                if (db.blackLists.Any(x => x.Login == login))
                    return "/banned";

                var user = db.Users.FirstOrDefault(x => x.Login == login && x.Password == password);
                if (user == null)
                    return "/auth_fail";

                if (AllClients.Count < MaxClient)
                {
                    Classes.Client newClient = new Classes.Client();
                    AllClients.Add(newClient);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"New client connection: " + newClient.Token);

                    return newClient.Token;

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"There is not enougt space on the license server");
                    return "/limit";
                }


            }
            else
            {
                Client c = AllClients.Find(x => x.Token == Command);
                return c != null ? "/connect" : "/disconnect";
            }
            return null;
        }
        public static void ConnectServer()
        {
            IPEndPoint EndPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket SocketListener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            SocketListener.Bind(EndPoint);
            SocketListener.Listen(10);
            while (true)
            {
                Socket Handler = SocketListener.Accept();
                byte[] Bytes = new byte[10485760];
                int ByteRec = Handler.Receive(Bytes);

                string Message = Encoding.UTF8.GetString(Bytes, 0, ByteRec);
                string Response = SetCommandClient(Message);

                Handler.Send(Encoding.UTF8.GetBytes(Response));
            }

        }
    }
}
