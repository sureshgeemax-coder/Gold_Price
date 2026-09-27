using System;
using System.Collections.Generic;

namespace GoldPriceDashboard.Models
{
    public class GoldDashboardViewModel
    {
        public decimal? MustafaRate916 { get; set; }
        public decimal? MustafaRate999 { get; set; }
        public DateTime? MustafaUpdated { get; set; }
        public string MustafaStatus { get; set; }
        public string MustafaErrorMessage { get; set; }

        public decimal? MalabarRate916 { get; set; }
        public decimal? MalabarRate999 { get; set; }
        public DateTime? MalabarUpdated { get; set; }
        public string MalabarStatus { get; set; }
        public string MalabarErrorMessage { get; set; }

        public decimal? GRTGoldRate916 { get; set; }
        public decimal? GRTGoldRate999 { get; set; }
        public DateTime? GRTGoldUpdated { get; set; }
        public string GRTGoldStatus { get; set; }
        public string GRTGoldErrorMessage { get; set; }

        public decimal? JoyalukkasRate916 { get; set; }
        public decimal? JoyalukkasRate999 { get; set; }
        public DateTime? JoyalukkasUpdated { get; set; }
        public string JoyalukkasStatus { get; set; }
        public string JoyalukkasErrorMessage { get; set; }

        public decimal? Difference916 { get; set; }
        public decimal? Difference999 { get; set; }
        public decimal? PercentageDifference916 { get; set; }
        public decimal? PercentageDifference999 { get; set; }

        public string HigherRate916 { get; set; }
        public string LowerRate916 { get; set; }
        public string HigherRate999 { get; set; }
        public string LowerRate999 { get; set; }

        public decimal? MustafaPrevious916 { get; set; }
        public decimal? MustafaPrevious999 { get; set; }
        public decimal? MalabarPrevious916 { get; set; }
        public decimal? MalabarPrevious999 { get; set; }
        public string MustafaMovement916 { get; set; }
        public string MustafaMovement999 { get; set; }
        public string MalabarMovement916 { get; set; }
        public string MalabarMovement999 { get; set; }
        public string GRTGoldMovement916 { get; set; }
        public string GRTGoldMovement999 { get; set; }
        public string JoyalukkasMovement916 { get; set; }
        public string JoyalukkasMovement999 { get; set; }

        public List<GoldRate> HistoricalRates { get; set; } = new List<GoldRate>();
        public DateTime? LastRefresh { get; set; }
        public string ChartFilter { get; set; } = "7";
    }
}
