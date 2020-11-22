/*using System;
using System.Linq;
using WebSocketsRPC.Server;
using Newtonsoft.Json;

namespace WebSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            var wssv = WebSocketsRPC.WebSocketHub.CreateHubServer(9000, "/Hub", out MyHub myHub);
            Console.ReadLine();
            //------------------------------------------------------------------------------------------------------------------
            //for callbacks:
            myHub?.SendToAllClient("TEST", "1", "2", "5", "8");
            /*wssv.WebSocketServices.TryGetServiceHost("/Hub", out var www);
            var sessions = www.Sessions.Sessions.ToList();
            for (int i = 0; i < sessions.Count; i++) 
            {
                sessions[i].Context.WebSocket.SendAsync("{ \"res\": \"OK\" }", ar => { });
            }*
            //----------------
            Console.ReadLine();
            wssv.Stop();
        }
    }
}
*/