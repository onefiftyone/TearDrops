using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OneFiftyOne.TearDrops.Repository
{

    /// <summary>
    /// Base DataModel Class from which all other DataModels should derive 
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class BaseModel
    {
        [Computed]
        public int RecursionLevel { get; internal set; }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseModel"/> class.
        /// </summary>
        public BaseModel()
        {
        }

        /// <summary>
        /// Generic Copy Constructor 
        /// </summary>
        /// <param name="m">The m.</param>
        public BaseModel(BaseModel m)
            : this()
        {
            if (this.GetType() != m.GetType())
                throw new InvalidCastException();

            foreach (var prop in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => !p.GetCustomAttributes(true).OfType<Computed>().Any()).Where(p => p.GetIndexParameters().Length == 0))
            {
                prop.SetValue(this, prop.GetValue(m, null), null);
            }
        }

        #endregion


        #region CACHING RELATED
        /*
        /// <summary>
        /// Generates a cache key for an instance of an object.
        /// </summary>
        /// <param name="generic">if set to <c>true</c> return the generic (table based) cache key.</param>
        /// <returns></returns>
        public string GetCacheKey(bool generic = false)
        {
            Type type = GetType();
            string hashString = OneFiftyOne.TearDrops.Repository.BaseRepository.GetDatabaseName() + "_" + ((generic) ? this.GetTableName() : GetCacheTableHandle());

            if (!generic)
            {
                hashString = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => (PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null).Aggregate(hashString, (current, prop) => current + "_{0}:{1}".FormatString(prop.Name, prop.GetValue(this, null)));
            }

            return hashString.ToLowerInvariant().GetHashCode().ToString();
        }

        /// <summary>
        /// Gets the handle to be used when creating the cache key hash. This will be either the string name of the table as defined by PonTable (for unified cache models) or the string name of the model type
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public string GetCacheTableHandle()
        {
            UnifiedCache uc = (UnifiedCache)Attribute.GetCustomAttribute(GetType(), typeof(UnifiedCache));
            return (uc != null) ? GetTableName() : GetType().Name;
        }

        /// <summary>
        /// Converts a simple record to a model of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="r">The r.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidCastException"></exception>
        public static T ConvertTo<T>(Simple.Data.SimpleRecord r) where T : BaseModel, new()
        {
            var model = new T();
            IDictionary<string, object> data = (IDictionary<string, object>)r;
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length == 0)
                {
                    object value;
                    if (data.TryGetValue(prop.Name, out value))
                    {
                        try
                        {
                            if (value == null)
                            {
                                prop.SetValue(model, null, null);
                            }
                            else if (!prop.PropertyType.IsAssignableFrom(value.GetType()))
                            {
                                //handle nullable type differences
                                var nType = Nullable.GetUnderlyingType(prop.PropertyType);
                                if (nType != null)
                                {
                                    var conv = Activator.CreateInstance(nType);
                                    conv = Convert.ChangeType(value, nType);
                                    prop.SetValue(model, conv, null);
                                }
                                else
                                {
                                    //probably an invalid property type for this column, but givew it a try anyway
                                    prop.SetValue(model, Convert.ChangeType(value, prop.PropertyType), null);
                                }
                            }
                            else
                                prop.SetValue(model, value, null);
                        }
                        catch(Exception e)
                        {
                            throw new Exception("Unable to convert datatypes", e);
                        }
                    }
                    else if ((Computed)Attribute.GetCustomAttribute(prop, typeof(Computed)) != null)
                        continue; //Ignore computed properties on the model.
                    else
                        throw new InvalidCastException();
                }
            }

            return model;
        }
        */
        #endregion

        /// <summary>
        /// Gets the name of the table as denoted by the PonTable attribute, or the name of the class if it is missing
        /// </summary>
        /// <returns></returns>
        public string GetTableName()
        {
            Table table = (Table)Attribute.GetCustomAttribute(GetType(), typeof(Table));
            return (table != null) ? table.Name.ToLower() : GetType().Name;
        }

        /// <summary>
        /// Override of ToString(), useful to include in exceptions for tracing back errors.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prop in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        sb.AppendFormat("{0} = {1}, ", prop.Name, prop.GetValue(this, null));
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }

            return sb.ToString().Trim().TrimEnd(',');
        }

        /// <summary>
        /// Determines whether another model is equal to this model
        /// </summary>
        /// <param name="other">Another model to compare this to</param>
        /// <returns>True if the other model equals this model (all property values are equal)</returns>
        public bool Equals(BaseModel other)
        {
            if (this.GetType() != other.GetType())
            {
                return false;
            }

            foreach (PropertyInfo property in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Don't compare indexer properties.
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // Don't compare foreign key properties.
                if (property.GetCustomAttributes(typeof(Computed), true).Any() || property.GetCustomAttributes(typeof(ForeignKey), true).Any())
                {
                    continue;
                }

                object thisValue = property.GetValue(this, null);
                object otherValue = property.GetValue(other, null);
                if (thisValue == null)
                {
                    if (otherValue == null)
                    {
                        continue;
                    }

                    return false;
                }

                if (thisValue.Equals(otherValue))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> value of the specified column.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        [JsonIgnore]
        public object this[string column]
        {
            get
            {
                BaseModel currentBaseModel = this;
                var property = this.GetType().GetProperty(column, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                //the property is not found on the model
                if (property == null)
                {
                    //search through related models
                    property = FindPropertyInRelatedModels(column, ref currentBaseModel);

                    //if the property is still unfound, throw an error
                    if (property == null) { throw new IndexOutOfRangeException("Could not find property: " + column); }
                }

                return property.GetValue(currentBaseModel, null);
            }

            set
            {
                BaseModel currentBaseModel = this;
                var property = this.GetType().GetProperty(column, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                {
                    //search through related models
                    property = FindPropertyInRelatedModels(column, ref currentBaseModel);

                    //if the property is still unfound, throw an error
                    if (property == null) { throw new IndexOutOfRangeException("Could not find property: " + column); }
                }
                property.SetValue(currentBaseModel, value, null);
            }
        }

        ///// <summary>
        ///// Deep object clone.
        ///// </summary>
        ///// <returns></returns>
        public T DeepClone<T>()
        {
            if (Object.ReferenceEquals(this, null))
                return default(T);

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Returns the model's property given its name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>PropertyInfo object of specified property.  Null if the property is not found.</returns>
        public PropertyInfo GetProperty(string propertyName)
        {
            return this.GetType().GetProperties().FirstOrDefault(pi => pi.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Returns the model's properties.
        /// </summary>
        /// <returns>List of PropertyInfo objects.</returns>
        public List<PropertyInfo> GetProperties()
        {
            List<PropertyInfo> list = new List<PropertyInfo>();

            foreach (var propertyInfo in this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.DeclaringType != typeof(BaseModel)))
            {
                var attrs = propertyInfo.GetCustomAttributes(true);
                bool cont = attrs.Any(attr => attr is Computed || attr is ForeignKey || attr is Parameter);
                if (cont)
                    continue;

                list.Add(propertyInfo);
            }

            return list;
        }

        /// <summary>
        /// Returns a string containing the primary key properties and values for the object.
        /// </summary>
        /// <returns></returns>
        public string GetPrimaryKeyString()
        {
            IEnumerable<PropertyInfo> keys = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => (PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null);

            StringBuilder sb = new StringBuilder();
            foreach (PropertyInfo key in keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("  ");
                }
                sb.Append(key.Name + ":" + Convert.ToString(key.GetValue(this, null)));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the name of every primary key property.
        /// </summary>
        /// <returns></returns>
        public List<string> GetPrimaryKeyPropertyList()
        {
            IEnumerable<PropertyInfo> keys = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => (PrimaryKey)Attribute.GetCustomAttribute(prop, typeof(PrimaryKey)) != null);
            return keys.Select(k => k.Name).ToList();

        }

        #region implementation

        /// <summary>
        /// finds instantiated data models in the passed model and attempts to find the passed property name in them
        /// </summary>
        /// <param name="propertyName">the name to find (not case-sensitive)</param>
        /// <param name="currentBaseModel">a ref variable of the base model</param>
        /// <returns>PropertyInfo object</returns>
        private PropertyInfo FindPropertyInRelatedModels(string propertyName, ref BaseModel currentBaseModel)
        {
            PropertyInfo property = null;

            //get a list of properties in the current model with "model" in the name (if any) and iterate them to find the requested property
            List<PropertyInfo> lstProps = this.GetType().GetProperties().Where(p => p.PropertyType.Name.ToLower().Contains("model")).ToList();
            foreach (PropertyInfo containedModel in lstProps)
            {
                //set the current model as the new base, get the property, and return it
                BaseModel bs = (BaseModel)this[containedModel.Name];
                property = bs.GetType().GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    currentBaseModel = bs;
                    return property;
                }
            }

            return property;
        }

        #endregion
    }
}
