using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ControllerWarcraft.Core.Profiles;

/// <summary>
/// Base leggera per i modelli di profilo che devono notificare i cambi di proprietà alla GUI
/// (Fase 3): implementa <see cref="INotifyPropertyChanged"/> così, ad esempio, uno slider e la
/// textbox numerica legati alla stessa proprietà restano sincronizzati (quando lo slider scrive
/// il valore, la textbox si aggiorna e viceversa).
///
/// È innocua per la serializzazione: System.Text.Json (de)serializza solo le <b>proprietà</b>
/// pubbliche, mentre l'evento <see cref="PropertyChanged"/> viene ignorato. A runtime, se nessuno
/// è sottoscritto (es. l'App), le notifiche sono no-op.
/// </summary>
public abstract class ObservableModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Imposta il campo e notifica se il valore è cambiato. Restituisce true se cambiato.</summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
