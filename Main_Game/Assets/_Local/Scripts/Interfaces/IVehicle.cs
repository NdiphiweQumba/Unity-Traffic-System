using System;
using System.ComponentModel;
public interface Ivehicle<T>: INotifyPropertyChanged, IObservable<T>
    where T : class
{
    
}
