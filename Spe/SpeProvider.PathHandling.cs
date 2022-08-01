using System.Management.Automation;

namespace Spe
{
    public partial class SpeProvider
    {
        private bool DoesItemExist(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var items = RemotingHelper.InvokeAndParse($"Test-Path -Path {GetItemPath(path)}");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool itemExists)
                {
                    return itemExists;
                }
            }

            //log error
            return false;
        }

        protected override bool IsValidPath(string path)
        {
            return DoesItemExist(path);
        }

        protected override bool IsItemContainer(string path)
        {
            return true;
        }       

        protected override bool ItemExists(string path)
        {
            return DoesItemExist(path);
        }

        protected override bool HasChildItems(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var items = RemotingHelper.InvokeAndParse($"Get-Item -Path {GetItemPath(path)} | Select-Object -Property HasChildren");
            if (items != null && items.Count > 0)
            {
                if (items[0].ImmediateBaseObject is bool hasChildren)
                {
                    return hasChildren;
                }
            }

            //log error
            return false;
        }

        private string GetItemPath(string path)
        {
            var colonIndex = path.IndexOf(':');
            var relativePath = path[(colonIndex + 1)..].Replace('\\', '/');
            var databaseName = colonIndex < 0 ? PSDriveInfo.Name : path[..colonIndex];

            return $"{databaseName}:{relativePath}";
        }

        private static string GetParentFromPath(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            var lastLeafIndex = path.LastIndexOf('/');
            return path[..lastLeafIndex];
        }

        private static string GetLeafFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            path = path.Replace('\\', '/').TrimEnd('/');
            var lastLeafIndex = path.LastIndexOf('/');
            return path[(lastLeafIndex + 1)..];
        }
    }
}
