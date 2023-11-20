using System.Net;

namespace PiBox.Plugins.Endpoints.RestResourceEntity.Endpoints
{
    public record RestSimpleResourceEndpointConfiguration
    {
        internal readonly string ResourceName;
        internal readonly IList<string> OpenApiTags = new List<string>();
        internal readonly IList<string> Policies = new List<string>();
        internal readonly IDictionary<int, Type> ProducesMap = new Dictionary<int, Type>();
        internal bool AllowAnonymousEnabled;
        internal string GroupName;
        internal string DisplayName;
        internal bool DefaultResponsesEnabled = true;

        public RestSimpleResourceEndpointConfiguration(string resourceName)
        {
            ResourceName = resourceName;
        }

        public RestSimpleResourceEndpointConfiguration WithOpenApiTags(params string[] openApiTags)
        {
            foreach (var tag in openApiTags)
            {
                OpenApiTags.Add(tag);
            }
            return this;
        }

        public RestSimpleResourceEndpointConfiguration WithPolicies(params string[] policies)
        {
            foreach (var policy in policies)
            {
                Policies.Add(policy);
            }
            return this;
        }

        public RestSimpleResourceEndpointConfiguration Produces<T>(HttpStatusCode statusCode)
        {
            ProducesMap.Add((int)statusCode, typeof(T));
            return this;
        }

        public RestSimpleResourceEndpointConfiguration DisableDefaultResponses()
        {
            DefaultResponsesEnabled = false;
            return this;
        }

        public RestSimpleResourceEndpointConfiguration WithDisplayName(string displayName)
        {
            DisplayName = displayName;
            return this;
        }

        public RestSimpleResourceEndpointConfiguration WithGroupName(string groupName)
        {
            GroupName = groupName;
            return this;
        }

        public RestSimpleResourceEndpointConfiguration AllowAnonymous()
        {
            AllowAnonymousEnabled = true;
            return this;
        }
    }
}
