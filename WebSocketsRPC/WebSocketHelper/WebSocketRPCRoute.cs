using Newtonsoft.Json;
using System.Linq;
using WebSocketsRPC.Server;

namespace WebSocketsRPC
{
    public class WebSocketRPCRoute
    {
        private volatile WebSocketSessionManager _sessions;
        public string Route { get; private set; }

        public int Port { get; private set; }

        public string HubPath { get; private set; }

        public WebSocketRPCRoute(int port, string hubPath, WebSocketSessionManager wssm)
        {
            Port = port;
            HubPath = hubPath;
            var route = port.ToString() + hubPath;
            Route = route;
            _sessions = wssm;
        }

        public void Stop() => _sessions = null;

        #region Base
        protected void SendToClients(SendToConfigurationType type, string[] additionalData, string method, object[] args)
        {
            if (_sessions == null)
                return;
            string message = JsonConvert.SerializeObject(new WebSocketRPCPackage() { Target = method, Arguments = args ?? new object[0] });
            if (type == SendToConfigurationType.All)
            {
                var sessions = _sessions.Sessions.ToList();
                for (int i = 0; i < sessions.Count; i++)
                    sessions[i].Context.WebSocket.SendAsync(message, ar => { });
            }
            else if (type == SendToConfigurationType.ExceptClients)
            {
                if (additionalData == null)
                    additionalData = new string[] { };
                var sessions = _sessions.Sessions.ToList();
                for (int i = 0; i < sessions.Count; i++)
                    if (!additionalData.Contains(sessions[i].ID))
                        sessions[i].Context.WebSocket.SendAsync(message, ar => { });
            }
            else if (type == SendToConfigurationType.SpecifiedClient)
            {
                if (additionalData != null && additionalData.Length == 1)
                    if (_sessions.TryGetSession(additionalData[0], out var session))
                        session.Context.WebSocket.SendAsync(message, ar => { });
            }
            else if (type == SendToConfigurationType.SpecifiedClients)
            {
                if (additionalData != null && additionalData.Length > 0)
                    for (int i = 0; i < additionalData.Length; i++)
                        if (_sessions.TryGetSession(additionalData[i], out var session))
                            session.Context.WebSocket.SendAsync(message, ar => { });
            }
        }
        #endregion Base
        #region Extensions
        public void SendToAllClient(string method, params object[] args) => SendToClients(SendToConfigurationType.All, null, method, args);

        public void SendToAllClientsExcept(string[] clientIds, string method, params object[] args) => SendToClients(SendToConfigurationType.ExceptClients, clientIds, method, args);

        public void SendToClient(string connectionId, string method, params object[] args) => SendToClients(SendToConfigurationType.SpecifiedClient, new string[] { connectionId }, method, args);

        public void SendToClients(string[] clientIds, string method, params object[] args) => SendToClients(SendToConfigurationType.SpecifiedClients, clientIds, method, args);
        #endregion Extensions
    }
}