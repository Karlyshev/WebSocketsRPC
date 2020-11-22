using WebSocketsRPC;
using WebSocketsRPC.Server;
using WebSocketManager.Ext;
using System.Net.NetworkInformation;
using System.Linq;
using WebSocketManager.Models;

namespace WebSocketManager.Services 
{
    public class HubService 
    {
        private WebSocketRPCRoute proxy;

        public HubService(SessionCollection sessionCollection) => WebSocketsRPCProxy.SetSessionCollection(sessionCollection);

        public void Send(SendToConfigurationType type, SessionProxy session, string[] ids, string command, object[] args = null) 
        {
            if (proxy == null)
                return;
            if (type == SendToConfigurationType.All)
                proxy.SendToAllClient(command, args);
            else if (type == SendToConfigurationType.ExceptClients)
                proxy.SendToAllClientsExcept(ids, command, args);
            else if (type == SendToConfigurationType.SpecifiedClient)
                proxy.SendToClient(session.ID, command, args);
            else if (type == SendToConfigurationType.SpecifiedClients)
                proxy.SendToClients(ids, command, args);
            else if (type == SendToConfigurationType.Caller)
                session.Proxy.SendToCallerClient(command, args);
            else if (type == SendToConfigurationType.Others)
                session.Proxy.SendToOtherClients(command, args);
        }

        public void Start(int port, string hubPath) => proxy = WebSocketRPCRouter.GetOrAddRoute<WebSocketsRPCProxy>(port, hubPath);

        public void Stop() 
        {
            if (proxy != null)
                WebSocketRPCRouter.RemoveRoute(proxy.Port, proxy.HubPath);
        }
    }
}