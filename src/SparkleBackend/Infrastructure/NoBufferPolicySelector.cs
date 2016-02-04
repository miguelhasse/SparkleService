using System.Web.Http.WebHost;

namespace System.Web.Http.Hosting
{
    internal class NoBufferPolicySelector : WebHostBufferPolicySelector
    {
        public override bool UseBufferedInputStream(object hostContext)
        {
            var contextBase = hostContext as HttpContextBase;
            if (contextBase != null && contextBase.Request.ContentType != null &&
                contextBase.Request.ContentType.Contains("multipart"))
            {
                return false; // we are enabling streamed mode here
            }
            return base.UseBufferedInputStream(hostContext);
        }
    }
}