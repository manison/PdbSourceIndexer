namespace PdbSourceIndexer
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionAttribute : Attribute
    {
        public string Alias { get; }

        public string Description { get; }

        public OptionAttribute(string alias, string description)
        {
            this.Alias = alias;
            this.Description = description;
        }
    }
}
