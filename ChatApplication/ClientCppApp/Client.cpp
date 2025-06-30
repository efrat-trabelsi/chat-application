#include <iostream>
#include <string>
#include <thread>
#include <cstring>

#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")

class Client {
private:
    SOCKET _client;
    std::string _username;

public:
    Client() : _client(INVALID_SOCKET) {
        WSADATA wsaData;
        WSAStartup(MAKEWORD(2, 2), &wsaData);
    }

    ~Client() {
        if (_client != INVALID_SOCKET) {
            closesocket(_client);
        }
        WSACleanup();
    }

    void Connect(const std::string& host, int port) 
    {
        try 
        {
            CreateSocket();
            ConnectToServer(host, port);
            GetUsernameFromUser();

            if (_username.empty()) 
            {
                std::cout << "Invalid username. Disconnecting..." << std::endl;
                return;
            }

            // Send username to server for authentication
            std::string response = AuthenticateWithServer();

            if (response == "SUCCESS") 
            {
                StartChatSession();
            }
            else 
            {
                HandleConnectionFailure(response);
            }
        }
        catch (const std::exception& ex) 
        {
            std::cout << "Connection error: " << ex.what() << std::endl;
        }

        if (_client != INVALID_SOCKET) 
        {
            closesocket(_client);
        }
    }

private:
    void CreateSocket() 
    {
        _client = socket(AF_INET, SOCK_STREAM, 0);
        if (_client == INVALID_SOCKET) 
        {
            throw std::runtime_error("Failed to create socket");
        }
    }

    void ConnectToServer(const std::string& host, int port) 
    {
        struct sockaddr_in serverAddr;
        memset(&serverAddr, 0, sizeof(serverAddr));
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_port = htons(port);

        // Try to convert as IP address first
        if (inet_pton(AF_INET, host.c_str(), &serverAddr.sin_addr) <= 0) 
        {
            // If localhost, use 127.0.0.1
            if (host == "localhost") 
            {
                inet_pton(AF_INET, "127.0.0.1", &serverAddr.sin_addr);
            }
            else 
            {
                // For other hostnames, try getaddrinfo (modern replacement for gethostbyname)
                struct addrinfo hints, * result;
                memset(&hints, 0, sizeof(hints));
                hints.ai_family = AF_INET;
                hints.ai_socktype = SOCK_STREAM;

                if (getaddrinfo(host.c_str(), nullptr, &hints, &result) != 0) 
                {
                    throw std::runtime_error("Failed to resolve hostname");
                }

                struct sockaddr_in* addr_in = (struct sockaddr_in*)result->ai_addr;
                serverAddr.sin_addr = addr_in->sin_addr;
                freeaddrinfo(result);
            }
        }

        if (connect(_client, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) 
        {
            throw std::runtime_error("Failed to connect to server");
        }
    }

    void GetUsernameFromUser() 
    {
        std::cout << "Please enter Name:" << std::endl;
        std::getline(std::cin, _username);
    }

    std::string AuthenticateWithServer() 
    {
        // Send username to server
        send(_client, _username.c_str(), _username.length(), 0);

        // Receive response
        char responseBuffer[1024];
        int responseBytes = recv(_client, responseBuffer, sizeof(responseBuffer) - 1, 0);
        if (responseBytes > 0) 
        {
            responseBuffer[responseBytes] = '\0';
            return std::string(responseBuffer);
        }
        return "";
    }

    static void HandleConnectionFailure(const std::string& response) 
    {
        std::cout << "Client Failed to connect to the server" << std::endl;
        if (response.substr(0, 7) == "FAILED:") 
        {
            std::string reason = response.substr(7);
            std::cout << "Reason: " << reason << std::endl;
        }
    }

    void StartChatSession() 
    {
        std::cout << "========================================" << std::endl;
        std::cout << "Client connected successfully!" << std::endl;
        std::cout << "========================================" << std::endl;

        std::thread receiveThread(&Client::ReceiveMessages, this);
        receiveThread.detach();

        SendMessages();
    }

    void SendMessages() 
    {
        try 
        {
            std::string message;
            while (true) 
            {
                std::getline(std::cin, message);
                if (message.empty()) continue;

                send(_client, message.c_str(), message.length(), 0);
            }
        }
        catch (const std::exception& ex) 
        {
            std::cout << "Error sending message: " << ex.what() << std::endl;
        }
    }

    void ReceiveMessages() 
    {
        char buffer[1024];
        try 
        {
            while (true) 
            {
                int byteCount = recv(_client, buffer, sizeof(buffer) - 1, 0);
                if (byteCount <= 0) break;

                buffer[byteCount] = '\0';
                std::cout << buffer << std::endl;
            }
        }
        catch (const std::exception& ex) 
        {
            std::cout << "Connection lost: " << ex.what() << std::endl;
        }
    }
};

int main() 
{
    std::string host = "localhost";
    int port = 8080;

    std::cout << "------------------------------------" << std::endl;
    std::cout << "Connecting to Server " << host << ":" << port << std::endl;
    std::cout << "------------------------------------" << std::endl;

    try 
    {
        Client client;
        client.Connect(host, port);
    }
    catch (const std::exception& ex) 
    {
        std::cout << "Error connecting to server: " << ex.what() << std::endl;
        std::cout << "Make sure the server is running and try again." << std::endl;
        std::cin.get();
    }

    return 0;
}
