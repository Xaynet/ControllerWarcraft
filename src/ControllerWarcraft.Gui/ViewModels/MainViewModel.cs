using System.Collections.ObjectModel;
using System.IO;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// ViewModel principale dell'editor di remap (Fase 2, punto 3). Espone:
///   - l'elenco dei profili disponibili e la selezione;
///   - le impostazioni del profilo caricato (movimento, mouselook, cursore) in binding;
///   - la tabella abilita' editabile;
///   - i comandi Ricarica / Salva / Imposta come attivo.
///
/// La GUI <b>non</b> invia mai input al gioco: si limita a leggere e scrivere i profili
/// JSON tramite il <see cref="ProfileManager"/> condiviso col runtime.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ProfileManager _manager = new();

    private ProfileInfo? _selectedProfile;
    private ControllerProfile _current = new();
    private string _status = "";
    private string _activeProfileStem = "";

    public MainViewModel()
    {
        ReloadCommand = new RelayCommand(ReloadCurrent, () => SelectedProfile is not null);
        SaveCommand = new RelayCommand(Save, () => SelectedProfile is not null);
        SetActiveCommand = new RelayCommand(SetActive, () => SelectedProfile is not null);

        RefreshProfileList();
    }

    // -------------------------------------------------------------- elenco profili

    public ObservableCollection<ProfileInfo> Profiles { get; } = new();

    public ProfileInfo? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetField(ref _selectedProfile, value) && value is not null)
                LoadProfile(value);
        }
    }

    /// <summary>Stem del profilo attualmente marcato come attivo in settings.json.</summary>
    public string ActiveProfileStem
    {
        get => _activeProfileStem;
        private set => SetField(ref _activeProfileStem, value);
    }

    // -------------------------------------------------------------- impostazioni bindabili

    public ControllerProfile Current
    {
        get => _current;
        private set => SetField(ref _current, value);
    }

    // Esposte separatamente cosi' i cambi di profilo notificano tutti i pannelli.
    public MovementSettings Movement => _current.Movement;
    public MouselookSettings Mouselook => _current.Mouselook;
    public CursorSettings Cursor => _current.Cursor;
    public SystemBindings System => _current.System;

    public string ProfileName
    {
        get => _current.Name;
        set { _current.Name = value; OnPropertyChanged(); }
    }

    public string GameVersion
    {
        get => _current.GameVersion;
        set { _current.GameVersion = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _current.Description;
        set { _current.Description = value; OnPropertyChanged(); }
    }

    public ObservableCollection<AbilityRowViewModel> Abilities { get; } = new();

    // -------------------------------------------------------------- stato / info

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public string PresetDir => _manager.PresetProfilesDir;
    public string UserDir => _manager.UserProfilesDir;

    // -------------------------------------------------------------- comandi

    public RelayCommand ReloadCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand SetActiveCommand { get; }

    // -------------------------------------------------------------- logica

    private void RefreshProfileList()
    {
        var previouslySelected = SelectedProfile?.FileName;

        Profiles.Clear();
        foreach (var p in _manager.ListProfiles())
            Profiles.Add(p);

        ActiveProfileStem = _manager.LoadSettings().ActiveProfile;

        // Ripristina/inizializza la selezione.
        ProfileInfo? toSelect =
            (previouslySelected is not null ? Profiles.FirstOrDefault(p => p.FileName == previouslySelected) : null)
            ?? Profiles.FirstOrDefault(p => p.FileName == ActiveProfileStem)
            ?? Profiles.FirstOrDefault();

        SelectedProfile = toSelect;
    }

    private void LoadProfile(ProfileInfo info)
    {
        var loaded = _manager.Load(info.FileName);
        if (loaded is null)
        {
            Status = $"Impossibile caricare '{info.FileName}'.";
            return;
        }

        Current = loaded;
        OnPropertyChanged(nameof(Movement));
        OnPropertyChanged(nameof(Mouselook));
        OnPropertyChanged(nameof(Cursor));
        OnPropertyChanged(nameof(System));
        OnPropertyChanged(nameof(ProfileName));
        OnPropertyChanged(nameof(GameVersion));
        OnPropertyChanged(nameof(Description));

        Abilities.Clear();
        foreach (var a in loaded.Abilities.OrderBy(a => a.Button).ThenBy(a => a.Layer))
            Abilities.Add(new AbilityRowViewModel(a));

        Status = $"Caricato: {loaded.Name}  [{info.Source}]  ({info.FilePath})";
    }

    private void ReloadCurrent()
    {
        if (SelectedProfile is not null) LoadProfile(SelectedProfile);
    }

    private void Save()
    {
        if (SelectedProfile is null) return;

        // Riporta la tabella editata nel profilo.
        _current.Abilities = Abilities.Select(r => r.ToBinding()).ToList();
        _current.InvalidateIndex();

        // Salva sempre nella cartella utente (i preset restano sola lettura).
        var stem = SelectedProfile.Source == ProfileSource.Preset
            ? ProfileManager.Slugify(_current.Name)
            : SelectedProfile.FileName;

        var path = _manager.Save(_current, stem);
        Status = $"Salvato in {path}";

        RefreshProfileList();
        // Riseleziona il file appena salvato (ora in cartella utente).
        SelectedProfile = Profiles.FirstOrDefault(p => p.FileName == stem) ?? SelectedProfile;
    }

    private void SetActive()
    {
        if (SelectedProfile is null) return;
        _manager.SetActiveProfile(SelectedProfile.FileName);
        ActiveProfileStem = SelectedProfile.FileName;
        Status = $"Profilo attivo impostato: {SelectedProfile.FileName}. Riavvia l'App per applicarlo.";
    }
}
