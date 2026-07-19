using System.Collections.ObjectModel;
using ControllerWarcraft.Core.Onboarding;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// ViewModel principale dell'editor di remap. Espone:
///   - l'elenco dei profili disponibili e la selezione;
///   - le impostazioni del profilo caricato (movimento, mouselook + curva, cursore, binding di sistema);
///   - la tabella abilita' editabile (4 layer: Base/+LB/+RB/+LB+RB);
///   - le impostazioni globali di Fase 3 (overlay, auto-switch profilo) da <c>settings.json</c>;
///   - i comandi Ricarica / Salva profilo / Imposta come attivo / Salva impostazioni.
///
/// La GUI <b>non</b> invia mai input al gioco: si limita a leggere e scrivere i profili e le
/// impostazioni JSON tramite il <see cref="ProfileManager"/> condiviso col runtime.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly ProfileManager _manager = new();

    private ProfileInfo? _selectedProfile;
    private ControllerProfile _current = new();
    private string _status = "";
    private string _activeProfileStem = "";

    private AppSettings _settings;

    private KeybindEditorViewModel? _jumpBind;
    private KeybindEditorViewModel? _tabTargetBind;
    private KeybindEditorViewModel? _cursorCancelBind;

    public MainViewModel()
    {
        _settings = _manager.LoadSettings();

        ReloadCommand = new RelayCommand(ReloadCurrent, () => SelectedProfile is not null);
        SaveCommand = new RelayCommand(Save, () => SelectedProfile is not null);
        SetActiveCommand = new RelayCommand(SetActive, () => SelectedProfile is not null);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        AddProcessMapRowCommand = new RelayCommand(() => ProcessMap.Add(new ProcessMapRowViewModel()));
        AddRadialItemCommand = new RelayCommand(() => RadialItems.Add(new RadialItemRowViewModel()));
        ApplyClassPresetCommand = new RelayCommand(ApplyClassPreset, () => SelectedClassPreset is not null);

        LoadSettingsIntoProcessMap();
        RefreshClassPresetList();
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

    // -------------------------------------------------------------- impostazioni bindabili (profilo)

    public ControllerProfile Current
    {
        get => _current;
        private set => SetField(ref _current, value);
    }

    // Esposte separatamente cosi' i cambi di profilo notificano tutti i pannelli.
    public MovementSettings Movement => _current.Movement;
    public MouselookSettings Mouselook => _current.Mouselook;
    public ResponseCurve MouselookCurve => _current.Mouselook.Curve;
    public CursorSettings Cursor => _current.Cursor;
    public InputHardeningSettings InputHardening => _current.InputHardening;

    // Attivazione modalità cursore (hardening input): pulsante + Toggle/Hold/None.
    public CursorActivationButton CursorActivationButton
    {
        get => _current.Cursor.ActivationButton;
        set { _current.Cursor.ActivationButton = value; OnPropertyChanged(); }
    }

    public CursorActivationMode CursorActivationMode
    {
        get => _current.Cursor.ActivationMode;
        set { _current.Cursor.ActivationMode = value; OnPropertyChanged(); }
    }

    // Hardening: hold minimo (ms) per i click-stick / Start contro le pressioni accidentali.
    public int ThumbClickMinHoldMs
    {
        get => _current.InputHardening.ThumbClickMinHoldMs;
        set { _current.InputHardening.ThumbClickMinHoldMs = value; OnPropertyChanged(); }
    }

    // Binding di sistema editabili (Fase 3).
    public KeybindEditorViewModel? JumpBind { get => _jumpBind; private set => SetField(ref _jumpBind, value); }
    public KeybindEditorViewModel? TabTargetBind { get => _tabTargetBind; private set => SetField(ref _tabTargetBind, value); }
    public KeybindEditorViewModel? CursorCancelBind { get => _cursorCancelBind; private set => SetField(ref _cursorCancelBind, value); }

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

    // -------------------------------------------------------------- test controller (live, sola lettura)

    /// <summary>Pannello di test del controller (tab "Test controller"). Legge il gamepad in sola lettura.</summary>
    public ControllerTestViewModel ControllerTest { get; } = new();

    // -------------------------------------------------------------- onboarding wizard

    /// <summary>True se il wizard di primo avvio va mostrato automaticamente (setup non completato).</summary>
    public bool NeedsSetup => OnboardingInfo.NeedsSetup(_manager.LoadSettings());

    /// <summary>
    /// Ricarica impostazioni e profili dopo la chiusura del wizard, così l'eventuale nuovo profilo
    /// attivo/scelte del wizard sono subito riflesse nell'editor.
    /// </summary>
    public void ReloadAfterWizard()
    {
        _settings = _manager.LoadSettings();
        OnPropertyChanged(nameof(ShowOverlay));
        OnPropertyChanged(nameof(AutoSwitchEnabled));
        OnPropertyChanged(nameof(PauseWhenGameNotForeground));
        LoadSettingsIntoProcessMap();
        RefreshClassPresetList();
        RefreshProfileList();
    }

    // -------------------------------------------------------------- radial menu (Fase 4)

    /// <summary>Voci del radial menu editabili (etichetta + un solo keybind).</summary>
    public ObservableCollection<RadialItemRowViewModel> RadialItems { get; } = new();

    public bool RadialEnabled
    {
        get => _current.RadialMenu.Enabled;
        set { _current.RadialMenu.Enabled = value; OnPropertyChanged(); }
    }

    public RadialTrigger RadialTrigger
    {
        get => _current.RadialMenu.Trigger;
        set { _current.RadialMenu.Trigger = value; OnPropertyChanged(); }
    }

    public double RadialSelectDeadzone
    {
        get => _current.RadialMenu.SelectDeadzone;
        set { _current.RadialMenu.SelectDeadzone = value; OnPropertyChanged(); }
    }

    // -------------------------------------------------------------- preset di classe (Fase 4)

    /// <summary>Preset di classe disponibili (applicabili sopra il profilo corrente).</summary>
    public ObservableCollection<ClassPresetInfo> ClassPresets { get; } = new();

    private ClassPresetInfo? _selectedClassPreset;
    public ClassPresetInfo? SelectedClassPreset
    {
        get => _selectedClassPreset;
        // CommandManager richiama automaticamente CanExecute di ApplyClassPresetCommand.
        set => SetField(ref _selectedClassPreset, value);
    }

    // -------------------------------------------------------------- impostazioni globali (Fase 3)

    public bool ShowOverlay
    {
        get => _settings.ShowOverlay;
        set { _settings.ShowOverlay = value; OnPropertyChanged(); }
    }

    public bool AutoSwitchEnabled
    {
        get => _settings.AutoSwitchEnabled;
        set { _settings.AutoSwitchEnabled = value; OnPropertyChanged(); }
    }

    public bool PauseWhenGameNotForeground
    {
        get => _settings.PauseWhenGameNotForeground;
        set { _settings.PauseWhenGameNotForeground = value; OnPropertyChanged(); }
    }

    /// <summary>Righe editabili della mappa processo → profilo (auto-switch).</summary>
    public ObservableCollection<ProcessMapRowViewModel> ProcessMap { get; } = new();

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
    public RelayCommand SaveSettingsCommand { get; }
    public RelayCommand AddProcessMapRowCommand { get; }
    public RelayCommand AddRadialItemCommand { get; }
    public RelayCommand ApplyClassPresetCommand { get; }

    // -------------------------------------------------------------- logica (profili)

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
        OnPropertyChanged(nameof(MouselookCurve));
        OnPropertyChanged(nameof(Cursor));
        OnPropertyChanged(nameof(InputHardening));
        OnPropertyChanged(nameof(CursorActivationButton));
        OnPropertyChanged(nameof(CursorActivationMode));
        OnPropertyChanged(nameof(ThumbClickMinHoldMs));
        OnPropertyChanged(nameof(ProfileName));
        OnPropertyChanged(nameof(GameVersion));
        OnPropertyChanged(nameof(Description));

        // Ricrea gli editor dei binding di sistema legati al nuovo profilo.
        JumpBind = new KeybindEditorViewModel(() => _current.System.Jump, kb => _current.System.Jump = kb);
        TabTargetBind = new KeybindEditorViewModel(() => _current.System.TabTarget, kb => _current.System.TabTarget = kb);
        CursorCancelBind = new KeybindEditorViewModel(() => _current.System.CursorCancel, kb => _current.System.CursorCancel = kb);

        Abilities.Clear();
        foreach (var a in loaded.Abilities.OrderBy(a => a.Button).ThenBy(a => a.Layer))
            Abilities.Add(new AbilityRowViewModel(a));

        RebuildRadialFromCurrent();

        Status = $"Caricato: {loaded.Name}  [{info.Source}]  ({info.FilePath})";
    }

    /// <summary>Ricostruisce i controlli del radial menu dal profilo corrente (dopo load/apply preset).</summary>
    private void RebuildRadialFromCurrent()
    {
        RadialItems.Clear();
        foreach (var item in _current.RadialMenu.Items)
            RadialItems.Add(new RadialItemRowViewModel(item));

        OnPropertyChanged(nameof(RadialEnabled));
        OnPropertyChanged(nameof(RadialTrigger));
        OnPropertyChanged(nameof(RadialSelectDeadzone));
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

        // Riporta le voci del radial menu (etichetta + un solo keybind, 1:1).
        _current.RadialMenu.Items = RadialItems.Select(r => r.ToItem()).ToList();

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
        _settings.ActiveProfile = SelectedProfile.FileName;
        _manager.SaveSettings(_settings);
        ActiveProfileStem = SelectedProfile.FileName;
        Status = $"Profilo attivo impostato: {SelectedProfile.FileName}. Riavvia l'App per applicarlo.";
    }

    // -------------------------------------------------------------- logica (preset di classe)

    private void RefreshClassPresetList()
    {
        ClassPresets.Clear();
        foreach (var c in _manager.ListClassPresets())
            ClassPresets.Add(c);
    }

    /// <summary>
    /// Applica il preset di classe selezionato SOPRA il profilo corrente (in memoria): sostituisce
    /// gli override di abilità e, se presente, il radial menu. Non salva: l'utente rivede il
    /// risultato e poi preme "Salva" per scriverlo come profilo utente.
    /// </summary>
    private void ApplyClassPreset()
    {
        if (SelectedClassPreset is null) return;

        var preset = _manager.LoadClassPreset(SelectedClassPreset.FileName);
        if (preset is null)
        {
            Status = $"Impossibile caricare il preset di classe '{SelectedClassPreset.FileName}'.";
            return;
        }

        preset.ApplyTo(_current);

        // Riflette il merge nei controlli: tabella abilità + radial.
        Abilities.Clear();
        foreach (var a in _current.Abilities.OrderBy(a => a.Button).ThenBy(a => a.Layer))
            Abilities.Add(new AbilityRowViewModel(a));
        RebuildRadialFromCurrent();

        Status = $"Preset di classe applicato: {preset.Name}. Rivedi e premi 'Salva' per scriverlo.";
    }

    // -------------------------------------------------------------- logica (impostazioni globali)

    private void LoadSettingsIntoProcessMap()
    {
        ProcessMap.Clear();
        foreach (var kv in _settings.ProcessProfileMap)
            ProcessMap.Add(new ProcessMapRowViewModel(kv.Key, kv.Value));
    }

    private void SaveSettings()
    {
        // Ricostruisce la mappa dalle righe (ignora quelle incomplete).
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ProcessMap)
        {
            var proc = row.Process?.Trim();
            var stem = row.ProfileStem?.Trim();
            if (!string.IsNullOrEmpty(proc) && !string.IsNullOrEmpty(stem))
                map[proc] = stem;
        }
        _settings.ProcessProfileMap = map;

        _manager.SaveSettings(_settings);
        Status = $"Impostazioni salvate in {_manager.SettingsPath}. Riavvia l'App per applicarle.";
    }
}
