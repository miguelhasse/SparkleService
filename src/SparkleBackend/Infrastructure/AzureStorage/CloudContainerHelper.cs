using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.Threading.Tasks;

namespace Hasseware.SparkleService.AzureStorage
{
    internal static class CloudContainerHelper
	{
        public static async Task<CloudBlobContainer> GetRootContainer()
        {
            // Retrieve a reference to a container 
            // Container name must use lower case
            CloudBlobContainer container = CreateBlobClient().GetRootContainerReference();

            // Create the container if it doesn't already exist
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            // Enable public access to blob
            var permissions = await container.GetPermissionsAsync().ConfigureAwait(false);
            if (permissions.PublicAccess != BlobContainerPublicAccessType.Off)
            {
                permissions.PublicAccess = BlobContainerPublicAccessType.Off;
                await container.SetPermissionsAsync(permissions).ConfigureAwait(false);
            }
            return container;
        }

        public static async Task<CloudBlobContainer> GetSharedContainer()
		{
            // Retrieve a reference to a container 
            // Container name must use lower case
            CloudBlobContainer container = CreateBlobClient().GetContainerReference("sparkleshare");

			// Create the container if it doesn't already exist
			await container.CreateIfNotExistsAsync().ConfigureAwait(false);

			// Enable public access to blob
			var permissions = await container.GetPermissionsAsync().ConfigureAwait(false);
			if (permissions.PublicAccess != BlobContainerPublicAccessType.Blob)
			{
				permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
				await container.SetPermissionsAsync(permissions).ConfigureAwait(false);
			}
			return container;
		}

        private static CloudBlobClient CreateBlobClient()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["CloudStorage"].ConnectionString;

            // Retrieve storage account from connection-string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            // Create the blob client 
            return storageAccount.CreateCloudBlobClient();
        }
    }
}