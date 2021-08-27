using Backend.DataAccess;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Interface
{
    public interface IProviderManager
    {
        void PopulateUser(ExternalLoginInfo info, ApplicationUser user);
    }
}
