using System;

namespace WebSocketsRPC 
{
    public class WebSocketRPCInvocation 
    {
        public string InvocationID { get; set; }
        public object Result { get; set; }

        public string Error { get; set; }   
    }
}