using System.Windows.Data;
using WebSocketManager.Ext;
using WebSocketManager.Models;
using WebSocketsRPC;

namespace WebSocketManager.Services
{
    public class WebSocketsRPCProxy : WebSocketRPCProxy
    {
        private SessionProxy session = null;
        private static SessionCollection sessions;

        public static void SetSessionCollection(SessionCollection sessionCollection) 
        {
            if (sessions == null)
                sessions = sessionCollection;
        }

        public void Test(string w_status) 
        {
            if (w_status == "All")
                session.Statuses.SendToAllClientsStatus = session.Statuses.SendToAllClientsStatus == TestStatus.Awaiting ? TestStatus.OK : TestStatus.Error;
            if (w_status == "ExceptClients")
                session.Statuses.SendToAllClientsExceptStatus = session.Statuses.SendToAllClientsExceptStatus == TestStatus.Awaiting ? TestStatus.OK : TestStatus.Error;
            if (w_status == "SpecifiedClient")
                session.Statuses.SendToClientStatus = session.Statuses.SendToClientStatus == TestStatus.Awaiting ? TestStatus.OK : TestStatus.Error;
            if (w_status == "SpecifiedClients")
                session.Statuses.SendToClientsStatus = session.Statuses.SendToClientsStatus == TestStatus.Awaiting ? TestStatus.OK : TestStatus.Error;
            if (w_status == "Caller")
                session.Statuses.SendToCallerClientStatus = session.Statuses.SendToCallerClientStatus == TestStatus.Awaiting ? TestStatus.OK : TestStatus.Error;
            if (w_status == "Others")
                session.Statuses.SendToOtherClientsStatus = session.Statuses.SendToOtherClientsStatus == TestStatus.Awaiting ? TestStatus.OK : TestStatus.Error;
            session.UpdateStatuses();
        }

        protected override void OnOpen() 
        {
            base.OnOpen();
            session = new SessionProxy() { ID = ID, Proxy = this, Statuses = new TestStatuses() };
            sessions?.Add(session);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            sessions?.Remove(session);
        }
    }
}
