using System;

namespace OneFiftyOne.TearDrops.Repository
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited=false)]
    public class Table : Attribute
    {
        public string Name { get; set; }
        public Table(string name) { this.Name = name; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Invalidates : Attribute
    {
        public string[] Tables { get; set; }
        public Invalidates(params string[] tables) { this.Tables = tables; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class PrimaryKey : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class ForeignKey : Attribute
    {
        public string Src { get; private set; }
        public string Dest { get; private set; }

        public ForeignKey(string sourceColumn, string destinationColumn)
        {
            Src = sourceColumn;
            Dest = destinationColumn;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class Computed : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class Parameter : Attribute
    {
        public string Field { get; set; }
        public string Table { get; set; }

        public Parameter(string tableName, string fieldName)
        {
            this.Field = fieldName;
            this.Table = tableName;
        }

        public Parameter(string tableName)
        {
            this.Field = null;
            this.Table = tableName;
        }
    }
}
