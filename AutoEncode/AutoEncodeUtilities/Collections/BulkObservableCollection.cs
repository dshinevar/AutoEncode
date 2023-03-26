using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AutoEncodeUtilities.Collections
{
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        public BulkObservableCollection()
            : base() { }

        public BulkObservableCollection(IEnumerable<T> collection)
            : base(collection) { }

        public void AddRange(IEnumerable<T> collection)
        {
            if (collection.Any() is false) return;

            foreach (T item in collection)
            {
                Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Items)));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add));
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            if (collection.Any() is false) return;

            foreach (T item in collection)
            {
                Remove(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Items)));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove));
        }
    }
}
