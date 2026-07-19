using System.Collections.ObjectModel;
using ControllerWarcraft.Core.Onboarding;
using ControllerWarcraft.Core.Profiles;
using ControllerWarcraft.Gui.Mvvm;

namespace ControllerWarcraft.Gui.ViewModels;

/// <summary>
/// ViewModel del <b>wizard di primo avvio</b>. Guida l'utente attraverso i passi che i tester
/// trovavano ostici:
/// <list type="number">
///   <item>Benvenuto + <b>test del controller</b> (pannello live).</item>
///   <item>Scelta della <b>versione</b> (Ascension/Classic/Retail) + preset di classe opzionale →
///         salvato come profilo attivo.</item>
///   <item>Spiegazione dei <b>keybinding da impostare in WoW</b> (tabella 1-9, Shift/Ctrl/Shift+Ctrl).</item>
///   <item>Avviso sul <b>prompt UAC/admin</b> di cwapp e su come funziona la <b>modalità cursore</b>
///         (toggle R3, o Hold/None).</item>
/// </list>
/// Al termine (o allo skip) imposta <see cref="AppSettings.SetupCompleted"/> = true così non
/// riappare. Riapribile dalla finestra principale.
///
/// Come la GUI in generale, il wizard <b>non invia mai input</b>: scrive solo profili/settings via
/// <see cref="ProfileManager"/> e legge il controller in sola lettura nel pannello di test.
/// </summary>
public sealed class WizardViewModel : ObservableObject
{
    private readonly ProfileManager _manager;
    private int _currentStep;
    private string _status = "";

    /// <summary>Titoli dei passi (l'indice = numero di passo).</summary>
    public static readonly string[] StepTitles =
    {
        "Benvenuto e test del controller",
        "Scegli la versione di gioco",
        "Imposta i keybinding dentro WoW",
        "Permessi (UAC) e modalità cursore",
    };

    /// <summary>Sollevato quando il wizard va chiuso. Argomento: esito (true = completato/fine).</summary>
    public event Action<bool>? RequestClose;

    public WizardViewModel(ProfileManager? manager = null)
    {
        _manager = manager ?? new ProfileManager();

        NextCommand = new RelayCommand(Next, () => !IsLastStep);
        BackCommand = new RelayCommand(Back, () => CurrentStep > 0);
        FinishCommand = new RelayCommand(Finish);
        SkipCommand = new RelayCommand(Skip);

        foreach (var row in OnboardingInfo.WowKeybindings) Keybindings.Add(row);

        RefreshProfiles();
        RefreshClassPresets();
    }

    // -------------------------------------------------------------- passo corrente

    public int CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (SetField(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(StepTitle));
                OnPropertyChanged(nameof(StepIndicator));
                OnPropertyChanged(nameof(IsStep0));
                OnPropertyChanged(nameof(IsStep1));
                OnPropertyChanged(nameof(IsStep2));
                OnPropertyChanged(nameof(IsStep3));
                OnPropertyChanged(nameof(IsLastStep));
                OnPropertyChanged(nameof(ShowNext));
            }
        }
    }

    public string StepTitle => StepTitles[CurrentStep];
    public string StepIndicator => $"Passo {CurrentStep + 1} di {StepTitles.Length}";

    public bool IsStep0 => CurrentStep == 0;
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;

    public bool IsLastStep => CurrentStep == StepTitles.Length - 1;
    public bool ShowNext => !IsLastStep;

    // -------------------------------------------------------------- passo 1: test controller

    /// <summary>Pannello di test del controller (sola lettura) mostrato nel primo passo.</summary>
    public ControllerTestViewModel ControllerTest { get; } = new();

    // -------------------------------------------------------------- passo 2: versione + classe

    public ObservableCollection<ProfileInfo> Profiles { get; } = new();

    private ProfileInfo? _selectedProfile;
    public ProfileInfo? SelectedProfile
    {
        get => _selectedProfile;
        set => SetField(ref _selectedProfile, value);
    }

    public ObservableCollection<ClassPresetInfo> ClassPresets { get; } = new();

    private ClassPresetInfo? _selectedClassPreset;
    public ClassPresetInfo? SelectedClassPreset
    {
        get => _selectedClassPreset;
        set => SetField(ref _selectedClassPreset, value);
    }

    // -------------------------------------------------------------- passo 3: keybinding WoW

    public ObservableCollection<KeybindingRow> Keybindings { get; } = new();

    // -------------------------------------------------------------- stato

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    // -------------------------------------------------------------- comandi

    public RelayCommand NextCommand { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand FinishCommand { get; }
    public RelayCommand SkipCommand { get; }

    // -------------------------------------------------------------- logica

    private void RefreshProfiles()
    {
        Profiles.Clear();
        foreach (var p in _manager.ListProfiles()) Profiles.Add(p);

        // Preseleziona il profilo attivo corrente, altrimenti il primo (idealmente Ascension).
        var activeStem = _manager.LoadSettings().ActiveProfile;
        SelectedProfile = Profiles.FirstOrDefault(p => p.FileName == activeStem)
                          ?? Profiles.FirstOrDefault(p => p.FileName == "ascension")
                          ?? Profiles.FirstOrDefault();
    }

    private void RefreshClassPresets()
    {
        ClassPresets.Clear();
        foreach (var c in _manager.ListClassPresets()) ClassPresets.Add(c);
    }

    private void Next()
    {
        if (!IsLastStep) CurrentStep++;
    }

    private void Back()
    {
        if (CurrentStep > 0) CurrentStep--;
    }

    /// <summary>
    /// Applica le scelte (profilo attivo + eventuale preset di classe), marca il setup come
    /// completato e chiude. Se è selezionato un preset di classe, lo fonde sul profilo di versione
    /// e salva il risultato come profilo utente (mantenendo lo stem della versione), poi lo attiva.
    /// </summary>
    private void Finish()
    {
        try
        {
            var stem = SelectedProfile?.FileName;

            if (stem is not null && SelectedClassPreset is not null)
            {
                var profile = _manager.Load(stem);
                var preset = _manager.LoadClassPreset(SelectedClassPreset.FileName);
                if (profile is not null && preset is not null)
                {
                    preset.ApplyTo(profile);
                    _manager.Save(profile, stem); // scrive nella cartella utente
                }
            }

            if (stem is not null)
                _manager.SetActiveProfile(stem);

            _manager.MarkSetupCompleted();
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            Status = $"Errore durante il salvataggio: {ex.Message}";
        }
    }

    /// <summary>Salta il wizard: marca comunque il setup come completato (non riappare) senza toccare il profilo.</summary>
    private void Skip()
    {
        _manager.MarkSetupCompleted();
        RequestClose?.Invoke(false);
    }
}
