
public class ExpertApiResponseBody
{
    public string articleId { get; set; }
    public string webcode { get; set; }
    public Price price { get; set; }
    public Bundle[] bundles { get; set; }
    public Warranties warranties { get; set; }
}

public class Price
{
    public Int64 storeStock { get; set; }
    public Int64 storeReservedStock { get; set; }
    public Int64 onlineStock { get; set; }
    public Int64 onlineReservedStock { get; set; }
    public string storeId { get; set; }
    public decimal nettoPrice { get; set; }
    public decimal bruttoPrice { get; set; }
    public decimal taxRate { get; set; }
    public string onlineButtonAction { get; set; }
    public string onlineAvailability { get; set; }
    public Int64 minOnlineDelivery { get; set; }
    public Int64 maxOnlineDelivery { get; set; }
    public string storeAvailability { get; set; }
    public Int64 minStoreDelivery { get; set; }
    public Int64 maxStoreDelivery { get; set; }
    public Shipmentarray[] shipmentArray { get; set; }
    public string storeButtonAction { get; set; }
    public bool itemOnDisplay { get; set; }
    public string itemOnDisplayDescription { get; set; }
    public Availableservice[] availableServices { get; set; }
    public object showStoreName { get; set; }
}

public class Shipmentarray
{
    public string shipmentType { get; set; }
    public decimal shipmentNettoPrice { get; set; }
    public decimal shipmentBruttoPrice { get; set; }
    public decimal shipmentTaxRate { get; set; }
    public string shipmentCurrency { get; set; }
    public bool hideVskText { get; set; }
}

public class Availableservice
{
    public string label { get; set; }
    public Price1 price { get; set; }
    public string icon { get; set; }
    public string wandaResourceId { get; set; }
    public object bankFinanceId { get; set; }
    public string id { get; set; }
    public object parentServiceId { get; set; }
    public string tooltipText { get; set; }
    public string expId { get; set; }
    public Childservice[] childService { get; set; }
}

public class Price1
{
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public decimal taxRate { get; set; }
    public string currency { get; set; }
}

public class Childservice
{
    public string label { get; set; }
    public Price2 price { get; set; }
    public string icon { get; set; }
    public string wandaResourceId { get; set; }
    public object bankFinanceId { get; set; }
    public string id { get; set; }
    public string parentServiceId { get; set; }
    public string tooltipText { get; set; }
    public string expId { get; set; }
    public object[] childService { get; set; }
}

public class Price2
{
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public decimal taxRate { get; set; }
    public string currency { get; set; }
}

public class Warranties
{
    public Warrantylist[] warrantyList { get; set; }
}

public class Warrantylist
{
    public string title { get; set; }
    public string typeName { get; set; }
    public string bruttoPrice { get; set; }
    public decimal taxRate { get; set; }
    public string currency { get; set; }
    public string description { get; set; }
    public string monthlyBruttoPrice { get; set; }
    public decimal monthlyTaxRate { get; set; }
    public Listoffiles listOfFiles { get; set; }
}

public class Listoffiles
{
    public List[] list { get; set; }
}

public class List
{
    public string fileName { get; set; }
    public string fileUrl { get; set; }
    public string fileLabel { get; set; }
}

public class Bundle
{
    public string id { get; set; }
    public Deliverycost deliveryCost { get; set; }
    public Promotedprice promotedPrice { get; set; }
    public Unpromotedprice unPromotedPrice { get; set; }
    public string onlineAvailability { get; set; }
    public Int64 minOnlineDelivery { get; set; }
    public Int64 maxOnlineDelivery { get; set; }
    public string onlineButtonAction { get; set; }
    public string validFrom { get; set; }
    public string validTo { get; set; }
    public Discount[] discounts { get; set; }
    public Properties properties { get; set; }
    public string client { get; set; }
    public Article[] articles { get; set; }
}

public class Deliverycost
{
    public Taxvalue[] taxValues { get; set; }
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public string currency { get; set; }
}

public class Taxvalue
{
    public decimal taxRate { get; set; }
    public decimal taxes { get; set; }
}

public class Promotedprice
{
    public Taxvalue1[] taxValues { get; set; }
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public string currency { get; set; }
}

public class Taxvalue1
{
    public decimal taxRate { get; set; }
    public decimal taxes { get; set; }
}

public class Unpromotedprice
{
    public Taxvalue2[] taxValues { get; set; }
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public string currency { get; set; }
}

public class Taxvalue2
{
    public decimal taxRate { get; set; }
    public decimal taxes { get; set; }
}

public class Properties
{
    public string title { get; set; }
    public string description { get; set; }
    public object teaser { get; set; }
}

public class Discount
{
    public Localizedpromotionproperties localizedPromotionProperties { get; set; }
    public string promotionId { get; set; }
    public Discount1 discount { get; set; }
}

public class Localizedpromotionproperties
{
    public Arraymap[] arrayMap { get; set; }
}

public class Arraymap
{
    public string key { get; set; }
    public Value value { get; set; }
}

public class Value
{
    public string eyeCatcher { get; set; }
    public string teaser { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string eyeCatcherTarget { get; set; }
    public object landingPageUrl { get; set; }
}

public class Discount1
{
    public Taxvalue3[] taxValues { get; set; }
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public string currency { get; set; }
}

public class Taxvalue3
{
    public decimal taxRate { get; set; }
    public decimal taxes { get; set; }
}

public class Article
{
    public string id { get; set; }
    public Exparticle expArticle { get; set; }
    public string link { get; set; }
    public string image { get; set; }
    public bool isWarranty { get; set; }
    public string name { get; set; }
    public string brand { get; set; }
    public Bundle1 bundle { get; set; }
    public Expprice expPrice { get; set; }
    public Warranty warranty { get; set; }
}

public class Exparticle
{
    public string[] descriptionList { get; set; }
    public string description { get; set; }
    public string headline { get; set; }
    public string formerPriceState { get; set; }
    public string primaryImage { get; set; }
    public string[] imageGallery { get; set; }
    public string[] documentList { get; set; }
    public string title { get; set; }
    public string primaryCategory { get; set; }
    public object crossSaleArticles { get; set; }
    public string[] additionalArticles { get; set; }
    public bool active { get; set; }
    public string scopeOfDelivery { get; set; }
    public Testresult[] testResults { get; set; }
    public decimal formerNettoPrice { get; set; }
    public decimal formerBruttoPrice { get; set; }
    public string formerCurrency { get; set; }
    public decimal formerTaxRate { get; set; }
    public string articleId { get; set; }
    public string slug { get; set; }
    public string nameSubscript { get; set; }
    public string[] featureLogos { get; set; }
    public string productType { get; set; }
    public string webcode { get; set; }
    public string InternationalArticleNumber { get; set; }
    public string brandId { get; set; }
    public string visibility { get; set; }
    public Technicaldata[] technicalData { get; set; }
}

public class Testresult
{
    public string image { get; set; }
    public string link { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public Int64 priority { get; set; }
}

public class Technicaldata
{
    public string key { get; set; }
    public Value1 value { get; set; }
}

public class Value1
{
    public string _class { get; set; }
    public string value { get; set; }
}

public class Bundle1
{
    public bool selectable { get; set; }
    public bool selected { get; set; }
    public string[] unsupportedArticles { get; set; }
    public string id { get; set; }
}

public class Expprice
{
    public decimal nettoPrice { get; set; }
    public decimal bruttoPrice { get; set; }
    public decimal taxRate { get; set; }
    public string onlineButtonAction { get; set; }
    public string onlineAvailability { get; set; }
    public Int64 minOnlineDelivery { get; set; }
    public Int64 maxOnlineDelivery { get; set; }
    public Shipmentarray1[] shipmentArray { get; set; }
    public string storeButtonAction { get; set; }
    public string storeAvailability { get; set; }
    public bool itemOnDisplay { get; set; }
    public string itemOnDisplayDescription { get; set; }
    public Availableservice1[] availableServices { get; set; }
}

public class Shipmentarray1
{
    public string shipmentType { get; set; }
    public decimal shipmentNettoPrice { get; set; }
    public decimal shipmentBruttoPrice { get; set; }
    public decimal? shipmentTaxRate { get; set; }
    public string shipmentCurrency { get; set; }
    public bool hideVskText { get; set; }
}

public class Availableservice1
{
    public string label { get; set; }
    public Price3 price { get; set; }
    public string icon { get; set; }
    public string wandaResourceId { get; set; }
    public object bankFinanceId { get; set; }
    public string id { get; set; }
    public string parentServiceId { get; set; }
    public string tooltipText { get; set; }
    public string expId { get; set; }
}

public class Price3
{
    public decimal net { get; set; }
    public decimal gross { get; set; }
    public decimal taxRate { get; set; }
    public string currency { get; set; }
}

public class Warranty
{
    public string title { get; set; }
    public string description { get; set; }
    public Listoffiles1 listOfFiles { get; set; }
}

public class Listoffiles1
{
    public List1[] list { get; set; }
}

public class List1
{
    public string fileName { get; set; }
    public string fileUrl { get; set; }
}
