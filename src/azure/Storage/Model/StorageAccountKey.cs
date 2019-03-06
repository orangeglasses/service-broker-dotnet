namespace azure.Storage.Model
{
    public class StorageAccountKey
    {
        public string KeyName { get; set; }

        public KeyPermission Permissions { get; set; }

        public string Value { get; set; }
    }
}
