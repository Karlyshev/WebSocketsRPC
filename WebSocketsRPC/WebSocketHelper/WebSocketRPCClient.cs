using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

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
                var isInvocation = json["IsInvocation"].ToObject<bool>();
                var invocationID = json["InvocationID"].ToString();
                if (isInvocation)
                {
                    if (callbacks.TryGetValue(invocationID, out WebSocketRPCInvocation wsRPCinvocation))
                    {
                        JArray jsonArgsInvocationEvent = (JArray)json["Arguments"];
                        wsRPCinvocation.Result = jsonArgsInvocationEvent[0].ToObject(wsRPCinvocation.ResultType);
                        wsRPCinvocation.Error = jsonArgsInvocationEvent[1].ToString();
                        wsRPCinvocation.waiter.Set();
                    }
                    return null;
                }
                var methodName = json["Target"].ToString();
                JArray jsonArgs = (JArray)json["Arguments"];
                var type = GetType();
                var method = type.GetMethod(methodName);
                if (method == null)
                    throw new NullReferenceException("method");
                var methodParameters = method.GetParameters();
                var methodArgs = new object[methodParameters.Length];
                Helpers.SetMethodsArgs(ref methodArgs, jsonArgs, methodParameters);
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

        public void Invoke(string method, short parametersCount, params object[] args) => _ws.Send(JsonConvert.SerializeObject(new WebSocketRPCPackage() { Target = method, ParametersCount = parametersCount, Arguments = args ?? new object[0] }));

        public async Task<TResult> InvokeAsync<TResult>(string method, params object[] args) => (TResult)await Task.Run(() =>
        {
            var dateTime = DateTime.Now;
            var waiter = new AutoResetEvent(false);
            var id = $"{method}#{dateTime.Day}.{dateTime.Month}.{dateTime.Year}#{dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}.{dateTime.Millisecond}";
            var invocationPackage = new WebSocketRPCInvocation() { InvocationID = id, waiter = waiter, ResultType = typeof(TResult) };
            callbacks.TryAdd(id, invocationPackage);
            _ws.Send(JsonConvert.SerializeObject(new WebSocketRPCPackage() { Target = method, Arguments = args ?? new object[0], IsInvocation = true, InvocationID = id }));
            waiter.WaitOne();
            callbacks.TryRemove(id, out invocationPackage);
            return invocationPackage.Error != null && invocationPackage.Error != "" ? throw new Exception(invocationPackage.Error) : invocationPackage.Result;
        });

        public async Task<TResult> InvokeAsync<TResult>(string method, short parametersCount, params object[] args) => (TResult)await Task.Run(() =>
        {
            var dateTime = DateTime.Now;
            var waiter = new AutoResetEvent(false);
            var id = $"{method}#{dateTime.Day}.{dateTime.Month}.{dateTime.Year}#{dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}.{dateTime.Millisecond}";
            var invocationPackage = new WebSocketRPCInvocation() { InvocationID = id, waiter = waiter, ResultType = typeof(TResult) };
            callbacks.TryAdd(id, invocationPackage);
            _ws.Send(JsonConvert.SerializeObject(new WebSocketRPCPackage() { Target = method, ParametersCount = parametersCount, Arguments = args ?? new object[0], IsInvocation = true, InvocationID = id }));
            waiter.WaitOne();
            callbacks.TryRemove(id, out invocationPackage);
            return invocationPackage.Error != null && invocationPackage.Error != "" ? throw new Exception(invocationPackage.Error) : invocationPackage.Result;
        });
    }
}