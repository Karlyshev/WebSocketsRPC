using System;

namespace Example
{
    public class TestClass 
    {
        public string arg1 { get; set; }
        public bool arg2 { get; set; }
        public int arg3 { get; set; }
    }

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

        public TestClass ComplexText() 
        {
            return new TestClass { arg1 = "test", arg2 = true, arg3 = 0123456789 };
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
            //var result = client.InvokeAsync<bool>("TestB", "1234").GetAwaiter().GetResult();
            //var resultO = client.InvokeAsync<object>("TestO").GetAwaiter().GetResult();
            var resultC = client.InvokeAsync<TestClass>("ComplexText").GetAwaiter().GetResult(); //client -> server -> client (client <=> server)
            Console.WriteLine("Press any key for stopping the server...");
            Console.ReadLine();
            service.Stop();
        }
    }
}
