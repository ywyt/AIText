using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entitys.WP
{

    public partial class WooCommerceProduct
    {
        public int id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
        public Uri permalink { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_created_gmt { get; set; }
        public DateTime? date_modified { get; set; }
        public DateTime? date_modified_gmt { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public bool featured { get; set; }
        public string catalog_visibility { get; set; }
        public string description { get; set; }
        public string short_description { get; set; }
        public string sku { get; set; }
        public string price { get; set; }
        public string regular_price { get; set; }
        public string sale_price { get; set; }
        public object date_on_sale_from { get; set; }
        public object date_on_sale_from_gmt { get; set; }
        public object date_on_sale_to { get; set; }
        public object date_on_sale_to_gmt { get; set; }
        public bool on_sale { get; set; }
        public bool purchasable { get; set; }
        public long total_sales { get; set; }
        public bool @virtual { get; set; }
        public bool downloadable { get; set; }
        public object[] downloads { get; set; }
        public long download_limit { get; set; }
        public long download_expiry { get; set; }
        public string external_url { get; set; }
        public string button_text { get; set; }
        public string tax_status { get; set; }
        public string tax_class { get; set; }
        public bool manage_stock { get; set; }
        public object stock_quantity { get; set; }
        public string backorders { get; set; }
        public bool backorders_allowed { get; set; }
        public bool backordered { get; set; }
        public object low_stock_amount { get; set; }
        public bool sold_individually { get; set; }
        public string weight { get; set; }
        public Dimensions dimensions { get; set; }
        public bool shipping_required { get; set; }
        public bool shipping_taxable { get; set; }
        public string shipping_class { get; set; }
        public long shipping_class_id { get; set; }
        public bool reviews_allowed { get; set; }
        public string average_rating { get; set; }
        public long rating_count { get; set; }
        public object[] upsell_ids { get; set; }
        public object[] cross_sell_ids { get; set; }
        public long parent_id { get; set; }
        public string purchase_note { get; set; }
        public Category[] categories { get; set; }
        public Category[] tags { get; set; }
        public Image[] images { get; set; }
        public Attribute[] attributes { get; set; }
        public object[] default_attributes { get; set; }
        public long[] variations { get; set; }
        public object[] grouped_products { get; set; }
        public long menu_order { get; set; }
        public string price_html { get; set; }
        public long[] related_ids { get; set; }
        public MetaDatum[] meta_data { get; set; }
        public string stock_status { get; set; }
        public bool has_options { get; set; }
        public Links _links { get; set; }
    }

    public partial class Links
    {
        public Self[] self { get; set; }
        public Collection[] collection { get; set; }
    }

    public partial class Collection
    {
        public Uri href { get; set; }
    }

    public partial class Self
    {
        public Uri href { get; set; }
        public TargetHints targetHints { get; set; }
    }

    public partial class TargetHints
    {
        public string[] allow { get; set; }
    }

    public partial class Attribute
    {
        public long id { get; set; }
        public string name { get; set; }
        public long position { get; set; }
        public bool visible { get; set; }
        public bool variation { get; set; }
        public string[] options { get; set; }
    }

    public partial class Category
    {
        public long id { get; set; }
        public string name { get; set; }
        public string slug { get; set; }
    }

    public partial class Dimensions
    {
        public string length { get; set; }
        public string width { get; set; }
        public string height { get; set; }
    }

    public partial class Image
    {
        public long id { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_created_gmt { get; set; }
        public DateTime? date_modified { get; set; }
        public DateTime? date_modified_gmt { get; set; }
        public Uri src { get; set; }
        public string name { get; set; }
        public string alt { get; set; }
    }

    public partial class MetaDatum
    {
        public long id { get; set; }
        public string key { get; set; }
        public dynamic value { get; set; }
    }
}

