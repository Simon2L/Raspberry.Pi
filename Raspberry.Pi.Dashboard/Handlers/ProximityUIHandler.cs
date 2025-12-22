using Raspberry.Pi.Dashboard.Events.Models;
using Raspberry.Pi.Dashboard.Events.Publishers;
using Raspberry.Pi.Dashboard.Services;

namespace Raspberry.Pi.Dashboard.Handlers;

public class ProximityUIHandler(IProximityEventPublisher publisher, IApplicationStateService appState) : IHostedService
{
    private readonly IApplicationStateService _appState = appState;
    private readonly IProximityEventPublisher _proximityEventPublisher = publisher;
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _proximityEventPublisher.ProximityDetected += OnProximityDetected;
        return Task.CompletedTask;
    }

    private void OnProximityDetected(object? sender, ProximityEvent e)
    {
        _appState.UpdateSensorEvent(e);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _proximityEventPublisher.ProximityDetected -= OnProximityDetected;
        return Task.CompletedTask;
    }
}