using System;
using WebSocketsRPC.Server;
using Newtonsoft.Json.Linq;

namespace WebSocketsRPC
{
    public class WebSocketRPCProxy : WebSocketBehavior
    {

        private WebSocketRPCRoute route;

        public WebSocketRPCProxy() : base() { }

        protected override void OnOpen() => route = WebSocketRPCRouter.GetOrAddRoute<WebSocketRPCProxy>(Context.RequestUri.Port, Context.RequestUri.LocalPath);

        #region Base
        protected void InvokeMethod(string message)
        {
            try
            {
                JObject json = JObject.Parse(message);
                var methodName = json["Target"].ToString();
                JArray jsonArgs = (JArray)json["Arguments"];
                var type = GetType();
                var method = type.GetMethod(methodName);
                if (method == null)
                    throw new NullReferenceException("method");
                var methodParameters = method.GetParameters();
                var methodArgs = new object[methodParameters.Length];
                for (int i = 0; i < methodArgs.Length; i++)
                    try
                    {
                        methodArgs[i] = i < jsonArgs.Count ? jsonArgs[i].ToObject(methodParameters[i].ParameterType) : (methodParameters[i].HasDefaultValue ? methodParameters[i].DefaultValue : null);
                    }
                    catch
                    {
                        try
                        {
                            methodArgs[i] = i < jsonArgs.Count ? JObject.Parse(jsonArgs[i].ToString()).ToObject(methodParameters[i].ParameterType) : (methodParameters[i].HasDefaultValue ? methodParameters[i].DefaultValue : null);
                        }
                        catch
                        {
                            methodArgs[i] = methodParameters[i].HasDefaultValue ? methodParameters[i].DefaultValue : null;
                        }
                    }
                method.Invoke(this, methodArgs);
            }
            catch (Exception ex)
            {
            }
        }
        #endregion Base

        #region Extensions
        public void SendToAllClient(string method, params object[] args) => route.SendToAllClient(method, args);

        public void SendToCallerClient(string method, params object[] args) => route.SendToClient(ID, method, args); //Native support

        public void SendToOtherClients(string method, params object[] args) => route.SendToAllClientsExcept(new string[] { ID }, method, args); //Native support

        public void SendToAllClientsExcept(string[] clientIds, string method, params object[] args) => route.SendToAllClientsExcept(clientIds, method, args);

        public void SendToClient(string connectionId, string method, params object[] args) => route.SendToClient(connectionId, method, args);

        public void SendToClients(string[] clientIds, string method, params object[] args) => route.SendToClients(clientIds, method, args);
        #endregion Extensions

        protected override void OnMessage(MessageEventArgs e) => InvokeMethod(e.Data);
    }
}