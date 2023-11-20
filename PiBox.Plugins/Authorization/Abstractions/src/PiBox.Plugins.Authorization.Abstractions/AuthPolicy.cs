namespace PiBox.Plugins.Authorization.Abstractions
{
    public sealed class AuthPolicy
    {
        /// <summary>
        /// The name of the policy
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The roles which the user must have
        /// </summary>
        public IList<string> Roles { get; set; }
    }
}
