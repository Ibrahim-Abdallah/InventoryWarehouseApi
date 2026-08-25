using InventoryWarehouseApi.Api;
using InventoryWarehouseApi.Api.BackgroundJobs;
using InventoryWarehouseApi.Application.LowStock;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InventoryWarehouseApi.IntegrationTests;

public sealed class Phase08WorkerTests
{
    [Fact]
    public async Task DisabledWorker_DoesNotInvokeMonitoringAndStopsCleanly()
    {
        FakeMonitoringService fake = new();
        await using ServiceProvider provider = Services(fake).BuildServiceProvider();
        LowStockMonitoringWorker worker = Worker(provider, new() { Enabled = false, IntervalSeconds = 5 });

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(0, fake.InvocationCount);
    }

    [Fact]
    public async Task EnabledWorker_ExecutesImmediatelyAndCancellationStopsCleanly()
    {
        FakeMonitoringService fake = new();
        TestLogger logger = new();
        await using ServiceProvider provider = Services(fake).BuildServiceProvider();
        LowStockMonitoringWorker worker = Worker(provider, new() { Enabled = true, IntervalSeconds = 5 }, logger);

        await worker.StartAsync(CancellationToken.None);
        await fake.WaitForInvocationsAsync(1, TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(fake.InvocationCount >= 1);
        Assert.DoesNotContain(LogLevel.Error, logger.Levels);
    }

    [Fact]
    public async Task IterationFailure_DoesNotKillWorker()
    {
        FakeMonitoringService fake = new(failFirstInvocation: true);
        await using ServiceProvider provider = Services(fake).BuildServiceProvider();
        LowStockMonitoringWorker worker = Worker(provider, new() { Enabled = true, IntervalSeconds = 5 });

        await worker.StartAsync(CancellationToken.None);
        await fake.WaitForInvocationsAsync(2, TimeSpan.FromSeconds(7));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(fake.InvocationCount >= 2);
        Assert.Equal(1, fake.SuccessCount);
    }

    [Theory]
    [InlineData(true, 5, true)]
    [InlineData(true, 60, true)]
    [InlineData(true, 4, false)]
    [InlineData(true, 86401, false)]
    [InlineData(false, 4, true)]
    public void OptionsValidation_MatchesConfiguredStartupRule(bool enabled, int interval, bool expectedValid)
    {
        Dictionary<string, string?> values = new()
        {
            ["LowStockMonitoring:Enabled"] = enabled.ToString(),
            ["LowStockMonitoring:IntervalSeconds"] = interval.ToString()
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddApiServices();
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<LowStockMonitoringOptions> configured = provider.GetRequiredService<IOptions<LowStockMonitoringOptions>>();
        if (expectedValid)
            Assert.Equal(interval, configured.Value.IntervalSeconds);
        else
            Assert.Throws<OptionsValidationException>(() => configured.Value);
    }

    private static IServiceCollection Services(FakeMonitoringService fake) =>
        new ServiceCollection().AddSingleton<ILowStockMonitoringService>(fake);

    private static LowStockMonitoringWorker Worker(IServiceProvider provider, LowStockMonitoringOptions options, ILogger<LowStockMonitoringWorker>? logger = null) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), Options.Create(options), logger ?? NullLogger<LowStockMonitoringWorker>.Instance);

    private sealed class FakeMonitoringService(bool failFirstInvocation = false) : ILowStockMonitoringService
    {
        private readonly TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int InvocationCount { get; private set; }
        public int SuccessCount { get; private set; }

        public Task<LowStockMonitoringRunResult> RunAsync(DateTimeOffset observedAtUtc, CancellationToken ct)
        {
            InvocationCount++;
            _changed.TrySetResult();
            if (failFirstInvocation && InvocationCount == 1) throw new InvalidOperationException("Controlled test failure.");
            SuccessCount++;
            return Task.FromResult(new LowStockMonitoringRunResult(0, 0, 0, 0, 0));
        }

        public async Task WaitForInvocationsAsync(int expected, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutSource = new(timeout);
            while (InvocationCount < expected)
            {
                await _changed.Task.WaitAsync(timeoutSource.Token);
                if (InvocationCount < expected) await Task.Delay(10, timeoutSource.Token);
            }
        }
    }

    private sealed class TestLogger : ILogger<LowStockMonitoringWorker>
    {
        public List<LogLevel> Levels { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }
}
