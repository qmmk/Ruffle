using Backend.DataAccess;
using Backend.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Manager
{
    public sealed class ServiceManager : IServiceManager, IDisposable
    {
        #region Fields
        private readonly DataContext _context;

        public ServiceManager(DataContext context)
        {
            _context = context;
        }

        ~ServiceManager()
        {
            // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(false);

        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                if (_context != null)
                    _context.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
        }
    }
}