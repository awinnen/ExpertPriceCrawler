using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertPriceCrawler
{
    struct AddItemResponse
    {
        public string ItemId { get; set; }
        public ShoppingCart ShoppingCart { get; set; }
    }

    struct ShoppingCart
    {
        public LastAdded LastAdded { get; set; }
    }

    struct LastAdded
    {
        public Price Price { get; set; }
    }

    struct Price
    {
        public decimal Gross { get; set; }

        public CustomData CustomData { get; set; }
    }

    struct CustomData
    {
        public ExpPrice ExpPrice { get; set; }
    }

    struct ExpPrice
    {
        public string OnlineStore { get; set; }
    }
}
