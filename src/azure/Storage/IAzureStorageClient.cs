﻿using System.Threading;
using System.Threading.Tasks;
using azure.Storage.Model;

namespace azure.Storage
{
    public interface IAzureStorageClient
    {
        Task<StorageAccount> CreateStorageAccount(
            string resourceGroupName, StorageAccount storageAccount, string apiVersion = AzureStorageClient.DefaultApiVersion,
            CancellationToken ct = default);
    }
}