using AutoEncodeUtilities.Data;
using AutoEncodeUtilities.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace AutoEncodeUtilities.Collections
{
    public class BulkObservableCollection<T> : 
        ObservableCollection<T>,
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

        public void Update(BulkObservableCollection<T> oldCollection)
        {
            IEnumerable<T> remove = oldCollection.Where(x => !Items.Any(y => y.Equals(x)));
            oldCollection.RemoveRange(remove);

            foreach (T item in Items)
            {
                T oldData = oldCollection.SingleOrDefault(x => x.Equals(item));
                if (oldData is not null)
                {
                    if (oldData is IUpdateable<T> updateAbleOldData) updateAbleOldData.Update(item);
                    else item.CopyProperties(oldData);
                }
                else
                {
                    oldCollection.Add(item);
                }
            }
        }
    }
}
