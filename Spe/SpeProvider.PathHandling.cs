using System.Linq;
using System.Management.Automation;

namespace Spe
{
    public partial class SpeProvider
    {
        protected override bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var items = InvokeAndParse($"Test-Path -Path {GetItemPath(path)}");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool)
                {
                    return (bool)items[0].ImmediateBaseObject;
                }
            }

            //log error
            return false;
        }

        protected override bool IsItemContainer(string path)
        {
            return true;
        }

        protected override bool ItemExists(string path)
        {
            //TODO: This seems a bit chatty.

            var items = InvokeAndParse($"Test-Path -Path {GetItemPath(path)}");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool)
                {
                    return (bool)items[0].ImmediateBaseObject;
                }
            }
            
            //log error
            return false;
        }

        protected override bool HasChildItems(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var items = InvokeAndParse($"Get-Item -Path {GetItemPath(path)} | Select-Object -Property HasChildren");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool)
                {
                    return (bool)items[0].ImmediateBaseObject;
                }
            }

            //log error
            return false;
        }

        private string GetItemPath(string path)
        {
            var colonIndex = path.IndexOf(':');
            var relativePath = path.Substring(colonIndex + 1).Replace('\\', '/');
            var databaseName = colonIndex < 0 ? PSDriveInfo.Name : path.Substring(0, colonIndex);

            return $"{databaseName}:{relativePath}";
        }

        private PSObject? GetItemForPath(string path)
        {
            return InvokeAndParse($"Get-Item -Path {GetItemPath(path)}").FirstOrDefault();
        }
    }
}
