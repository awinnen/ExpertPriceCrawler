namespace ExpertPriceCrawler
{
    public struct Result
    {
        public string BranchId { get; set; }
        public string BranchName { get; set; }
        public string Price { get; set; }
        public string Url { get; set; }
        public decimal PriceDecimal { get; set; }
    }
}
