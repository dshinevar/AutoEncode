using AutoEncodeUtilities;
using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AutoEncodeClient.Collections
{
    public class BulkObservableCollection<T> : 
        ObservableCollection<T>,
        IUpdateable<IEnumerable<T>>,
        IUpdateable<BulkObservableCollection<T>>
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

        public void Update(IEnumerable<T> newCollection)
        {
            IEnumerable<T> remove = newCollection.Where(x => !Items.Any(y => y.Equals(x)));
            RemoveRange(remove);

            foreach (T item in newCollection)
            {
                T oldItem = Items.SingleOrDefault(x => x.Equals(item));
                if (oldItem is not null)
                {
                    if (oldItem is IUpdateable<T> updateableOldData) updateableOldData.Update(item);
                    else item.CopyProperties(oldItem);
                }
                else
                {
                    Add(item);
                }
            }
        }

        public void Update(BulkObservableCollection<T> newCollection) => Update((IEnumerable<T>)newCollection);
    }
}
