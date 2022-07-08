using System.Reflection;

namespace PartnerLed.Utility
{
    internal class Constants
    {
        public static readonly string InputFolderPath = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent.FullName + "/GDAPBulkMigration/operations";

        public static readonly string OutputFolderPath = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent.FullName + "/GDAPBulkMigration/downloads";
        
        public static readonly string LogFolderPath = Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent.FullName + "/Logs";

        public const string BasepathVariable = "BasepathForOperations";

    }
}
