using System.ComponentModel;

namespace AvalonEdit.Utils;

public sealed class PropertyChangedWeakEventManager : WeakEventManagerBase<PropertyChangedWeakEventManager, INotifyPropertyChanged>
{
    protected override void StartListening(INotifyPropertyChanged source)
    {
        source.PropertyChanged += DeliverEvent;
    }

    protected override void StopListening(INotifyPropertyChanged source)
    {
        source.PropertyChanged -= DeliverEvent;
    }
}