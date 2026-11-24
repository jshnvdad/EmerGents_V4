using EmergencyApp.User.Models;

namespace EmergencyApp.User.Services;

/// While the app is open and an emergency is ongoing: sends the user's live location every 5 s
/// and pulls the latest status / responder info. Pages just listen to <see cref="Updated"/>.
public class LiveTrackingService
{
    private readonly DatabaseService _db;
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly SyncService _sync;
    private readonly LocationService _location;
    private CancellationTokenSource? _cts;

    public Location? LastLocation { get; private set; }
    public event Action<EmergencyRequest>? Updated;

    public LiveTrackingService(DatabaseService db, ApiService api, SessionService session,
                               SyncService sync, LocationService location)
        => (_db, _api, _session, _sync, _location) = (db, api, session, sync, location);

    public void EnsureRunning()
    {
        if (_cts is not null || _session.CurrentUser is null) return;
        var cts = new CancellationTokenSource();
        _cts = cts;
        _ = RunAsync(cts);
    }

    public void Stop() { _cts?.Cancel(); _cts = null; }

    private async Task RunAsync(CancellationTokenSource cts)
    {
        var ct = cts.Token;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var user = _session.CurrentUser;
                if (user is null) break;

                var active = (await _db.GetRequestsAsync(user.Id)).Where(r => r.IsActive).ToList();
                if (active.Count == 0) break;   // nothing ongoing: stop until the next SOS

                var loc = await _location.GetQuietAsync();
                if (loc is not null) LastLocation = loc;

                foreach (var r in active) await TickAsync(r.ClientId, loc);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        finally { if (ReferenceEquals(_cts, cts)) _cts = null; }
    }

    private async Task TickAsync(string clientId, Location? loc)
    {
        var r = await _db.FindByClientIdAsync(clientId);
        if (r is null || !r.IsActive) return;

        if (!r.IsSynced || r.ServerId is null)
        {
            if (!await _sync.PushAsync(r)) { Updated?.Invoke(r); return; }   // still offline
            r = await _db.FindByClientIdAsync(clientId);
            if (r is null) return;
        }

        var res = await _api.TrackAsync(r.ServerId!.Value,
            new TrackRequest(loc?.Latitude, loc?.Longitude, loc?.Accuracy), _session.Token);

        if (res.Success && res.Data is not null)
        {
            r.Apply(res.Data);
            await _db.SaveRequestAsync(r);
        }
        else if (!res.Offline)
        {
            // /track not available on the server yet: fall back to the plain status list.
            await _sync.SyncAllAsync();
            r = await _db.FindByClientIdAsync(clientId) ?? r;
        }
        Updated?.Invoke(r);
    }
}
