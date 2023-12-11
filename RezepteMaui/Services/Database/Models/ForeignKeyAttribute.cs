namespace Rezepte.Services.Database.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignKeyAttribute: Attribute
    {

        public ForeignKeyAttribute() { }

        public Type ParentType { get; set; }

    }
}
