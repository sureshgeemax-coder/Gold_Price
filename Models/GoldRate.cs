using System;

namespace GoldPriceDashboard.Models
{
    public class GoldRate
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string Source { get; set; }
        public string GoldType { get; set; }
        public int Purity { get; set; }
        public decimal RatePerGram { get; set; }
        public string Currency { get; set; }
        public string SourceUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
