namespace WebSocketManager.Models 
{
    public enum TestStatus: byte 
    {
        NotUse = 0,
        Awaiting = 1,
        OK = 2,
        Error = 3,
    }

    public class TestStatuses 
    {
        public TestStatus SendToAllClientsStatus { get; set; }
        public TestStatus SendToAllClientsExceptStatus { get; set; }
        public TestStatus SendToClientStatus { get; set; }
        public TestStatus SendToClientsStatus { get; set; }
        public TestStatus SendToCallerClientStatus { get; set; }
        public TestStatus SendToOtherClientsStatus { get; set; }
    }
}