using ServerApp;

int port = 8080;

Console.WriteLine("------------------------------------");
Console.WriteLine($"Starting chat server on port {port}");
Console.WriteLine("------------------------------------");

try
{
    var server = new Server(port);
    server.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Error starting server: {ex.Message}");
    Console.ReadKey();
}