using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Backend.DataAccess
{
    
    public partial class DataContext : DbContext
    {
        #region Properties
        private readonly AppSettings _settings;
        public DataContext(DbContextOptions<DataContext> opt, IOptions<AppSettings> settings) : base(opt)
        {
            _settings = settings.Value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_settings.ConnectionString);
            }
        }

        public virtual DbSet<User> Users { get; set; }

        #endregion
    }
}