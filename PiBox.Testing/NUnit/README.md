# PiBox.Testing.Nunit

[![PiBox framework](https://img.shields.io/badge/powered_by-PiBox-%23000?style=flat-square)](https://github.com/sia-digital/pibox/tree/main#readme)


## How to unit test metric collection

```csharp
using var metricsCollector = new TestMetricsCollector("my-metric-name");
metricsCollector.CollectedMetrics.Should().BeEmpty();

// do test stuff e.g. call method, etc..

metricsCollector.Instruments.Should().Contain("my-metric-name");
metricsCollector.CollectedMetrics.Should().ContainsMetric(1);
```
