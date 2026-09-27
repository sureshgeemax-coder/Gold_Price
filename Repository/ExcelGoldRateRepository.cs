using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GoldPriceDashboard.Models;
using Serilog;

namespace GoldPriceDashboard.Repository
{
    public class ExcelGoldRateRepository : IExcelGoldRateRepository
    {
        private readonly string _excelPath;
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public ExcelGoldRateRepository()
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var excelRelative = System.Configuration.ConfigurationManager.AppSettings["ExcelFilePath"] ?? "App_Data\\GoldRates\\GoldRates.xlsx";
            _excelPath = Path.Combine(appPath, excelRelative);
            var dataDir = Path.GetDirectoryName(_excelPath);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            _connectionString = $"Excel File={_excelPath};";
            _logger = Log.ForContext<ExcelGoldRateRepository>();
        }

        public Task InitializeAsync()
        {
            if (!File.Exists(_excelPath))
            {
                _logger.Information("Excel file not found at {Path}. Creating new file.", _excelPath);
                CreateExcelFile();
            }
            else
            {
                _logger.Information("Excel file exists at {Path}.", _excelPath);
            }

            return Task.CompletedTask;
        }

        private void CreateExcelFile()
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("GoldRates");
            var headers = new[] { "Date", "Time", "Source", "GoldType", "Purity", "RatePerGram", "Currency", "SourceUrl", "LastUpdated", "Status", "ErrorMessage" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }
            ws.Columns().AdjustToContents();
            wb.SaveAs(_excelPath);
        }

        public async Task AppendRatesAsync(List<GoldRate> rates)
        {
            if (rates == null || rates.Count == 0) return;
            await Task.Run(() =>
            {
                try
                {
                    var wb = new XLWorkbook(_excelPath);
                    var ws = wb.Worksheet("GoldRates");
                    var lastRowUsed = ws.LastRowUsed();
                    var lastRow = lastRowUsed == null ? 1 : lastRowUsed.RowNumber() + 1;
                    foreach (var rate in rates)
                    {
                        ws.Cell(lastRow, 1).Value = rate.Date.ToString("dd-MM-yyyy");
                        ws.Cell(lastRow, 2).Value = rate.Time.ToString(@"hh\:mm\:ss");
                        ws.Cell(lastRow, 3).Value = rate.Source;
                        ws.Cell(lastRow, 4).Value = rate.GoldType;
                        ws.Cell(lastRow, 5).Value = rate.Purity;
                        ws.Cell(lastRow, 6).Value = (double)rate.RatePerGram;
                        ws.Cell(lastRow, 7).Value = rate.Currency;
                        ws.Cell(lastRow, 8).Value = rate.SourceUrl;
                        ws.Cell(lastRow, 9).Value = rate.LastUpdated.ToString("dd-MM-yyyy HH:mm:ss");
                        ws.Cell(lastRow, 10).Value = rate.Status;
                        ws.Cell(lastRow, 11).Value = rate.ErrorMessage ?? "";
                        lastRow++;
                    }
                    wb.Save();
                    _logger.Information("Appended {Count} records to Excel.", rates.Count);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to append rates to Excel.");
                    throw;
                }
            });
        }

        public async Task<List<GoldRate>> GetHistoricalRatesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? purity = null, string source = null)
        {
            return await Task.Run(() =>
            {
                var results = new List<GoldRate>();
                if (!File.Exists(_excelPath)) return results;
                try
                {
                    var wb = new XLWorkbook(_excelPath);
                    var ws = wb.Worksheet("GoldRates");
                    var rows = ws.RangeUsed().RowsUsed().Skip(1);
                    foreach (var row in rows)
                    {
                        try
                        {
                            var rate = new GoldRate
                            {
                                Date = DateTime.ParseExact(row.Cell(1).GetString(), "dd-MM-yyyy", null),
                                Time = TimeSpan.Parse(row.Cell(2).GetString()),
                                Source = row.Cell(3).GetString(),
                                GoldType = row.Cell(4).GetString(),
                                Purity = int.Parse(row.Cell(5).GetString()),
                                RatePerGram = decimal.Parse(row.Cell(6).GetString()),
                                Currency = row.Cell(7).GetString(),
                                SourceUrl = row.Cell(8).GetString(),
                                LastUpdated = DateTime.ParseExact(row.Cell(9).GetString(), "dd-MM-yyyy HH:mm:ss", null),
                                Status = row.Cell(10).GetString(),
                                ErrorMessage = row.Cell(11).GetString()
                            };
                            if (fromDate.HasValue && rate.Date < fromDate.Value.Date) continue;
                            if (toDate.HasValue && rate.Date > toDate.Value.Date) continue;
                            if (purity.HasValue && rate.Purity != purity.Value) continue;
                            if (!string.IsNullOrEmpty(source) && !rate.Source.Equals(source, StringComparison.OrdinalIgnoreCase)) continue;
                            results.Add(rate);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to read historical rates from Excel.");
                }
                return results.OrderBy(r => r.LastUpdated).ToList();
            });
        }

        public async Task<GoldRate> GetLatestRateAsync(string source, int purity)
        {
            var all = await GetHistoricalRatesAsync(source: source);
            return all.Where(r => r.Purity == purity && r.Status == "Success").OrderByDescending(r => r.LastUpdated).FirstOrDefault();
        }

        public async Task<bool> IsDuplicateAsync(string source, int purity, decimal rate, DateTime date, DateTime lastUpdated)
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(_excelPath)) return false;
                try
                {
                    var wb = new XLWorkbook(_excelPath);
                    var ws = wb.Worksheet("GoldRates");
                    var rows = ws.RangeUsed().RowsUsed().Skip(1);
                    foreach (var row in rows)
                    {
                        try
                        {
                            var rowSource = row.Cell(3).GetString();
                            var rowPurity = int.Parse(row.Cell(5).GetString());
                            var rowRate = decimal.Parse(row.Cell(6).GetString());
                            var rowDate = DateTime.ParseExact(row.Cell(1).GetString(), "dd-MM-yyyy", null);
                            var rowLastUpdated = DateTime.ParseExact(row.Cell(9).GetString(), "dd-MM-yyyy HH:mm:ss", null);
                            if (rowSource.Equals(source, StringComparison.OrdinalIgnoreCase) &&
                                rowPurity == purity &&
                                rowRate == rate &&
                                rowDate.Date == date.Date &&
                                rowLastUpdated == lastUpdated)
                            {
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to check duplicate in Excel.");
                }
                return false;
            });
        }
    }
}
