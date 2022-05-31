using System;
using System.Collections.Generic;
using System.Text;

namespace Shell.SPOCPI.MigrationPipeline.WebJob
{
    public static class Constants
    {
        /// <summary>
        /// The migration log category.
        /// </summary>
        public const string LogCategory = "MigrationPipelineWebJob";

        /// <summary>
        /// SDU constant.
        /// </summary>
        public const string SDU = "SDU";

        /// <summary>
        /// Migration constant.
        /// </summary>
        public const string Migration = "Migration";
        
        /// <summary>
        /// The azure management URL.
        /// </summary>
        public const string AzureManagementUrl = "https://management.azure.com/";

        /// <summary>
        /// The storage account connection name.
        /// </summary>
        public const string MigrationStorageConnectionString = "ConnectionStrings:SDUDataStorageConnectionString";

        /// <summary>
        /// Gets or sets the is processed.
        /// </summary>
        /// <value>
        /// The is processed.
        /// </value>
        public const string IsProcessed = "IsProcessed";

        /// <summary>
        /// 
        /// </summary>
        public const string ResourceGroupName = "ResourceGroupName";

        /// <summary>
        /// 
        /// </summary>
        public const string DataFactoryName = "DataFactoryName";

        /// <summary>
        /// 
        /// </summary>
        public const string AzureSubscriptionId = "AzureSubscriptionId";

        /// <summary>
        /// 
        /// </summary>
        public const string MigrationPipelineName = "Migration Pipeline";

        /// <summary>
        /// 
        /// </summary>
        public const string MigrationQueuedPipelinesLimit = "5";
    }
}
