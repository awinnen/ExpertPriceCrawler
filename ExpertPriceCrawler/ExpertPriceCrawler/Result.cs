namespace ExpertPriceCrawler
{

    public class ResultBase
    {
        public string BranchId { get; set; }
        public string BranchName { get; set; }
        public string Price { get; set; }
        public string Url { get; set; }
    }
    public class Result: ResultBase
    {
        public decimal PriceDecimal { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public bool IsExhibition { get; set; }
    }
}
