using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using WebSocketManager.Models;
using WebSocketsRPC.Server;

namespace WebSocketManager.Ext 
{
    public class SessionCollection
    {
        private volatile ObservableCollection<SessionProxy> _collection = new ObservableCollection<SessionProxy>();
        private object lockObj = new object();
        private Dispatcher dispatcher;

        public ObservableCollection<SessionProxy> Collection => _collection;
        public void Lock() => Monitor.Enter(lockObj);
        public void Unlock() => Monitor.Exit(lockObj);

        public SessionCollection() 
        {
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Add(SessionProxy session) 
        {
            Lock();
            dispatcher.Invoke(() => _collection.Add(session));
            Unlock();
        }

        public void Remove(SessionProxy session) 
        {
            Lock();
            dispatcher.Invoke(() => _collection.Remove(session));
            Unlock();
        }

        public IWebSocketSession GetSession(string id) 
        {
            //Sessions.TryGetSession(id, out IWebSocketSession session);
            //return session;
            return null;
        }
    }
}