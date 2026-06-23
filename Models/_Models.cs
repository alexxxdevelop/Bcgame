using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Lib;
using OpenQA.Selenium;

namespace Bcgame
{
    public class Set
    {
        public double period = 5;
        public List<Profile> profiles = new List<Profile>();
        public bool isReload;
        public bool isTest;
        public int reloadMins = 60;
    }

    public class Profile
    {
        public string id { get; set; } = "";
        public int type { get; set; } = 0;

        public int randomSecs1 { get; set; } = 5;
        public int randomSecs2 { get; set; } = 15;
        public int market { get; set; } = 0;
        public int eventsCount { get; set; } = 1300;
        public int eventsCurrentCount { get; set; } = 0;
        public List<string> hrefs { get; set; } = new List<string>();
        public bool started { get; set; } = false;
        public string comment { get; set; } = "";
        public decimal amount1 { get; set; } = 1;
        public string amount1S { get { return amount1.ToString(true); } set { amount1 = Helper.DecimalParse(value); } }
        public int reload { get; set; } = 5;

        public int betsCount { get; set; } = 3;
        public int marketM { get; set; } = 0;
        public decimal amount { get; set; } = 1;
        public decimal limit { get; set; } = 1000;
        public bool miss2odds { get; set; } = true;
        public decimal sum { get; set; }
        public List<Bet> bets = new List<Bet>();
        public string amountS { get { return amount.ToString(true); } set { amount = Helper.DecimalParse(value); } }
        public string limitS { get { return limit.ToString(true); } set { limit = Helper.DecimalParse(value); } }
        public string sumS { get { return sum.ToString(true); } set { } }
        public bool toggle { get; set; } = false;
        public decimal toggleBalance { get; set; } = 1000;
        public string toggleBalanceS { get { return toggleBalance.ToString(true); } set { toggleBalance = Helper.DecimalParse(value); } }

        public List<string> fifas { get; set; } = new List<string>();
        public double downOdd { get; set; } = 1.81;
        public decimal limit1 { get; set; } = 1000;
        public string limitS1 { get { return limit1.ToString(true); } set { limit1 = Helper.DecimalParse(value); } }
        public decimal sum1 { get; set; }
        public string sumS1 { get { return sum1.ToString(true); } set { } }
        public decimal amount2 { get; set; } = 1;
        public string amount2S { get { return amount2.ToString(true); } set { amount2 = Helper.DecimalParse(value); } }
        public decimal balance { get; set; }
        public string balanceS { get { return balance.ToString(true); } set { } }

        public decimal limit_surebet { get; set; } = 1000;
        public string limitS_surebet { get { return limit_surebet.ToString(true); } set { limit_surebet = Helper.DecimalParse(value); } }
        public decimal sum_surebet { get; set; }
        public string sumS_surebet { get { return sum_surebet.ToString(true); } set { } }
        public int delay_surebet { get; set; } = 30;
        public int delay_between_bets_surebet { get; set; } = 90;
        public decimal amount_surebet { get; set; } = 500;
        public string amountS_surebet { get { return amount_surebet.ToString(true); } set { amount_surebet = Helper.DecimalParse(value); } }
        public string link_surebet { get; set; } = "https://bcgame.st/ru/sports?bt-path=%2Fecricket-x-battle-bats-322";
        public int type_surebet { get; set; } = 0;
        public bool fifa_surebet { get; set; }
        public int index_surebet { get; set; } = 3;
        public double minprofit_surebet { get; set; } = -5;

        public Visibility grid1 { get { return type == 0 ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility grid2 { get { return type == 1 ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility grid3 { get { return type == 2 ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility grid_surebet { get { return type == 3 ? Visibility.Visible : Visibility.Collapsed; } }
    }

    public class Bet
    {
        public string name;
        public decimal amount;
        public Result result;
        public string id;
    }

    public enum Result { none, win, lost }

    public class Market
    {
        public string group;
        public string name;
        public double odd;
        public IWebElement button;
    }

    public class PairMarket
    {
        public List<Market> markets;
        public double dif { get { return Math.Abs(markets[0].odd - markets[1].odd); } }
    }

    public class PairMarketSure
    {
        public Market market1;
        public Market market2;

        public (double stake1, double stake2, double profit) GetEqualWinStakes(double total)
        {
            if (total <= 0) total = 100;
            double k1 = market1.odd;
            double k2 = market2.odd;

            if (k1 <= 0 || k2 <= 0) return (0, 0, 0);

            double stake1 = total / (1 + (k1 / k2));
            double stake2 = total - stake1;
            double win = stake1 * k1; // или stake2 * k2
            double profit = (win / total - 1) * 100;

            return (Math.Floor(stake1), Math.Floor(stake2), Math.Round(profit, 2));
        }
    }
}
