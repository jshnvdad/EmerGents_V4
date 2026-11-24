using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmergencyApp.User.Models;
using EmergencyApp.User.Services;

namespace EmergencyApp.User.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly SessionService _session;

    public ObservableCollection<EmergencyContact> Contacts { get; } = new();

    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string phone = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string bloodType = "";
    [ObservableProperty] private string allergies = "";
    [ObservableProperty] private string conditions = "";
    [ObservableProperty] private string serverUrl = ApiService.BaseUrl;
    [ObservableProperty] private string emergencyNumber = Preferences.Get("emergency_number", "911");

    private readonly TriggerService _triggers;

    [ObservableProperty] private bool tapMode = AppSettings.TapMode;
    [ObservableProperty] private bool shakeEnabled = AppSettings.ShakeEnabled;
    [ObservableProperty] private bool volumeEnabled = AppSettings.VolumeEnabled;

    partial void OnTapModeChanged(bool value) => AppSettings.TapMode = value;
    partial void OnShakeEnabledChanged(bool value) { AppSettings.ShakeEnabled = value; _triggers.ApplySettings(); }
    partial void OnVolumeEnabledChanged(bool value) => AppSettings.VolumeEnabled = value;

    public ProfileViewModel(DatabaseService db, SessionService session, TriggerService triggers)
        => (_db, _session, _triggers) = (db, session, triggers);

    public async Task LoadAsync()
    {
        var u = _session.CurrentUser;
        if (u is null) return;
        FullName = u.FullName; Phone = u.Phone; Email = u.Email;
        BloodType = u.BloodType; Allergies = u.Allergies; Conditions = u.Conditions;
        ServerUrl = ApiService.BaseUrl;

        Contacts.Clear();
        foreach (var c in await _db.GetContactsAsync(u.Id)) Contacts.Add(c);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var u = _session.CurrentUser;
        if (u is null) return;
        u.FullName = FullName.Trim(); u.Phone = Phone.Trim();
        u.BloodType = BloodType.Trim(); u.Allergies = Allergies.Trim(); u.Conditions = Conditions.Trim();
        await _db.SaveUserAsync(u);

        if (!string.IsNullOrWhiteSpace(ServerUrl)) ApiService.BaseUrl = ServerUrl.Trim();
        if (!string.IsNullOrWhiteSpace(EmergencyNumber)) Preferences.Set("emergency_number", EmergencyNumber.Trim());

        await Shell.Current.DisplayAlertAsync("Saved", "Profile saved on this device.", "OK");
    }

    [RelayCommand]
    private async Task AddContactAsync()
    {
        var u = _session.CurrentUser;
        if (u is null) return;
        var name = await Shell.Current.DisplayPromptAsync("Emergency contact", "Name");
        if (string.IsNullOrWhiteSpace(name)) return;
        var phone = await Shell.Current.DisplayPromptAsync("Emergency contact", "Phone number", keyboard: Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(phone)) return;

        var c = new EmergencyContact { UserId = u.Id, Name = name.Trim(), Phone = phone.Trim() };
        await _db.SaveContactAsync(c);
        Contacts.Add(c);
    }

    [RelayCommand]
    private async Task DeleteContactAsync(EmergencyContact c)
    {
        await _db.DeleteContactAsync(c);
        Contacts.Remove(c);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await Shell.Current.DisplayAlertAsync("Log out", "Log out of this account?", "Log out", "Cancel")) return;
        _session.SignOut();
        await Shell.Current.GoToAsync("//login");
    }
}
