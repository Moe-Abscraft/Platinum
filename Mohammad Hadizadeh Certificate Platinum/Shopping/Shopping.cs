using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Crestron.SimplSharpPro.UI;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class Shopping
    {
        public static List<Retail> ShoppingItems;
        public static List<Retail> ShoppingCart;
        public static List<Retail> GetShoppingList(int page)
        {
            if (ShoppingItems == null) return null;
            var startPos = page * 5 - 4;
            var newListStart = ShoppingItems.Skip(startPos - 1).ToList();
            var newList = newListStart.Count >= 5 ? newListStart.Take(5).ToList() : newListStart;
            return newList;
        }
        
        public static List<Retail> GetShoppingList(int page, string vendor)
        {
            if (ShoppingItems == null) return null;
            var startPos = page * 5 - 4;
            var newListStart = ShoppingItems.Where(item => item.VENDOR == vendor).Skip(startPos - 1).ToList();
            var newList = newListStart.Count >= 5 ? newListStart.Take(5).ToList() : newListStart;
            return newList;
        }

        public static List<Retail> AddToCart(int item)
        {
            if (ShoppingCart == null) ShoppingCart = new List<Retail>();
            var shoppingItem = ShoppingItems[item];
            ShoppingCart.Add(shoppingItem);
            RentalService.TotalCharge += float.Parse(shoppingItem.PRICE);
            return ShoppingCart;
        }
        
        public static List<Retail> AddToCart(int itemIndex, string vendor)
        {
            if (ShoppingCart == null) ShoppingCart = new List<Retail>();
            var shoppingItem = ShoppingItems.Where(item => item.VENDOR == vendor).ToList()[itemIndex];
            ShoppingCart.Add(shoppingItem);
            RentalService.TotalCharge += float.Parse(shoppingItem.PRICE);
            return ShoppingCart;
        }
        
        public static List<Retail> RemoveFromCart(int item)
        {
            if (ShoppingCart == null) return null;
            var shoppingItem = ShoppingCart[item];
            ShoppingCart.RemoveAt(item);
            RentalService.TotalCharge -= float.Parse(shoppingItem.PRICE);
            return ShoppingCart;
        }
        
        public static List<Retail> GetCartList(int page)
        {
            if (ShoppingCart == null) return null;
            var startPos = page * 5 - 4;
            var newListStart = ShoppingCart.Skip(startPos - 1).ToList();
            var newList = newListStart.Count >= 5 ? newListStart.Take(5).ToList() : newListStart;
            return newList;
        }
    }
}