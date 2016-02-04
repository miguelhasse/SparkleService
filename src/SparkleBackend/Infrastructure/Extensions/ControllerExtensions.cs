using System.Globalization;
using System.Web.Http.Tracing;

namespace System.Web.Http
{
    internal static class ControllerExtensions
    {
        public static void TraceError(this ApiController @this, Exception exception)
        {
            TraceInternal(@this, TraceLevel.Error, traceRecord => traceRecord.Exception = exception);
        }

        public static void TraceInformation(this ApiController @this, string messageFormat, params object[] messageArguments)
        {
            TraceInternal(@this, TraceLevel.Info, traceRecord =>
            {
                traceRecord.Message = String.Format(CultureInfo.InvariantCulture, messageFormat, messageArguments);
            });
        }

        public static void TraceWarning(this ApiController @this, string messageFormat, params object[] messageArguments)
        {
            TraceInternal(@this, TraceLevel.Warn, traceRecord =>
            {
                traceRecord.Message = String.Format(CultureInfo.InvariantCulture, messageFormat, messageArguments);
            });
        }

        private static void TraceInternal(ApiController controller, TraceLevel level, Action<TraceRecord> traceAction)
        {
            var writer = controller.Configuration.Services.GetTraceWriter();

            if (writer != null)
            {
                string category = controller.ControllerContext.ControllerDescriptor.ControllerName;
                writer.Trace(controller.Request, category, level, traceAction);
            }
        }
    }
}