using System;

namespace GoldPriceDashboard.Models
{
    public class GoldRateResult
    {
        public string Source { get; set; }
        public decimal? Rate916 { get; set; }
        public decimal? Rate999 { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Currency { get; set; }
        public string SourceUrl { get; set; }
    }
}
