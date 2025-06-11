using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    public class Server
    {
        private TcpListener _listener;
        private bool _isRunning = false;
        private List<TcpClient> _clients = [];
        private Dictionary<TcpClient, string> _clientUsernames = [];

        public Server(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine("Server started successfully!");
            Console.WriteLine("------------------------------------\n");

            while (_isRunning)
            {
                TcpClient client = _listener.AcceptTcpClient();
                Console.WriteLine("          ***************          ");
                Console.WriteLine("New client attempting to connect...");

                // Open a separate thread for each client
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                // wait for messages from client
                int byteCount = stream.Read(buffer, 0, buffer.Length);
                if (byteCount == 0)
                {
                    client.Close();
                    return;
                }

                // the first message should be the username
                string username = Encoding.UTF8.GetString(buffer, 0, byteCount).Trim();
                TryAddNewClient(client, username, stream);

                while(true)
                {
                    // wait for messages from client
                    byteCount = stream.Read(buffer, 0, buffer.Length);
                    if (byteCount == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    HandleMessage(client, username, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                lock(_clients)
                    _clients.Remove(client);
                lock (_clientUsernames)
                    _clientUsernames.Remove(client);
                client.Close();
                Console.WriteLine("Client disconnecting");
            }

        }

        private void TryAddNewClient(TcpClient client, string username, NetworkStream stream)
        {
            Console.WriteLine($"Try add new client with username {username}");

            lock (_clientUsernames)
            {
                if (_clientUsernames.ContainsValue(username))
                {
                    byte[] response = Encoding.UTF8.GetBytes("FAILED:Username already taken");
                    stream.Write(response, 0, response.Length);
                    Console.WriteLine($"Username '{username}' rejected - already taken");
                    client.Close();
                    return;
                }
                else
                {
                    _clientUsernames[client] = username;
                    lock (_clients)
                        _clients.Add(client);

                    byte[] response = Encoding.UTF8.GetBytes("SUCCESS");
                    stream.Write(response, 0, response.Length);
                    Console.WriteLine($"Username '{username}' accepted - client connected");
                    Console.WriteLine("          ***************          ");
                }
            }
        }

        private void HandleMessage(TcpClient client, string username, string message)
        {
            Console.WriteLine($"\nReceived from {username}: {message}");

            var (isPrivate, targetUser, messageContent) = ParseMessage(message);

            if (isPrivate)
            {
                string privateMessage = $"{DateTime.Now:dd/MM/yyyy hh:mm:ss}, {username} (private) - {messageContent}";
                SendPrivateMessage(privateMessage, targetUser);
            }
            else
            {
                string fullMessage = $"{DateTime.Now:dd/MM/yyyy hh:mm:ss}, {username} - {messageContent}";
                Broadcast(fullMessage, client);
            }
        }

        private (bool isPrivate, string targetUser, string messageContent) ParseMessage(string message)
        {
            if (!message.StartsWith("To:"))
                return (false, string.Empty, message);

            int dashIndex = message.IndexOf(" - ");
            if (dashIndex == -1)
                return (false, string.Empty, message);

            string targetUser = message.Substring(3, dashIndex - 3).Trim();
            string messageContent = message.Substring(dashIndex + 3);

            return (true, targetUser, messageContent);
        }

        private TcpClient GetClientByUsername(string username)
        {
            lock (_clientUsernames)
            {
                foreach (var kvp in _clientUsernames)
                {
                    if (kvp.Value == username)
                        return kvp.Key;
                }
            }
            return null;
        }

        private void SendPrivateMessage(string message, string targetUsername)
        {
            TcpClient targetClient = GetClientByUsername(targetUsername);

            if (targetClient == null)
            {
                Console.WriteLine($"User '{targetUsername}' not found for private message");
                return;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            try
            {
                targetClient.GetStream().Write(buffer, 0, buffer.Length);
                Console.WriteLine($"Private message sent to {targetUsername}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send private message to {targetUsername}: {ex.Message}");
                lock (_clients)
                    _clients.Remove(targetClient);
                lock (_clientUsernames)
                    _clientUsernames.Remove(targetClient);
            }
        }

        private void Broadcast(string message, TcpClient excludeClient)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            lock (_clients)
            {
                foreach (var client in _clients)
                {
                    if (client == excludeClient) continue;
                    try
                    {
                        client.GetStream().Write(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        _clients.Remove(client); //connection with client lost
                        lock (_clientUsernames)
                            _clientUsernames.Remove(client);
                        break;
                    }
                }
                Console.WriteLine($"Message sent to all users");
            }
        }
    }
}
