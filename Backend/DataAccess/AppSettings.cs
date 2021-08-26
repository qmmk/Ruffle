using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.DataAccess
{
    public class AppSettings
    {
        public string JwtTokenSecret { get; set; }
        public string ConnectionString { get; set; }
    }
}
