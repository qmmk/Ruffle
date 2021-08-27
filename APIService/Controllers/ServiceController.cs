using Backend.DataAccess;
using Backend.Interface;
using Backend.Manager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ruffle.Controllers
{
    [ApiController, Authorize]
    [Route("[controller]/[action]")]
    public class ServiceController : ControllerBase
    {
        #region Fields

        private readonly IServiceManager _manager;
        private readonly AppSettings _opt;
        private readonly IHubContext<HubManager> _hub;

        #endregion

        public ServiceController(IServiceManager manager,
            IOptions<AppSettings> opt,
            IHubContext<HubManager> hub)
        {
            _manager = manager;
            _opt = opt.Value;
            _hub = hub;
        }

        #region Login
        public class LoginRequestModel
        {
            public string username { get; set; }
            public string password { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login([FromBody] LoginRequestModel lrm)
        {
            // FIRST CHECK
            var res = _manager.GetUsers();
            return Ok(res);

        }

        #endregion
    }
}