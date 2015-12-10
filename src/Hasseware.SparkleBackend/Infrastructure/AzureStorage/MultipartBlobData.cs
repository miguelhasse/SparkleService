using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Hasseware.SparkleService.AzureStorage
{
    public class MultipartBlobData : IDisposable
    {
        private HashAlgorithm hashAlgorithm;

        internal MultipartBlobData(HttpContentHeaders headers, CloudBlockBlob cloudBlob)
        {
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            if (cloudBlob == null)
            {
                throw new ArgumentNullException("cloudBlob");
            }
            this.Headers = headers;
            this.BlobReference = cloudBlob;
        }

        public HttpContentHeaders Headers
        {
            get;
            private set;
        }

        public ICloudBlob BlobReference
        {
            get;
            private set;
        }

        public byte[] Hash
        {
            get { return hashAlgorithm != null ? hashAlgorithm.Hash : null;  }
        }

        internal async Task<System.IO.Stream> GetWriteStreamAsync()
        {
            var blobStream = await ((CloudBlockBlob)this.BlobReference).OpenWriteAsync();

            if (Headers.ContentType.MediaType.Contains("application/octet-stream"))
            {
                hashAlgorithm = new SHA1CryptoServiceProvider();
                return new CryptoStream(blobStream, hashAlgorithm, CryptoStreamMode.Write);
            }
            return blobStream;
        }

		internal async Task SetPropertiesFromHeadersAsync()
		{
			this.BlobReference.Properties.ContentType = this.Headers.ContentType.MediaType;
            this.BlobReference.Properties.ContentDisposition = this.Headers.ContentDisposition.FileName;
            this.BlobReference.Properties.ContentEncoding = (this.Headers.ContentEncoding.Count > 0) ?
                this.Headers.ContentEncoding.ToString() : null;
            this.BlobReference.Properties.ContentLanguage = (this.Headers.ContentLanguage.Count > 0) ?
                this.Headers.ContentLanguage.ToString() : null;

            await this.BlobReference.SetPropertiesAsync().ConfigureAwait(false);
        }
		
        public void Dispose()
        {
            if (hashAlgorithm != null)
                ((IDisposable)hashAlgorithm).Dispose();
        }
    }
}
