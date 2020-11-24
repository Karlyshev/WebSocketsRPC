using System;

namespace Example
{
    //proxy from clients, I used WebSocketRPC/ws-index.html for tests
    //simple: it's listener on server
    class MyWebSocketService: WebSocketsRPC.WebSocketRPCProxy
    {
        public void Test(string w_string) => Console.WriteLine($"client -> server: method = Test, args = {w_string}");
    }

    //proxy from server
    //simple: it's listener on client
    class MyWSServiceClient : WebSocketsRPC.WebSocketRPCClient 
    {
        public MyWSServiceClient(string connection) : base(connection) { }

        public void Test(string w) 
        {
            Console.WriteLine($"server -> client: method = Test, args = {w}");
            Invoke("Test", w); 
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //server/service
            var service = new WebSocketsRPC.WebSocketRPCService<MyWebSocketService>();
            service.Start(9000, "/Hub");
            //client
            var client = new MyWSServiceClient("ws://127.0.0.1:9000/Hub");
            
            //test:
            Console.WriteLine("Press any key for sending Test message to clients...");
            Console.ReadLine();
            service.Send(WebSocketsRPC.SendToConfigurationType.All, null, null, "Test", new object[] { "ping" }); //server -> clients
            Console.WriteLine("Press any key for stopping the server...");
            Console.ReadLine();
            service.Stop();
        }
    }
}
