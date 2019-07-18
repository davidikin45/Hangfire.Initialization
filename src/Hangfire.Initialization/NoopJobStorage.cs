using Hangfire.Storage;
using System;

namespace Hangfire.Initialization
{
    public class NoopJobStorage : JobStorage
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
