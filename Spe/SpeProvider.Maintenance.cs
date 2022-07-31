using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Spe
{
    public partial class SpeProvider
    {
        private ProviderInfo providerInfo;
        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            providerInfo.Description = "Spe Provider";
            this.providerInfo = providerInfo;
            return providerInfo;
        }

        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            //TODO: Query list of drives
            var drives = new Collection<PSDriveInfo>();

            var items = InvokeAndParse("Get-Database | Where-Object { $_.Name -ne 'filesystem' }");

            foreach (var item in items)
            {
                var dbName = item.Properties["Name"].Value.ToString();
                var drive = new PSDriveInfo(dbName, providerInfo, $"{dbName}:",
                        $"Sitecore '{dbName}' database.", PSCredential.Empty);
                drives.Add(drive);
            }

            return drives;
        }
    }
}
