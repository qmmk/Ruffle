using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Interface
{
    public interface IEmailManager
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
