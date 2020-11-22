using System.Collections.Generic;
using System.Linq;
using WebSocketsRPC.Server;

namespace WebSocketsRPC
{
    public static class WebSocketRPCRouter
    {
        private static Dictionary<int, WebSocketServer> _wsServers = new Dictionary<int, WebSocketServer>();
        private static List<WebSocketRPCRoute> routes = new List<WebSocketRPCRoute>();
        private static object _lock = new object();

        public static WebSocketServer GetOrAddServer(int port)
        {
            lock (_lock)
            {
                if (_wsServers.TryGetValue(port, out WebSocketServer wsS))
                    return wsS;
                else
                {
                    var new_wsS = new WebSocketServer(port); //--> действует на все виды ip; new WebSocketServer("ws://<ip>:<port>"); --> как укажешь, так и будет
                    _wsServers.Add(port, new_wsS);
                    return new_wsS;
                }
            }
        }

        public static WebSocketRPCRoute GetOrAddRoute<T>(int port, string hubPath) where T : WebSocketRPCProxy, new()
        {
            lock (_lock)
            {
                var server = GetOrAddServer(port);
                var route = port.ToString() + hubPath;
                for (int i = 0; i < routes.Count; i++)
                {
                    if (routes[i].Route == route)
                        return routes[i];
                }
                var hub = server.AddWebSocketService<T>(hubPath);
                var ws_route = new WebSocketRPCRoute(port, hubPath, hub.Sessions);
                server.Start();
                routes.Add(ws_route);
                return ws_route;
            }
        }

        public static void RemoveRoute(int port, string hubPath)
        {
            lock (_lock)
            {
                var server = GetOrAddServer(port);
                var route = port.ToString() + hubPath;
                for (int i = 0; i < routes.Count; i++)
                {
                    if (routes[i].Route == route)
                    {
                        server.WebSocketServices.RemoveService(hubPath);
                        routes[i].Stop();
                        routes.RemoveAt(i);
                        if (!server.WebSocketServices.Hosts.Any())
                        {
                            server.Stop();
                            _wsServers.Remove(port);
                        }
                        return;
                    }
                }
            }
        }
    }
}