namespace System.Web.Http.Filters
{
    internal sealed class ExceptionTracingFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var controller = context.ActionContext.ControllerContext.Controller as ApiController;
            if (controller != null) controller.TraceError(context.Exception);
        }
    }
}