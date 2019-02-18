using System.Diagnostics.CodeAnalysis;

namespace azure.Storage.Model
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum StorageSkuName
    {
        Premium_LRS,
        Premium_ZRS,
        Standard_GRS,
        Standard_LRS,
        Standard_RAGRS,
        Standard_ZRS
    }
}