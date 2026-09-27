# Gold Price Rate Dashboard

An ASP.NET MVC 5 web application that retrieves and displays live gold rates from Mustafa Jewellery, Malabar Gold & Diamonds, GRT Jewellers, and Joyalukkas (Singapore) in a professional dashboard with historical tracking via Excel.

## Tech Stack

- **Framework:** ASP.NET MVC 5 / .NET Framework 4.8
- **Language:** C#
- **Frontend:** HTML5, CSS3, Bootstrap 5, jQuery, Chart.js
- **Data Storage:** Excel (ClosedXML)
- **Logging:** Serilog
- **HTTP:** HttpClient

## Prerequisites

- Visual Studio 2022
- .NET Framework 4.8 Developer Pack
- NuGet Package Restore enabled

## Setup

1. Open `GoldPriceDashboard.sln` in Visual Studio 2022.
2. Restore NuGet packages (right-click solution → Restore NuGet Packages).
3. Ensure the `App_Data\GoldRates` folder exists (it will be auto-created).
4. Build the solution (Ctrl+Shift+B).

## Configuration

Edit `Web.config` `<appSettings>` to configure URLs and refresh intervals:

```xml
<add key="MustafaGoldUrl" value="https://mustafajewellery.com/" />
<add key="MalabarGoldUrl" value="https://www.malabargoldanddiamonds.com/ae/stores/singapore" />
<add key="GRTGoldUrl" value="https://www.grtjewels.com/asia/" />
<add key="JoyalukkasGoldUrl" value="https://www.joyalukkas.com/graphql" />
<add key="GoldRateRefreshMinutes" value="30" />
<add key="RequestTimeoutSeconds" value="30" />
<add key="ExcelFilePath" value="App_Data\GoldRates\GoldRates.xlsx" />
```

## Running

1. Open `GoldPriceDashboard.sln` in Visual Studio 2022.
2. Right-click `GoldPriceDashboard` in Solution Explorer and select **Set as Startup Project**.
3. Open **Project > GoldPriceDashboard Properties > Web** and confirm **Specific Page** is set to `GoldRate/Index`. Do not select or browse directly to `Views/GoldRate/Index.cshtml`; MVC views must run through their controller route.
4. Press F5 to run with the debugger or Ctrl+F5 to run without it.
5. If the browser does not open automatically, browse to `http://localhost:57103/` or `http://localhost:57103/GoldRate/Index` while IIS Express is running.

The Web API routes are registered separately from MVC routes at application startup; `/api/goldrates` and `/api/goldrates/history` are available alongside the dashboard.

The dashboard will:
- Fetch latest gold rates from all four sources
- Display indicative 22K/916 and 24K/999 rates in SGD/gram with source links and update times
- Provide dedicated shop pages at `/GoldRate/Shop/malabar`, `/GoldRate/Shop/mustafa`, `/GoldRate/Shop/grt`, and `/GoldRate/Shop/joyalukkas`
- Compare the highest and lowest available published rates and show the observed rate spread
- Plot actual Excel observations for 7 days, 1 month, 3 months, 6 months, or 1 year; missing values are not interpolated
- Convert SGD prices to INR, USD, MYR, AED, GBP, or EUR using dated reference exchange rates; SGD remains the primary price display
- Identify historical fallback values as "Last available rate" if a live source is unavailable
- Save all successful rates to `App_Data\GoldRates\GoldRates.xlsx`

Currency equivalents use the public Frankfurter reference-rate service and are estimates only. They are not shop quotations. Published shop rates are shown as received; the application does not add GST or independently verify each shop's GST treatment. The dashboard includes a purchase-price disclaimer on the main and shop-detail pages.

If the old Razor inheritance error remains after this change, stop IIS Express and delete the temporary ASP.NET files under `%LOCALAPPDATA%\Temp\Temporary ASP.NET Files`, then rebuild and run again. Do not delete the project's `App_Data` folder.

## API Endpoints

- `GET /api/goldrates` - Returns latest rates JSON
- `GET /api/goldrates/history?fromDate=&toDate=&purity=&source=` - Returns historical rates

## Project Structure

```
GoldPriceDashboard/
├── App_Data/GoldRates/          # Excel historical data
├── App_Start/RouteConfig.cs     # MVC routing
├── Controllers/
│   ├── GoldRateController.cs    # Dashboard & refresh
│   └── GoldRateApiController.cs # REST API
├── Models/
│   ├── GoldRate.cs
│   ├── GoldRateResult.cs
│   └── GoldDashboardViewModel.cs
├── Repository/
│   ├── IExcelGoldRateRepository.cs
│   └── ExcelGoldRateRepository.cs
├── Services/
│   ├── IGoldRateService.cs
│   ├── IMustafaGoldRateService.cs
│   ├── MustafaGoldRateService.cs
│   ├── IMalabarGoldRateService.cs
│   ├── MalabarGoldRateService.cs
│   ├── IGRTGoldRateService.cs
│   ├── GRTGoldRateService.cs
│   ├── IJoyalukkasGoldRateService.cs
│   ├── JoyalukkasGoldRateService.cs
│   └── GoldRateService.cs
├── Views/GoldRate/Index.cshtml
├── Content/gold-dashboard.css
├── Scripts/gold-dashboard.js
├── Logs/                        # Serilog log files
├── Web.config
├── packages.config
└── GoldPriceDashboard.csproj
```

## Notes

- Gold rates are scraped live from the configured URLs.
- The app respects source websites' terms and does not send excessive requests.
- Duplicate records are prevented based on source, purity, rate, date, and last-updated timestamp.
- If a source fails, the dashboard continues displaying the other source.
