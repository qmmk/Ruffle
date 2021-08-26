using Backend.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Manager
{
    [Authorize]
    public class HubManager : Hub
    {
        private static readonly Lazy<HubManager> _instance = new Lazy<HubManager>(() => new HubManager());
        public static HubManager Instance { get { return _instance.Value; } }


        #region Defaults

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"{ Context.UserIdentifier} joined.");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"{Context.UserIdentifier} left.");
            await base.OnDisconnectedAsync(exception);
        }
        #endregion

    }
}
