using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Square
{
    public class SquareSale
    {
        public DateTime TransactionTime { get; set; }
        public float TotalCollected { get; set; }
        public float NetTotal { get; set; }
        public float GrossSales { get; set; }
        public float Tax { get; set; }
        public float Tip { get; set; }
        public float ChargeFee { get; set; }
        public List<SquareItemization> Itemizations { get; set; }

        public SquareSale(dynamic sale)
        {
            TransactionTime = SquareService.FromSquareDateString(sale.created_at.ToString());
            int total;
            int.TryParse(sale.total_collected_money.amount.ToString(), out total);
            TotalCollected = total / 100f;
            int netTotal;
            int.TryParse(sale.net_total_money.amount.ToString(), out netTotal);
            NetTotal = netTotal / 100f;
            int gross;
            int.TryParse(sale.gross_sales_money.amount.ToString(), out gross);
            GrossSales = gross / 100f;
            int tax;
            int.TryParse(sale.tax_money.amount.ToString(), out tax);
            Tax = tax / 100f;
            int tip;
            int.TryParse(sale.tip_money.amount.ToString(), out tip);
            Tip = tip / 100f;
            int fee;
            int.TryParse(sale.processing_fee_money.amount.ToString(), out fee);
            ChargeFee = -fee / 100f;

            Itemizations = new List<SquareItemization>();
            foreach (dynamic item in sale.itemizations)
            {
                try
                {
                    Itemizations.Add(new SquareItemization(item));
                }
                catch(Exception e)
                {
                    int debug = 1;
                }
            }
        }
    }

    public class SquareItemization
    {
        public string Name { get; set; }
        public string Notes { get; set; }
        public int Quantity { get; set; }
        public float SingleItemPrice { get; set; }
        public float Tax { get; set; }
        public List<ItemizationModifier> Modifiers { get; set; }

        public float TotalPrice
        {
            get
            {
                float modPrices = 0;
                if(Modifiers != null && Modifiers.Count > 0)
                    modPrices = Modifiers.Sum(x => x.Price);
                return SingleItemPrice * Quantity + modPrices;
            }
        }
        public float NetTotal
        {
            get
            {
                return TotalPrice - Tax;
            }
        }

        public SquareItemization(dynamic itemization)
        {
            Name = WebUtility.HtmlDecode(itemization.name.ToString());
            if(itemization.notes != null)
                Notes = WebUtility.HtmlDecode(itemization.notes.ToString());
            float qtyFloat;
            float.TryParse(itemization.quantity.ToString(), out qtyFloat);
            Quantity = (int)qtyFloat;
            int itemPrice;
            int.TryParse(itemization.single_quantity_money.amount.ToString(), out itemPrice);
            SingleItemPrice = itemPrice / 100f;

            if (itemization.taxes != null && itemization.taxes.Count > 0)
            {
                int taxFloat;
                int.TryParse(itemization.taxes[0].applied_money.amount.ToString(), out taxFloat);
                Tax = taxFloat / 100f;
            }
            else
            {
                Tax = 0;
            }

            Modifiers = new List<ItemizationModifier>();
            foreach (dynamic mod in itemization.modifiers)
            {
                Modifiers.Add(new ItemizationModifier(mod));
            }
        }
    }

    public class ItemizationModifier
    {
        public string Name { get; set; }
        public float Price { get; set; }

        public ItemizationModifier(dynamic modifier)
        {
            Name = WebUtility.HtmlDecode(modifier.name.ToString());
            int modPrice;
            int.TryParse(modifier.applied_money.amount.ToString(), out modPrice);
            Price = modPrice / 100f;
        }
    }
}
