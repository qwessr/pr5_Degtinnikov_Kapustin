using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class Program
    {
        static IPAddress ServerIpAddress;
        static int ServerPort;
        static string ClientToken;
        static DateTime ClientDateConnection;

        static void Main(string[] args)
        {
            OnSettings();

            Thread tCheckToken = new Thread(CheckToken);
            tCheckToken.Start();

            while (true)
            {
                SetCommand();
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
            else if (Command == "/connect") ConnectServer();
            else if (Command == "/register") RegisterUser();
            else if (Command == "/status") GetStatus();
            else if (Command == "/help") Help();
        }

        public static void RegisterUser()
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("Create login: ");
            string login = Console.ReadLine();

            Console.Write("Create password: ");
            string password = Console.ReadLine();

            Console.Write("Confirm password: ");
            string confirmPassword = Console.ReadLine();

            if (password != confirmPassword)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Passwords don't match!");
                return;
            }

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Login and password cannot be empty!");
                return;
            }

            IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(endPoint);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Connection error: {ex.Message}");
                return;
            }

            string msg = $"/register {login} {password}";
            socket.Send(Encoding.UTF8.GetBytes(msg));

            byte[] buffer = new byte[1024];
            int size = socket.Receive(buffer);
            string response = Encoding.UTF8.GetString(buffer, 0, size);

            if (response == "/register_success")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Registration successful! You can now login with /connect");
            }
            else if (response == "/register_exists")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("User already exists!");
            }
            else if (response == "/register_fail")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Registration failed!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unknown response: {response}");
            }

            socket.Close();
        }

        public static void ConnectServer()
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("Login: ");
            string login = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            IPEndPoint endPoint = new IPEndPoint(ServerIpAddress, ServerPort);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Connecting to {ServerIpAddress}:{ServerPort}...");

                socket.Connect(endPoint);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Connected to server!");

                string msg = $"/connect {login} {password}";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                socket.Send(data);

                byte[] buffer = new byte[1024];
                int size = socket.Receive(buffer);
                string response = Encoding.UTF8.GetString(buffer, 0, size);

                if (response == "/auth_fail")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Incorrect login or password");
                    socket.Close();
                    return;
                }

                if (response == "/banned")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Your account is in blacklist!");
                    socket.Close();
                    return;
                }

                if (response == "/limit")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No available licenses.");
                    socket.Close();
                    return;
                }

                ClientToken = response;
                ClientDateConnection = DateTime.Now;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Successfully connected!");
                Console.WriteLine($"Your token: {ClientToken}");
                Console.WriteLine($"Connection time: {ClientDateConnection:HH:mm:ss dd.MM.yyyy}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Connection error: {ex.Message}");
            }
            finally
            {
                if (socket.Connected)
                {
                    socket.Close();
                }
            }
        }

        public static void CheckToken()
        {
            while (true)
            {
                if (!String.IsNullOrEmpty(ClientToken))
                {
                    IPEndPoint EndPoint = new IPEndPoint(ServerIpAddress, ServerPort);
                    Socket Socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp);

                    try
                    {
                        Socket.Connect(EndPoint); ;

                    }
                    catch (Exception exp)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: " + exp.Message);
                    }

                    if (Socket.Connected)
                    {

                        Socket.Send(Encoding.UTF8.GetBytes(ClientToken));

                        byte[] Bytes = new byte[10485760];
                        int ByteRec = Socket.Receive(Bytes);

                        string Response = Encoding.UTF8.GetString(Bytes, 0, ByteRec);
                        if (Response == "/disconnect")
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("The client is disconnected from server");
                            ClientToken = String.Empty;
                        }
                    }
                }

                Thread.Sleep(1000);
            }


        }

        public static void GetStatus()
        {
            if (string.IsNullOrEmpty(ClientToken))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Not connected to server");
                return;
            }

            int Duration = (int)DateTime.Now.Subtract(ClientDateConnection).TotalSeconds;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Client: {ClientToken}, connection time: {ClientDateConnection.ToString("HH:mm:ss dd.MM")}, duration: {Duration}s");
        }

        public static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Available commands:");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - change server settings");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/register");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - register new user");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/connect");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - connect to server");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show connection status");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/help");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - show this help");
        }

        public static void OnSettings()
        {
            string Path = Directory.GetCurrentDirectory() + "/.config";

            if (File.Exists(Path))
            {
                using StreamReader streamReader = new StreamReader(Path);
                string IpAddress = streamReader.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);
                ServerPort = int.Parse(streamReader.ReadLine());
                streamReader.Close();

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(ServerPort.ToString());
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Enter license server IP address: ");
                Console.ForegroundColor = ConsoleColor.Green;
                string IpAddress = Console.ReadLine();
                ServerIpAddress = IPAddress.Parse(IpAddress);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Enter license server port: ");
                Console.ForegroundColor = ConsoleColor.Green;
                ServerPort = int.Parse(Console.ReadLine());

                using StreamWriter streamWriter = new StreamWriter(Path);
                streamWriter.WriteLine(IpAddress);
                streamWriter.WriteLine(ServerPort.ToString());
                streamWriter.Close();
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("To change settings, type: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("/config");
        }
    }
}