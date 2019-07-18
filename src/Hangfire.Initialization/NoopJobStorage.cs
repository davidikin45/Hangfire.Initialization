using System;
using System.Collections.Generic;
using System.Text;
using Hangfire.Storage;

namespace Hangfire.Initialization
{
    class NoopJobStorage : JobStorage
    {
        public override IStorageConnection GetConnection()
        {
            throw new NotImplementedException();
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            throw new NotImplementedException();
        }
    }
}
