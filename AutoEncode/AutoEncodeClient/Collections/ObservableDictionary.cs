using AutoEncodeUtilities.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AutoEncodeClient.Collections;

public class ObservableDictionary<TKey, TValue> :
    IDictionary<TKey, TValue>,
    ICollection<KeyValuePair<TKey, TValue>>,
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
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
    }

    private bool RemoveAndNotify(TKey key)
    {
        if (Dictionary.TryGetValue(key, out TValue value) && Dictionary.Remove(key))
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                                        new KeyValuePair<TKey, TValue>(key, value)));
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(nameof(Keys));
            OnPropertyChanged(nameof(Values));

            return true;
        }

        return false;
    }

    private void UpdateAndNotify(TKey key, TValue value)
    {
        if (Dictionary.TryGetValue(key, out TValue oldValue))
        {
            if (oldValue is IUpdateable<TValue> updateableOldValue)
            {
                updateableOldValue.Update(value);
            }
            else
            {
                Dictionary[key] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                        new KeyValuePair<TKey, TValue>(key, value),
                                                        new KeyValuePair<TKey, TValue>(key, oldValue),
                                                        Dictionary.Keys.ToList().IndexOf(key)));
            }

            OnPropertyChanged(nameof(Values));
        }
        else
        {
            AddAndNotify(key, value);
        }
    }

    #endregion Inner Functions

    #region IDictionary
    public int Count => Dictionary.Count;
    public bool IsReadOnly => Dictionary.IsReadOnly;
    public void Add(TKey key, TValue value) => AddAndNotify(key, value);
    public void Add(KeyValuePair<TKey, TValue> item) => AddAndNotify(item);
    public bool Remove(TKey key) => RemoveAndNotify(key);
    public bool Remove(KeyValuePair<TKey, TValue> item) => RemoveAndNotify(item.Key);
    public bool Remove(IEnumerable<TKey> keys)
    {
        bool success = true;
        foreach (TKey key in keys)
        {
            success &= RemoveAndNotify(key);
        }
        return success;
    }
    public void Clear()
    {
        Dictionary.Clear();

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(Keys));
        OnPropertyChanged(nameof(Values));
    }
    public bool ContainsKey(TKey key) => Dictionary.ContainsKey(key);
    public bool Contains(KeyValuePair<TKey, TValue> item) => Dictionary.Contains(item);
    public ICollection<TKey> Keys => Dictionary.Keys;
    public ICollection<TValue> Values => Dictionary.Values;
    public bool TryGetValue(TKey key, out TValue value) => Dictionary.TryGetValue(key, out value);
    public TValue this[TKey key]
    {
        get => Dictionary[key];
        set => UpdateAndNotify(key, value);
    }
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => Dictionary.CopyTo(array, arrayIndex);
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Dictionary).GetEnumerator();

    public void Refresh(IDictionary<TKey, TValue> dictionary)
    {
        if (dictionary is null)
        {
            Dictionary.Clear();
        }
        else
        {
            IEnumerable<TKey> keysToRemove = Dictionary.Keys.Where(x => !dictionary.ContainsKey(x));
            Remove(keysToRemove);

            foreach (KeyValuePair<TKey, TValue> item in dictionary)
            {
                UpdateAndNotify(item.Key, item.Value);
            }
        }
    }
    #endregion IDictionary

    #region Events
    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        => CollectionChanged?.Invoke(this, args);
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion Events
}
