using MVVMPattern;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using WebSocketManager.Ext;
using WebSocketManager.Models;
using WebSocketManager.Services;
using WebSocketsRPC;

namespace WebSocketManager.ViewModels 
{
    public class MainVM : NotifyPropertyChanged
    {
        private WebSocketRPCService<WebSocketsRPCProxy> HubSrvc;
        private Visibility _startButtonVisible, _stopButtonVisible;

        public ObservableCollection<SessionProxy> Sessions { get; set; }

        public Visibility StartButtonVisibility
        {
            get => _startButtonVisible;
            private set 
            {
                _startButtonVisible = value;
                _stopButtonVisible = _startButtonVisible == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged("StartButtonVisibility", nameof(StopButtonVisibility), nameof(IsEnabled));
            }
        }
        public Visibility StopButtonVisibility 
        {
            get => _stopButtonVisible;
            private set
            {
                _stopButtonVisible = value;
                _startButtonVisible = _stopButtonVisible == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged(nameof(StartButtonVisibility), "StopButtonVisibility", nameof(IsEnabled));
            }
        }

        public bool IsEnabled => !(_stopButtonVisible == Visibility.Visible && _startButtonVisible == Visibility.Collapsed);

        private int? _port;
        public int? Port 
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged("Port");
            }
        }


        private string _hubPath;
        public string HubPath 
        {
            get => _hubPath;
            set 
            {
                _hubPath = value;
                OnPropertyChanged("HubPath");
            }
        }


        public Command StartButton => new Command(obj =>
        {
            try
            {
                if (Port.HasValue)
                {
                    HubSrvc.Start(Port.Value, HubPath);
                    StartButtonVisibility = Visibility.Collapsed;
                }
            }
            catch 
            {
            }
        }, obj => StopButtonVisibility == Visibility.Collapsed);
        public Command StopButton => new Command(obj =>
        {
            HubSrvc.Stop();
            StartButtonVisibility = Visibility.Visible;
        }, obj => StartButtonVisibility == Visibility.Collapsed);

        public Command SendTo => new Command(obj => {
            if (obj is SendToConfigurationType type) 
                _SendTo(null, type);
        });

        public void _SendTo(SessionProxy sender, SendToConfigurationType type) 
        {
            List<string> ids = type == SendToConfigurationType.ExceptClients || type == SendToConfigurationType.SpecifiedClients ? new List<string>() : null;
            for (int i = 0; i < Sessions.Count; i++)
            {
                Sessions[i].ResetStatuses();
                if (type == SendToConfigurationType.All)
                {
                    Sessions[i].Statuses.SendToAllClientsStatus = TestStatus.Awaiting;
                }
                else if (type == SendToConfigurationType.Caller)
                {
                    sender.Statuses.SendToCallerClientStatus = TestStatus.Awaiting;
                }
                else if (type == SendToConfigurationType.ExceptClients)
                {
                    if (!Sessions[i].send_to_all_client_except_IsExcepted)
                        Sessions[i].Statuses.SendToAllClientsExceptStatus = TestStatus.Awaiting;
                    else
                        ids.Add(Sessions[i].ID);
                }
                else if (type == SendToConfigurationType.Others)
                {
                    if (Sessions[i].ID != sender.ID)
                        Sessions[i].Statuses.SendToOtherClientsStatus = TestStatus.Awaiting;
                }
                else if (type == SendToConfigurationType.SpecifiedClient)
                {
                    sender.Statuses.SendToClientStatus = TestStatus.Awaiting;
                }
                else if (type == SendToConfigurationType.SpecifiedClients)
                {
                    if (Sessions[i].send_to_clients_IsSelected)
                    {
                        Sessions[i].Statuses.SendToClientsStatus = TestStatus.Awaiting;
                        ids.Add(Sessions[i].ID);
                    }
                }
                Sessions[i].UpdateStatuses();
            }
            HubSrvc.Send(type, sender?.Proxy, ids?.ToArray(), "Test", new object[] { type.ToString() });
        }

        public MainVM(SessionCollection sessions, WebSocketRPCService<WebSocketsRPCProxy> hubService) 
        {
            Sessions = sessions.Collection;
            WebSocketsRPCProxy.SetSessionCollection(sessions);
            HubSrvc = hubService;
            StartButtonVisibility = Visibility.Visible;
        }
    }
}