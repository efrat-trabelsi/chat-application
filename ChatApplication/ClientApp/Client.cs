using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    public class Client
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private string? _username;

        public void Connect(string host, int port)
        {
            try
            {
                _client = new TcpClient(host, port);
                _stream = _client.GetStream();

                Console.WriteLine("Please enter Name:");
                _username = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(_username))
                {
                    Console.WriteLine("Invalid username. Disconnecting...");
                    return;
                }

                // Send username to server for authentication
                string response = AuthenticateWithServer();

                if (response == "SUCCESS")
                    StartChatSession();
                else
                    HandleConnectionFailure(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: { ex.Message}");
            }
            finally
            {
                _client?.Close();
            }
        }

        private string AuthenticateWithServer()
        {
            byte[] usernameBuffer = Encoding.UTF8.GetBytes(_username);
            _stream.Write(usernameBuffer, 0, usernameBuffer.Length);

            byte[] responseBuffer = new byte[1024];
            int responseBytes = _stream.Read(responseBuffer, 0, responseBuffer.Length);
            string response = Encoding.UTF8.GetString(responseBuffer, 0, responseBytes);
            return response;
        }

        private static void HandleConnectionFailure(string response)
        {
            Console.WriteLine("Client Failed to connect to the server");
            if (response.StartsWith("FAILED:"))
            {
                string reason = response.Substring(7);
                Console.WriteLine($"Reason: {reason}");
            }
        }

        private void StartChatSession()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Client connected successfully!");
            Console.WriteLine("========================================");

            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();

            SendMessages();
        }

        private void SendMessages()
        {
            try
            {
                while (true)
                {
                    string message = Console.ReadLine();
                    if (string.IsNullOrEmpty(message)) continue;

                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    _stream.Write(buffer, 0, buffer.Length);
                    _stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int byteCount = _stream.Read(buffer, 0, buffer.Length);
                    if (byteCount == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Console.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection lost: {ex.Message}");
            }
        }
    }
}
