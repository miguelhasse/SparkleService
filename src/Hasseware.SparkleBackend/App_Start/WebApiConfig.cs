using System.Web.Http;
using System.Web.Http.Routing;
using System.Web.Http.Tracing;

namespace Hasseware.SparkleService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            var traceWriter = config.EnableSystemDiagnosticsTracing();
            traceWriter.MinimumLevel = TraceLevel.Debug;

            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("container", typeof(AzureStorage.ContainerNameConstraint));

            // Web API routes
            config.MapHttpAttributeRoutes(constraintResolver);
        }
    }
}
