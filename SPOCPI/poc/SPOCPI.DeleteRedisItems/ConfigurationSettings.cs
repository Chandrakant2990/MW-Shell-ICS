using System;
using System.Collections.Generic;
using System.Text;

namespace SPOCPI.DeleteRedisItems
{
    class ConfigurationSettings
    {
        public string SearchUpdateKey { get; set; }
        public string DocumentTrackingIndexName { get; set; }
        public string SPOCPISearchServiceName { get; set; }
        public string AppInsightsInstrumentationKey { get; set; }    
        public string RedisConnectionString { get; set; }
        public string PartitionKey { get; set; }
        public string Time { get; set; }
        
    }
}
