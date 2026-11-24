using EmergencyApp.User.Models;

namespace EmergencyApp.User.Services;

public class SyncService
{
    private readonly DatabaseService _db;
    private readonly ApiService _api;
    private readonly SessionService _session;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _pushGate = new(1, 1);

    public SyncService(DatabaseService db, ApiService api, SessionService session)
        => (_db, _api, _session) = (db, api, session);

    /// Uploads one request. Safe to call from several places at once (never double-sends).
    public async Task<bool> PushAsync(EmergencyRequest r)
    {
        await _pushGate.WaitAsync();
        try
        {
            var fresh = await _db.FindByClientIdAsync(r.ClientId) ?? r;
            if (fresh.IsSynced && fresh.ServerId is not null) return true;

            var dto = new CreateEmergencyDto(fresh.ClientId, fresh.UserId, fresh.Type, fresh.Description,
                                             fresh.Latitude, fresh.Longitude, fresh.CreatedAtUtc,
                                             fresh.PeopleCount, fresh.Source);
            var res = await _api.CreateEmergencyAsync(dto, _session.Token);
            if (!res.Success || res.Data is null) return false;

            fresh.Apply(new LiveEmergencyDto
            {
                ServerId = res.Data.ServerId,
                ClientId = res.Data.ClientId,
                Status = res.Data.Status,
                ResponderName = res.Data.ResponderName
            });
            await _db.SaveRequestAsync(fresh);
            return true;
        }
        finally { _pushGate.Release(); }
    }

    /// Upload anything still pending, then pull the latest status of all requests.
    public async Task SyncAllAsync()
    {
        if (_session.CurrentUser is null || !await _gate.WaitAsync(0)) return;
        try
        {
            foreach (var r in (await _db.GetUnsyncedAsync(_session.CurrentUser.Id)).Where(x => x.IsActive))
                await PushAsync(r);

            var mine = await _api.GetMyEmergenciesAsync(_session.Token);
            if (!mine.Success || mine.Data is null) return;

            foreach (var dto in mine.Data)
            {
                var local = await _db.FindByClientIdAsync(dto.ClientId);
                if (local is null) continue;
                local.Apply(dto);
                await _db.SaveRequestAsync(local);
            }
        }
        finally { _gate.Release(); }
    }
}
