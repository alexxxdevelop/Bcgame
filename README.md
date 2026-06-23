# Bcgame

A Windows desktop automation tool for the **BC.Game** sports book (`bcgame.st`). It drives one or more
[Dolphin{anty}](https://dolphin-anty.com/) anti-detect browser profiles through Selenium WebDriver and
places bets automatically according to a configurable per-profile strategy.

The app is a WPF (.NET Framework 4.8) front end: you add browser profiles, pick a strategy and its
parameters for each, then start/stop them individually. All activity is streamed to an in-app log.

> ⚠️ **Disclaimer.** This project automates real-money gambling and was built as a private freelance
> commission. It is published here for reference/educational purposes only. Automated betting may violate
> the terms of service of the target platform and may be restricted or illegal in your jurisdiction. Use it
> at your own risk; the authors accept no liability for any loss, account action, or other consequence.

## Features

- **Multi-profile** — runs many Dolphin{anty} browser profiles in parallel, each on its own strategy and settings.
- **Per-second scheduler** — a background timer ticks every second and advances every running profile.
- **Live in-app log** with color coding (wins green, losses red, errors red) and file logs on disk.
- **Test mode** — exercise the full flow without actually submitting bets.
- **Auto-reload** — optionally recycle the browser sessions on a configurable interval.
- **Persistent config** — all profiles and settings are saved to `config.json` next to the executable.

### Betting strategies

Each profile is assigned one of four strategies (the `type` field):

| # | Name (UI)      | Description |
|---|----------------|-------------|
| 0 | **Original**   | Quick bets on penalty-shootout markets (`under 6.5` / `over 5.5`) with randomized delays, an event-count cap, and periodic page reloads. |
| 1 | **Martingale** | Places bets and tracks each one in a win/loss history; the stake is **doubled after a loss**. Can auto-switch to *Penalty Handicap* once the balance reaches a threshold. Honors a maximum total-stake limit. |
| 2 | **Penalty Handicap** | FIFA penalty-handicap betting governed by a "lower odds" threshold, a stake amount, and a maximum total. |
| 3 | **FULL-LIMIT (Surebet)** | Arbitrage betting: finds an opposing market pair and stakes both sides so the win is equal regardless of outcome (see `PairMarketSure.GetEqualWinStakes`). Filtered by minimum profitability, with FULL/LIMIT modes and configurable delays. |

## Tech stack

- **.NET Framework 4.8**, **WPF** (custom-styled controls under `Resources/`)
- **Selenium WebDriver** 4.x + ChromeDriver
- **Dolphin{anty}** local automation API (`http://localhost:3001/v1.0/browser_profiles/{id}/start?automation=1`)
- **HtmlAgilityPack** for HTML parsing
- **Newtonsoft.Json** for config serialization

## Requirements

- Windows with the **.NET Framework 4.8** runtime
- **Visual Studio 2022** (the solution targets VS 17.13) to build
- **Dolphin{anty}** installed and running locally, with the browser profiles you want to automate already created
- **Google Chrome** (matching the bundled/`chromedriver.exe`) for the Selenium driver
- The private helper libraries **`Lib.dll`** and **`WebCh.dll`** — these are referenced from an external path
  (`..\..\..\Me\_Template\WebCh\bin\...`) and are **not** included in this repository. The project will not
  build without them.

## Build

```sh
git clone <this-repo>
cd Bcgame
# Open Bcgame.sln in Visual Studio 2022 and build (Debug or Release),
# or from a Developer prompt:
msbuild Bcgame.sln /p:Configuration=Release
```

NuGet packages (`HtmlAgilityPack`, `Newtonsoft.Json`, `Selenium.WebDriver`, `OpenQA.Selenium.Chrome.ChromeDriverExtensions`)
restore automatically. Make sure `Lib.dll` and `WebCh.dll` resolve, and that `chromedriver.exe` ends up in the output directory.

## Usage

1. Launch **Dolphin{anty}** and confirm its automation API is reachable at `localhost:3001`.
2. Run **Bcgame.exe**.
3. Click **Add profile**, enter the **Dolphin profile ID**, and choose a strategy from the dropdown.
4. Fill in the strategy parameters (stake, limits, odds, delays, etc.).
5. (Optional) Enable **Test mode** to dry-run without placing bets, and/or **Reload browsers** with an interval in minutes.
6. Press the **▶ / ■** button on a profile to start or stop it. The app opens that Dolphin profile, attaches
   Selenium, navigates to the relevant BC.Game sports page, and begins working.
7. Watch progress in the log panel; use **Reset** buttons to clear a profile's accumulated state/history.

Configuration and counters are written to `config.json` automatically; logs are written under a `logs/`
folder next to the executable (the **Clear log** action deletes them).

## Project structure

```
Bcgame.sln
Bcgame/
├─ App.xaml / App.xaml.cs        # WPF app entry, merged resource dictionaries
├─ MainWindow.xaml               # UI: profile list, per-strategy parameter grids, log
├─ MainWindow.xaml.cs            # Core logic: scheduler, Go(), PlaceBet, PenaltyHandicap, Surebet, QuickBet
├─ Models/_Models.cs             # Set, Profile, Bet, Market, PairMarket(Sure) data models
├─ Resources/                    # Custom WPF control styles (Button, TabControl, DataGrid, …)
├─ Images/                       # start.png / stop.png toggle icons
└─ icon.ico
```

## Notes & limitations

- The UI labels are in **Russian**; the app targets the Russian locale of the site (`bcgame.st/ru/...`).
- Selectors are tightly coupled to BC.Game's current DOM (including shadow-DOM widgets). Markup changes on
  the site will break navigation and bet placement and require selector updates.
- Depends on external, non-public DLLs (`Lib`, `WebCh`) and on a running Dolphin{anty} instance — it is not a
  self-contained, drop-in build.
