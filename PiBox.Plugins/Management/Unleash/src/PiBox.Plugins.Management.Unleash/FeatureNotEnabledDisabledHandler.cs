using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.FeatureManagement.Mvc;
using PiBox.Hosting.Abstractions.Exceptions;

namespace PiBox.Plugins.Management.Unleash
{
    [ExcludeFromCodeCoverage(Justification = "not worth the time and resources to test this")]
    public class FeatureNotEnabledDisabledHandler : IDisabledFeaturesHandler
    {
        public Task HandleDisabledFeatures(IEnumerable<string> features, ActionExecutingContext context)
        {
            throw new NotFoundPiBoxException("not found");
        }
    }
}
