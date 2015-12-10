using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;

namespace System.Web.Http.Filters
{
    internal sealed class ValidateMimeMultipartContentAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (!actionContext.Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        }
    }
}