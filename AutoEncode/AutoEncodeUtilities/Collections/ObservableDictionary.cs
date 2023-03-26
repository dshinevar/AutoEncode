using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoEncodeUtilities.Collections
{
    /*public class ObservableDictionary<TKey, TValue> :
        ICollection<KeyValuePair<TKey, TValue>>,
        IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
    {
        private readonly IDictionary<TKey, TValue> Dictionary;

        public ObservableDictionary() 
            : this(new Dictionary<TKey, TValue>()) { }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            Dictionary = dictionary;
        }

        #region Inner Functions
        private void AddAndNotify(TKey key, TValue value) => AddAndNotify(new KeyValuePair<TKey, TValue>(key, value));
        private void AddAndNotify(KeyValuePair<TKey, TValue> item)
        {
            Dictionary.Add(item);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            OnPropertyChanged("Count");
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));
        }

        private bool RemoveAndNotify(TKey key)
        {
            if (Dictionary.TryGetValue(key, out TValue value) && Dictionary.Remove(key))
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, 
                                                                            new KeyValuePair<TKey, TValue>(key, value)));
                OnPropertyChanged("Count");
                OnPropertyChanged(nameof(Keys));
                OnPropertyChanged(nameof(Values));
            }

            return false;
        }

        private void UpdateAndNotify(TKey key, TValue value)
        {
            if (Dictionary.TryGetValue(key, out TValue oldValue))
            {
                Dictionary[key] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                                            new KeyValuePair<TKey, TValue>(key, value),
                                                                            new KeyValuePair<TKey, TValue>(key, oldValue)));
                OnPropertyChanged(nameof(Values));
            }
            else
            {
                AddAndNotify(key, value);
            }
        }


        #endregion Inner Functions

        #region IDictionary
        public void Add(TKey key, TValue value) => AddAndNotify(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => AddAndNotify(item);
        //public bool Remove(TKey key) { }
        public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
        public ICollection<TKey> Keys => Dictionary.Keys;
        public ICollection<TValue> Values => Dictionary.Values;
        public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);
        public TValue this[TKey key]
        {
            get => Dictionary[key];
            set => UpdateAndNotify(key, value);
        }

        #endregion IDictionary

        #region ICollection
        int ICollection<KeyValuePair<TKey, TValue>>.Count => Dictionary.Count;
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => AddAndNotify(item);
        #endregion ICollection

        #region Events
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args) 
            => CollectionChanged?.Invoke(this, args);
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion Events
    }
    */
}
