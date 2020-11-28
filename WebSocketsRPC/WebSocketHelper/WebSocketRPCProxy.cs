using System;
using Newtonsoft.Json.Linq;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Linq;
using System.Reflection;

namespace WebSocketsRPC
{
    public class WebSocketRPCProxy : WebSocketBehavior
    {
        private WebSocketRPCRoute route;

        public WebSocketRPCProxy() : base()
        {
        }

        protected override void OnOpen() => route = WebSocketRPCRouter.GetOrAddRoute<WebSocketRPCProxy>(Context.RequestUri.Port, Context.RequestUri.LocalPath);

        #region Base
        protected void InvocationEvent(bool isInvocation, string id, out bool isLocked, object result, string error) 
        {
            isLocked = false;
            if (isInvocation && id != null)
            {
                InvocationEventCallback(new WebSocketRPCInvocation() { InvocationID = id, Error = error, Result = result });
                isLocked = true;
            }
        }
        
        protected object InvokeMethod(string message)
        {
            bool isInvocation = false;
            string invocationID = null;
            bool isInvocationLocked = false;
            try
            {
                JObject json = JObject.Parse(message);
                isInvocation = json["IsInvocation"].ToObject<bool>();
                invocationID = json["InvocationID"].ToString();
                var methodName = json["Target"].ToString();
                if (methodName == null || methodName.Length == 0) 
                {
                    InvocationEvent(isInvocation, invocationID, out isInvocationLocked, null, "NullReferenceException: methodName");
                    throw new NullReferenceException("methodName");
                }
                JArray jsonArgs = (JArray)json["Arguments"];
                MethodInfo foundMethod;
                var type = GetType();
                if (json.ContainsKey("ParametersCount"))
                {
                    short parametersCount = json["ParametersCount"].ToObject<short>();
                    foundMethod = type.GetMethods().Where(method => method.IsPublic && method.Name == methodName && method.GetParameters().Length == (parametersCount > 0 ? parametersCount : jsonArgs.Count)).SingleOrDefault();
                }
                else
                {
                    try
                    {
                        foundMethod = type.GetMethod(methodName);
                    }
                    catch
                    {
                        foundMethod = type.GetMethods().Where(method => method.IsPublic && method.Name == methodName && method.GetParameters().Length == jsonArgs.Count).SingleOrDefault();
                    }
                }
                if (foundMethod == null)
                {
                    InvocationEvent(isInvocation, invocationID, out isInvocationLocked, null, "Exception: method wasn't found");
                    throw new NullReferenceException("foundMethod");
                }
                var methodParameters = foundMethod.GetParameters();
                var methodArgs = new object[methodParameters.Length];
                Helpers.SetMethodsArgs(ref methodArgs, jsonArgs, methodParameters);
                var result = foundMethod.Invoke(this, methodArgs);
                InvocationEvent(isInvocation, invocationID, out isInvocationLocked, result, null);
                return result;
            }
            catch (Exception ex)
            {
                InvocationEvent(isInvocation, invocationID, out isInvocationLocked, null, "CatchException: " + ex.Message);
                return null;
            }
        }
        #endregion Base

        #region Extensions
        public void SendToAllClient(string method, params object[] args) => route.SendToAllClient(method, args);

        public void SendToCallerClient(string method, params object[] args) => route.SendToClient(ID, method, args); //Native support

        public void InvocationEventCallback(WebSocketRPCInvocation wsRPCinvocation_package) => route.InvocationEventReceive(ID, wsRPCinvocation_package);

        public void SendToOtherClients(string method, params object[] args) => route.SendToAllClientsExcept(new string[] { ID }, method, args); //Native support

        public void SendToAllClientsExcept(string[] clientIds, string method, params object[] args) => route.SendToAllClientsExcept(clientIds, method, args);

        public void SendToClient(string connectionId, string method, params object[] args) => route.SendToClient(connectionId, method, args);

        public void SendToClients(string[] clientIds, string method, params object[] args) => route.SendToClients(clientIds, method, args);
        #endregion Extensions

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsText)
                InvokeMethod(e.Data);
            else if (e.IsPing)
            {
                //NotImplementedException
            }
            else if (e.IsBinary)
            {
                //e.RawData, NotImplementedException
            }
        }
    }
}