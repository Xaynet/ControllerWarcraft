using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// Riga editabile della mappa auto-switch (Fase 3, punto 4): <c>nome processo → file stem profilo</c>.
/// Es. <c>wow → retail</c>. Il nome processo va scritto senza <c>.exe</c> (il resolver è tollerante).
/// </summary>
public sealed class ProcessMapRowViewModel : ObservableObject
{
    private string _process;
    private string _profileStem;

    public ProcessMapRowViewModel(string process = "", string profileStem = "")
    {
        _process = process;
        _profileStem = profileStem;
    }

    /// <summary>Nome dell'eseguibile in primo piano (senza <c>.exe</c>), es. <c>wow</c>, <c>ascension</c>.</summary>
    public string Process { get => _process; set => SetField(ref _process, value); }

    /// <summary>File stem del profilo da caricare, es. <c>retail</c>, <c>classic</c>.</summary>
    public string ProfileStem { get => _profileStem; set => SetField(ref _profileStem, value); }
}
