using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http.Routing;

namespace Hasseware.SparkleService.AzureStorage
{
    internal sealed class ContainerNameConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName,
            IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            object value;
            if (values.TryGetValue(parameterName, out value) && value is string)
            {
                return IsValidContainerName((string)value);
            }
            return false;
        }

        public static bool IsValidContainerName(string name)
        {
            var regex = new Regex(@"[a-z\d](?:-[a-z\d]|[a-z\d]){2,62}",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            return regex.IsMatch(name);
        }
    }
}