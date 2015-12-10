using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Hasseware.SparkleService.AzureStorage
{
    public class MultipartBlobStreamProvider : MultipartStreamProvider
    {
        private CloudBlobDirectory directory;
        private Collection<bool> formDataMarkers;
        private readonly Func<ContentDispositionHeaderValue, string> blobNameResolver;

        public MultipartBlobStreamProvider(CloudBlobDirectory directory,
            Func<ContentDispositionHeaderValue, string> blobNameResolver = null)
        {
            this.BlobData = new Collection<MultipartBlobData>();
            this.FormData = new NameValueCollection();

            this.directory = directory;
            this.blobNameResolver = blobNameResolver;
            this.formDataMarkers = new Collection<bool>();
        }

        public ICollection<MultipartBlobData> BlobData { get; private set; }

        public NameValueCollection FormData { get; private set; }

        public override async Task ExecutePostProcessingAsync(CancellationToken cancellationToken)
        {
            var contents = base.Contents.Where((content, index) => this.formDataMarkers[index])
                .Select(async content =>
                {
                    string name = content.Headers.ContentDisposition.Name.Trim('"');
                    this.FormData.Add(name, await content.ReadAsStringAsync());
                });
            foreach (var formDataTask in contents)
            {
                if (formDataTask.IsFaulted)
                    throw formDataTask.Exception.InnerException;

                cancellationToken.ThrowIfCancellationRequested();
            }
            foreach (var blobData in BlobData)
            {
                await blobData.SetPropertiesFromHeadersAsync();
            }
            this.formDataMarkers = null;
            await base.ExecutePostProcessingAsync(cancellationToken);
        }

		public virtual string GetBlobFileName(HttpContentHeaders headers)
		{
			if (headers == null)
			{
				throw new ArgumentNullException("headers");
			}
			return (blobNameResolver != null) ? blobNameResolver(headers.ContentDisposition) :
                Path.GetFileName(headers.ContentDisposition.FileName.Trim('"'));
		}
		
        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (headers == null)
            {
                throw new ArgumentNullException("headers");
            }
            ContentDispositionHeaderValue contentDisposition = headers.ContentDisposition;
            if (contentDisposition == null)
            {
                throw new InvalidOperationException(String.Format(
                    Properties.Resources.MultipartBlobStreamProviderNoContentDisposition,
                    new object[] { "Content-Disposition" }));
            }
            if (string.IsNullOrEmpty(contentDisposition.FileName))
            {
                formDataMarkers.Add(true);
                return new MemoryStream();
            }

            string blobName = GetBlobFileName(headers);
            var blobReference = directory.GetBlockBlobReference(blobName.ToSlug());

            var multipartFileDatum = new MultipartBlobData(headers, blobReference);
            this.BlobData.Add(multipartFileDatum);

            formDataMarkers.Add(false);
            return multipartFileDatum.GetWriteStreamAsync().Result;
        }
    }
}
