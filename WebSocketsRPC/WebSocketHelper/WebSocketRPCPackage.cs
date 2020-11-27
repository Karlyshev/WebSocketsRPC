namespace WebSocketsRPC 
{
    public class WebSocketRPCPackage
    {
        public string Target { get; set; }
        public object[] Arguments { get; set; }
        public short ParametersCount { get; set; } = -1;

        public bool IsInvocation { get; set; }
        public string InvocationID { get; set; }
    }
}