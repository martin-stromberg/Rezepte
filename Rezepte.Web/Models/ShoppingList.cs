using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Models
{
    public sealed class ShoppingList
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; } // <--- Diese Property muss vorhanden sein!

        public List<ShoppingGroup> Groups { get; set; } = new();

        public ShoppingList() { }

        public ShoppingList(string id, string name)
        {
            Id = id;
            Name = name;
            Groups = new List<ShoppingGroup>();
        }
    }

    public sealed class ShoppingGroup
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        public List<ShoppingItem> Items { get; set; } = new();

        public ShoppingGroup() { }

        public ShoppingGroup(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public sealed class ShoppingItem
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public ShoppingItem() { }

        public ShoppingItem(string id, string name, string quantity = "", string unit = "")
        {
            Id = id;
            Name = name;
            Quantity = quantity;
            Unit = unit;
        }
    }
}