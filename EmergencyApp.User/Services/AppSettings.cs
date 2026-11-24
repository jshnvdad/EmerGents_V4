namespace EmergencyApp.User.Services;

public static class AppSettings
{
    public const int CountdownSeconds = 5;

    public static bool TapMode
    {
        get => Preferences.Get("sos_tap_mode", false);
        set => Preferences.Set("sos_tap_mode", value);
    }
    public static bool ShakeEnabled
    {
        get => Preferences.Get("trg_shake", false);
        set => Preferences.Set("trg_shake", value);
    }
    public static bool VolumeEnabled
    {
        get => Preferences.Get("trg_volume", false);
        set => Preferences.Set("trg_volume", value);
    }
}
