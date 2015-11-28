using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace OneFiftyOne.TearDrops.Repository
{
    /// <summary>
    /// Dynamic Object that represents a Single Database Row
    /// </summary>
    public class DynamicRecord : DynamicObject, IDictionary<string, object>, ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable
    {
        private Dictionary<string, object> _dictionary;
        private Dictionary<string, Type> _fieldTypes;

        public DynamicRecord()
        {
            _dictionary = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
            _fieldTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);
        }

        public DynamicRecord(IDictionary<string, object> source)
        {
            _dictionary = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
            _fieldTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

            foreach (var item in source)
            {
                if (item.Value != null)
                    _fieldTypes.Add(item.Key, item.Value.GetType());
                _dictionary.Add(item.Key, item.Value);

            }
        }

        public static explicit operator ExpandoObject(DynamicRecord record)
        {
            var retVal = new ExpandoObject() as IDictionary<string, object>;
            foreach (var item in record._dictionary)
                retVal.Add(item);

            return (ExpandoObject)retVal;
        }

        public IEnumerator GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
        

        public void Add(KeyValuePair<string, object> item)
        {
            _dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            array.ToList().InsertRange(arrayIndex, _dictionary);
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _dictionary.Remove(item.Key);
        }

        public void Add(string key, object value)
        {
            _dictionary.Add(key, value);
        }

        public void Add(string key, object value, Type type)
        {
            _dictionary.Add(key, value);
            _fieldTypes.Add(key, type);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<object> Values
        {
            get { return _dictionary.Values; }
        }

        public object this[string key]
        {
            get
            {
                return _dictionary[key];
            }
            set
            {
                _dictionary[key] = value;
            }
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _dictionary.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            object value;
            if (_dictionary.TryGetValue(binder.Name, out value))
            {
                result = value;
                return true;
            }

            return base.TryGetMember(binder, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_dictionary.ContainsKey(binder.Name))
                _dictionary[binder.Name] = value;
            else
                _dictionary.Add(binder.Name, value);

            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = _dictionary[(string)indexes[0]];
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            string key = (string)indexes[0];
            if (_dictionary.ContainsKey(key))
                _dictionary[key] = value;
            else
                _dictionary.Add(key, value);

            return true;
        }

        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            string key = (string)indexes[0];
            _dictionary.Remove(key);
            return true;
        }

        public List<Type> GetTypeList()
        {
            return _fieldTypes.Values.ToList();
        }

        public Type GetColumnType(string key)
        {
            Type val;
            return (_fieldTypes.TryGetValue(key, out val)) ? val : null;
        }

    }
}
