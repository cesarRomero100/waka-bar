using Windows.Storage;
using WakaBar.Core;

namespace WakaBar.App.Services;

/// <summary>The latest snapshot, saved in the package's local folder so it shows right away on the next launch.</summary>
public static class SnapshotCache
{
    private static string FilePath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "snapshot.json");

    public static Snapshot? Load()
    {
        try
        {
            return SnapshotJson.Deserialize(File.ReadAllText(FilePath));
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void Save(Snapshot snapshot)
    {
        try
        {
            // Write then rename, so a crash mid-write never leaves a half file.
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, SnapshotJson.Serialize(snapshot));
            File.Move(temporary, FilePath, overwrite: true);
        }
        catch (IOException)
        {
            // Only a cache: the next refresh fetches the numbers again.
        }
    }

    public static void Clear()
    {
        try
        {
            File.Delete(FilePath);
        }
        catch (IOException)
        {
        }
    }
}
