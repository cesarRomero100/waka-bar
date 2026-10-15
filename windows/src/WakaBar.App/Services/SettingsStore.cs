using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.Storage;
using WakaBar.Core;

namespace WakaBar.App.Services;

/// <summary>
/// The API key lives in Windows Credential Manager (like the Keychain on macOS);
/// the server URL and the daily goal in the package's local settings.
/// </summary>
public sealed class SettingsStore
{
    private const string VaultResource = "WakaBar";
    private const string VaultUser = "apiKey";
    private const string BaseUrlKey = "baseUrl";
    private const string DailyGoalKey = "dailyGoalSeconds";

    private readonly IPropertySet values = ApplicationData.Current.LocalSettings.Values;

    /// <summary>The server set in Settings, or null for WakaTime.</summary>
    public string? CustomBaseUrl
    {
        get => values.TryGetValue(BaseUrlKey, out var value) ? value as string : null;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                values.Remove(BaseUrlKey);
            }
            else
            {
                values[BaseUrlKey] = value;
            }
        }
    }

    public string BaseUrl => CustomBaseUrl ?? WakaTimeClient.DefaultBaseUrl;

    /// <summary>Daily goal in seconds; 0 means off.</summary>
    public double DailyGoalSeconds
    {
        get => values.TryGetValue(DailyGoalKey, out var value) && value is double seconds ? seconds : 0;
        set => values[DailyGoalKey] = value;
    }

    /// <summary>The saved API key, or null when there is none.</summary>
    public string? ApiKey
    {
        get
        {
            try
            {
                var credential = new PasswordVault().Retrieve(VaultResource, VaultUser);
                credential.RetrievePassword();
                return string.IsNullOrEmpty(credential.Password) ? null : credential.Password;
            }
            catch (Exception)
            {
                // Retrieve throws when there is no saved credential.
                return null;
            }
        }
    }

    /// <summary>Replaces the saved API key; an empty key removes it.</summary>
    public void SetApiKey(string? apiKey)
    {
        var vault = new PasswordVault();
        try
        {
            vault.Remove(vault.Retrieve(VaultResource, VaultUser));
        }
        catch (Exception)
        {
            // Nothing saved yet.
        }

        if (!string.IsNullOrEmpty(apiKey))
        {
            vault.Add(new PasswordCredential(VaultResource, VaultUser, apiKey));
        }
    }
}
