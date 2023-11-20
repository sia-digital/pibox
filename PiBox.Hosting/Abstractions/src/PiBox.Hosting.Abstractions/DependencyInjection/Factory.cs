using Microsoft.Extensions.DependencyInjection;

namespace PiBox.Hosting.Abstractions.DependencyInjection
{
    public sealed class Factory<TService> : IFactory<TService> where TService : class
    {
        private readonly Func<TService> _serviceProvider;

        public Factory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider.GetService<TService>;
        }

        public TService CreateOrNull() => _serviceProvider();

        public TService Create() =>
            CreateOrNull()
            ?? throw new ArgumentException($"Could not create instance of {typeof(TService).Name} within ServiceFactory");
    }

    public interface IFactory<out TService> where TService : class
    {
        TService CreateOrNull();
        TService Create();
    }
}
