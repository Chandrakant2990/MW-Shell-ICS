﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Shell.SPOCPI.RefreshDeltaToken.WebJob {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Shell.SPOCPI.RefreshDeltaToken.WebJob.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DrivesDelta or DrivesDeltaTransaction Table is not available..
        /// </summary>
        internal static string DriveDeltaTableIsNullMessage {
            get {
                return ResourceManager.GetString("DriveDeltaTableIsNullMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No DriveId found. Token refresh cannot be peformed for subscriptionId: {0} .
        /// </summary>
        internal static string DriveIdNotFoundMessage {
            get {
                return ResourceManager.GetString("DriveIdNotFoundMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initialized Function successfully.
        /// </summary>
        internal static string FunctionsInitializationSuccess {
            get {
                return ResourceManager.GetString("FunctionsInitializationSuccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Maximum retry attempts {0}, has been attempted..
        /// </summary>
        internal static string MaximumRetryAttemptWarning {
            get {
                return ResourceManager.GetString("MaximumRetryAttemptWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ProcessJob Completed at:.
        /// </summary>
        internal static string ProcessRefreshCompleted {
            get {
                return ResourceManager.GetString("ProcessRefreshCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ProcessJob Triggered at:.
        /// </summary>
        internal static string ProcessRefreshTrigerred {
            get {
                return ResourceManager.GetString("ProcessRefreshTrigerred", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to RefreshDeltaTokenCustomMinuteSchedule.
        /// </summary>
        internal static string RefreshDeltaTokenCustomMinuteSchedule {
            get {
                return ResourceManager.GetString("RefreshDeltaTokenCustomMinuteSchedule", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Token refresh not required for DriveId: {0} and SubscriptionId: {1}.
        /// </summary>
        internal static string RefreshTokenNotRequiredMessage {
            get {
                return ResourceManager.GetString("RefreshTokenNotRequiredMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No DeltaUrl available.
        /// </summary>
        internal static string UpdateDeltaTokenNoDeltaUrl {
            get {
                return ResourceManager.GetString("UpdateDeltaTokenNoDeltaUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Started updating deltaToken in Drive delta storage table..
        /// </summary>
        internal static string UpdateDeltaTokenStart {
            get {
                return ResourceManager.GetString("UpdateDeltaTokenStart", resourceCulture);
            }
        }
    }
}