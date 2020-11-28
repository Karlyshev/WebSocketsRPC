using System;
using System.Threading;

namespace WebSocketsRPC 
{
    public class WebSocketRPCInvocation 
    {
        public string InvocationID { get; set; }
        public object Result { get; set; }

        public string Error { get; set; }   

        public AutoResetEvent waiter { get; set; }
        public Type ResultType { get; set; }
    }
}