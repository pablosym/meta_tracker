using Microsoft.AspNetCore.SignalR;


    public class NotificacionHub : Hub
    {


        public override async Task OnConnectedAsync()
        {

            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, "Notificacion");
            await base.OnConnectedAsync();
        }

        //public async Task SendMessage(NotificacionDTO notificacion)
        //{
        //    await Clients.All.SendAsync("ReceiveNotificacion", notificacion);
        //}


        //public async Task SendMessageGrupo(string grupo, NotificacionDTO notificacion)
        //{
        //   await Clients.Group(grupo).SendAsync("ReceiveNotificacion", notificacion);
        //}

        
    }

