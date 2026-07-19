using System.IO;

namespace ControllerWarcraft.Tests;

/// <summary>
/// Helper per individuare la root del repository a partire dalla cartella dell'assembly di test,
/// così i test che leggono i preset JSON reali (<c>profiles/</c>) funzionano indipendentemente da
/// dove viene eseguito <c>dotnet test</c> (locale o CI).
/// </summary>
internal static class TestPaths
{
    /// <summary>Root del repo: la prima cartella risalendo che contiene <c>ControllerWarcraft.slnx</c>.</summary>
    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>Cartella <c>profiles/</c> versionata nel repo.</summary>
    public static string ProfilesDir => Path.Combine(RepoRoot, "profiles");

    /// <summary>Cartella <c>profiles/classes/</c> con i preset di classe.</summary>
    public static string ClassesDir => Path.Combine(ProfilesDir, "classes");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ControllerWarcraft.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Impossibile individuare la root del repo (ControllerWarcraft.slnx) da " + AppContext.BaseDirectory);
    }
}
