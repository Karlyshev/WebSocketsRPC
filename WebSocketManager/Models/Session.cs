using MVVMPattern;
using WebSocketManager.Services;
using WebSocketManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WebSocketsRPC;

namespace WebSocketManager.Models 
{
    public class SessionProxy: MVVMPattern.NotifyPropertyChanged
    {
        public string ID { get; set; }
        public WebSocketsRPCProxy Proxy { get; set; }
        public TestStatuses Statuses { get; set; }
		
		public bool send_to_all_client_except_IsExcepted { get; set; }
		public bool send_to_clients_IsSelected { get; set; }

        public void UpdateStatuses() 
        {
            OnPropertyChanged(nameof(Statuses));
        }

        public void ResetStatuses() 
        {
            Statuses.SendToAllClientsExceptStatus = TestStatus.NotUse;
            Statuses.SendToAllClientsStatus = TestStatus.NotUse;
            Statuses.SendToCallerClientStatus = TestStatus.NotUse;
            Statuses.SendToClientsStatus = TestStatus.NotUse;
            Statuses.SendToClientStatus = TestStatus.NotUse;
            Statuses.SendToOtherClientsStatus = TestStatus.NotUse;
        }

        public Command SendTo => new Command(obj => {
            if (obj is SendToConfigurationType type)
                App.ServiceProvider.GetService<MainVM>()?._SendTo(this, type);
        });
    }
}