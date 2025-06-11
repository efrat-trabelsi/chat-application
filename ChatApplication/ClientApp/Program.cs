using ClientApp;

string host = "localhost";
int port = 8080;

Console.WriteLine("------------------------------------");
Console.WriteLine($"Connecting to Server {host}:{port}");
Console.WriteLine("------------------------------------");

try
{
    var client = new Client();
    client.Connect(host, port);
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to server: {ex.Message}");
    Console.WriteLine("Make sure the server is running and try again.");
    Console.ReadKey();
}