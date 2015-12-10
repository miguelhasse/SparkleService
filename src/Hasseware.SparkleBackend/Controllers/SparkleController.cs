using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace Hasseware.SparkleService.Controllers
{
    [ExceptionTracingFilter]
    [RoutePrefix("api/sparkle")]
    public class SparkleController : ApiController
    {
        [Route("appcasts")]
        public async Task<HttpResponseMessage> GetAppCasts()
        {
            var rootContainer = await AzureStorage.CloudContainerHelper.GetRootContainer();

            var blobs = rootContainer.ListBlobs(useFlatBlobListing: true,
                blobListingDetails: Microsoft.WindowsAzure.Storage.Blob.BlobListingDetails.Metadata)
                .Where(s => s is Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob)
                .Cast<Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob>();

            var names = blobs.Select(s => s.Name).ToList();
            return this.Request.CreateResponse(HttpStatusCode.OK, names);
        }

        [Route("appcasts/{id:container}")]
        public async Task<HttpResponseMessage> GetAppCastFeed(string id)
        {
            if (Request.Headers.Accept.Count(s => s.MediaType.EndsWith("xml")) == 0)
            {
                return this.Request.CreateResponse(HttpStatusCode.UnsupportedMediaType,
                    "Accepted reponse media types unsupported.");
            }
            var rootContainer = await AzureStorage.CloudContainerHelper.GetRootContainer();
            var blob = rootContainer.GetBlockBlobReference(id);

            if (await blob.ExistsAsync().ConfigureAwait(false) == false)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound,
                    String.Format("AppCast named {0} not found.", id));
            }
            Models.AppCastFeed appcast = new Models.AppCastFeed();
            using (var stream = await blob.OpenReadAsync().ConfigureAwait(false))
            {
                appcast.Load(stream);
                appcast.Link = Request.RequestUri.AbsoluteUri;

                var sharedContainer = await AzureStorage.CloudContainerHelper.GetSharedContainer();

                foreach (var appcastitem in appcast)
                {
                    var enclosure = appcastitem.Enclosure;
                    if (enclosure != null && enclosure.ContentLink != null) enclosure.ContentLink = 
						String.Join("/", sharedContainer.Uri, enclosure.ContentLink);
                    if (appcastitem.NotesLink != null) appcastitem.NotesLink =
						String.Join("/", sharedContainer.Uri, appcastitem.NotesLink);
                }
            }
            Action<Stream, HttpContent, TransportContext> onStreamAvailable = (stream, content, context) =>
            {
                appcast.Save(stream);
                stream.Close();
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new PushStreamContent(onStreamAvailable, blob.Properties.ContentType);
            return response;
        }

        [HttpPost, Route("appcasts")]
        public async Task<HttpResponseMessage> CreateAppCast(Models.AppCastFeed appcast)
        {
            if (!this.ModelState.IsValid)
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            var rootContainer = await AzureStorage.CloudContainerHelper.GetRootContainer();

            string id = appcast.Title.ToSlug();
            var blob = rootContainer.GetBlockBlobReference(id);

            if (await blob.ExistsAsync().ConfigureAwait(false))
            {
                return this.Request.CreateResponse(HttpStatusCode.Ambiguous,
                    String.Format("AppCast named {0} already exists.", appcast.Title));
            }
            using (DSACryptoServiceProvider prv = new DSACryptoServiceProvider())
            {
                byte[] b = System.Text.Encoding.UTF8.GetBytes(prv.ToXmlString(true));
                blob.Metadata["PrivateKey"] = Convert.ToBase64String(b);
                blob.Properties.ContentType = "application/xml";

                using (var stream = await blob.OpenWriteAsync().ConfigureAwait(false))
                    appcast.Save(stream);
            }
            var response = this.Request.CreateResponse(HttpStatusCode.Created, id);
            response.Headers.ETag = new EntityTagHeaderValue(blob.Properties.ETag);
            return response;
        }

        [ValidateMimeMultipartContent]
        [HttpPost, Route("appcasts/{id:container}")]
        public async Task<HttpResponseMessage> CreateAppCastItem(string id)
        {
            var rootContainer = await AzureStorage.CloudContainerHelper.GetRootContainer();
            var blob = rootContainer.GetBlockBlobReference(id);

            if (await blob.ExistsAsync().ConfigureAwait(false) == false)
            {
                return this.Request.CreateResponse(HttpStatusCode.NotFound,
                    String.Format("AppCast named {0} not found.", id));
            }
            using (DSACryptoServiceProvider prv = new DSACryptoServiceProvider())
            {
                string key;
                if (blob.Metadata.TryGetValue("PrivateKey", out key))
                {
                    byte[] b = Convert.FromBase64String(key);
                    prv.FromXmlString(System.Text.Encoding.UTF8.GetString(b));
                }
                Models.AppCastFeed appcast = new Models.AppCastFeed();
                using (var stream = await blob.OpenReadAsync().ConfigureAwait(false))
                    appcast.Load(stream);

                var sharedContainer = await AzureStorage.CloudContainerHelper.GetSharedContainer();
                var directory = sharedContainer.GetDirectoryReference(id);

                string requestUID = Guid.NewGuid().ToString("N");
                var streamProvider = new AzureStorage.MultipartBlobStreamProvider(directory,
                    v => String.Join("-", v.Name.Trim('"'), requestUID));

                await Request.Content.ReadAsMultipartAsync(streamProvider);

                var appcastItem = new Models.AppCastItem
                {
                    Title = streamProvider.FormData["title"],
                    Enclosure = new Models.AppCastEnclosure
                    {
                        Version = Version.Parse(streamProvider.FormData["version"])
                    }
                };
                foreach (var blobData in streamProvider.BlobData)
                {
                    if (blobData.Hash != null)
                    {
                        await blobData.BlobReference.FetchAttributesAsync().ConfigureAwait(false);
                        blobData.BlobReference.Metadata["DigestSHA1"] = Convert.ToBase64String(blobData.Hash);
                        byte[] signatureHash = prv.SignHash(blobData.Hash, null);

                        //TODO: Apply validations here! (and check leases)

                        appcastItem.Published = blobData.BlobReference.Properties.LastModified.Value.DateTime;
                        var enclosure = appcastItem.Enclosure;

                        enclosure.ContentLink = blobData.BlobReference.Name;
                        enclosure.ContentLength = blobData.BlobReference.Properties.Length;
                        enclosure.ContentType = blobData.BlobReference.Properties.ContentType;
                        enclosure.Signature = Convert.ToBase64String(signatureHash);
                    }
                    else
                    {
                        appcastItem.NotesLink = blobData.BlobReference.Name;
                    }
                    string filename = blobData.Headers.ContentDisposition.FileName.Trim('"');
                    blobData.BlobReference.Metadata["FileName"] = Path.GetFileName(filename);
                    await blobData.BlobReference.SetMetadataAsync().ConfigureAwait(false);
                }
                appcast.Add(appcastItem);
                using (var stream = await blob.OpenWriteAsync().ConfigureAwait(false))
                    appcast.Save(stream);
            }
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
