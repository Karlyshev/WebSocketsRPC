namespace WebSocketsRPC
{
    public class WebSocketRPCService<T> where T: WebSocketRPCProxy, new()
    {
        private WebSocketRPCRoute route;

        public WebSocketRPCService() { }

        public void Send(SendToConfigurationType type, T client, string[] ids, string command, object[] args = null)
        {
            if (client == null && (type == SendToConfigurationType.SpecifiedClient || type == SendToConfigurationType.Caller || type == SendToConfigurationType.Others))
                return;
            if (type == SendToConfigurationType.All)
                route.SendToAllClient(command, args);
            else if (type == SendToConfigurationType.ExceptClients)
                route.SendToAllClientsExcept(ids, command, args);
            else if (type == SendToConfigurationType.SpecifiedClient)
                route.SendToClient(client.ID, command, args);
            else if (type == SendToConfigurationType.SpecifiedClients)
                route.SendToClients(ids, command, args);
            else if (type == SendToConfigurationType.Caller)
                client.SendToCallerClient(command, args);
            else if (type == SendToConfigurationType.Others)
                client.SendToOtherClients(command, args);
        }

        public void Start(int port, string hubPath) => route = WebSocketRPCRouter.GetOrAddRoute<T>(port, hubPath);
        public void Stop() 
        {
            if (route != null)
                WebSocketRPCRouter.RemoveRoute(route.Port, route.HubPath);
        }
    }
}
