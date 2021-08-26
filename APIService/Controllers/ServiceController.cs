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
            var res = _manager.Login(lrm.username, lrm.password);

            if (!res.Success)
                return BadRequest(res.Message);

            // NEW LOGIN CRED
            var user = UserService.Instance.Authenticate(res.Data,
                Encoding.ASCII.GetBytes(_opt.JwtTokenSecret));

            if (user.RefreshToken == null || !user.RefreshToken.IsActive)
            {
                //REFRESH
                var randomBytes = new byte[64];
                using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
                {
                    rngCryptoServiceProvider.GetBytes(randomBytes);
                }

                user.RefreshToken = new RefreshToken
                {
                    rToken = Convert.ToBase64String(randomBytes),
                    Expires = DateTime.UtcNow.AddDays(7),
                    CreatedBy = user.PID
                };
            }
            var x = _manager.InsertOrUpdateRefreshToken(user.RefreshToken);
            if (x.Success)
            {
                // NEW USER CREDENTIALS (ACCESS & REFRESH TOKEN)
                return Ok(user);
            }
            else
            {
                return BadRequest(x.Message);
            }
        }
    }
}