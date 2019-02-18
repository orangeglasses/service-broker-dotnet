using System.Runtime.Serialization;

namespace azure.Storage.Model
{
    public enum StorageEncryptionKeySource
    {
        [EnumMember(Value = "Microsoft.Keyvault")]
        KeyVault,

        [EnumMember(Value = "Microsoft.Storage")]
        Storage
    }
}
