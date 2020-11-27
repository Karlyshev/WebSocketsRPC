using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace WebSocketsRPC
{
    public class WebSocketRPCClient
    {
        ConcurrentDictionary<string, WebSocketRPCInvocation> callbacks;
        #region Base
        protected object InvokeMethod(string message)
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
                return method.Invoke(this, methodArgs);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion Base

        private WebSocket _ws;
        public WebSocketRPCClient(string connection) 
        {
            callbacks = new ConcurrentDictionary<string, WebSocketRPCInvocation>();
            _ws = new WebSocket(connection);
            _ws.OnMessage += (sender, e) =>
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
            };
            _ws.OnOpen += (sender, e) => Console.WriteLine("client opened");
            _ws.OnClose += (sender, e) => Console.WriteLine($"client closed: [ reason = {e.Reason}, code = {e.Code} ]");
            _ws.OnError += (sender, e) => Console.WriteLine($"error: {e.Message}");
            _ws.Connect();
        }

        public void Invoke(string method, params object[] args) => _ws.Send(JsonConvert.SerializeObject(new WebSocketRPCPackage() { Target = method, Arguments = args ?? new object[0] }));
        public async Task<object> InvokeAsync<TResult>(string method, params object[] args) => await Task.Run(() =>
        {
            var dateTime = DateTime.Now;
            var waiter = new AutoResetEvent(false);
            var id = $"{method}#{dateTime.Day}.{dateTime.Month}.{dateTime.Year}#{dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}.{dateTime.Millisecond}";
            _ws.Send(JsonConvert.SerializeObject(new WebSocketRPCPackage() { Target = method, Arguments = args ?? new object[0], IsInvocation = true, InvocationID = id }));
            var invocationPackage = new WebSocketRPCInvocation() { InvocationID = id };
            callbacks.TryAdd(id, invocationPackage);
            waiter.WaitOne();
            callbacks.TryRemove(id, out invocationPackage);
            return invocationPackage.Result != null ? (TResult)invocationPackage.Result : new object();
        });
    }

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
                SendToCallerClient(id, new WebSocketRPCInvocation() { InvocationID = id, Error = error, Result = result });
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
                    throw new NullReferenceException("method");
                }
                var methodParameters = foundMethod.GetParameters();
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