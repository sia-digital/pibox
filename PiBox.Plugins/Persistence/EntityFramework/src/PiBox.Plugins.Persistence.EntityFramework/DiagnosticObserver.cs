using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace PiBox.Plugins.Persistence.EntityFramework
{
    [ExcludeFromCodeCoverage] //can't check if observer where subscribed
    public class DiagnosticObserver : IObserver<DiagnosticListener>
    {
        public DiagnosticObserver()
        {

        }

        public void OnCompleted()
        {
            // do nothing
        }

        public void OnError(Exception error)
        {
            // do nothing
        }

        public void OnNext(DiagnosticListener value)
        {
            if (value.Name == DbLoggerCategory.Name) // "Microsoft.EntityFrameworkCore"
            {
                value.Subscribe(new MetricsObserver()!);
            }
        }
    }
}
