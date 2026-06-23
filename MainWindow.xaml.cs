using Lib;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Bcgame
{
    public partial class MainWindow : Window
    {
        public static bool dev;
        public Set config = new Set();
        List<WebCh> wchs = new List<WebCh>();
        Stopwatch sw = Stopwatch.StartNew();
        Stopwatch sw1;
        bool wbusy = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var wch in wchs) wch.Dispose(); wchs.Clear();
        }

        #region События элементов
        private void clearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.Delete(Helper.PathExe + "logs", true);
                log.Document.Blocks.Clear();
            }
            catch { }
        }

        private void eventsCurrentCountReset_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            string id = b.Tag.ToString();
            var profile = config.profiles.First(q => q.id == id);
            profile.eventsCurrentCount = 0;
            profile.hrefs.Clear();
            SaveConfig();
        }

        private void deleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            string id = b.Tag.ToString();
            config.profiles.RemoveAll(q => q.id == id);
            SaveConfig();
        }

        private void addProfile_Click(object sender, RoutedEventArgs e)
        {
            config.profiles.Add(new Profile());
            SaveConfig();
            Render();
        }

        private void startProfile_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            string id = b.Tag.ToString();
            var profile = config.profiles.First(q => q.id == id);
            profile.started = !profile.started;
            if (!profile.started)
            {
                var w = wchs.FirstOrDefault(q => q.name == profile.id);
                if (w != null) w.Dispose();
                wchs.RemoveAll(q => q.name == profile.id);
            }
            SaveConfig();
        }

        private async void type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var source = (e.Source as ComboBox).Text;
            if (string.IsNullOrEmpty(source)) return;
            await Task.Delay(100);
            var _sender = (sender as ComboBox).Text;
            if (source != _sender) Render();
        }

        private void betsReset_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            string id = b.Tag.ToString();
            var profile = config.profiles.First(q => q.id == id);
            profile.bets.Clear();
            profile.hrefs.Clear();
            profile.sum = 0;
            SaveConfig();
        }

        private void penaltyReset_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            string id = b.Tag.ToString();
            var profile = config.profiles.First(q => q.id == id);
            profile.hrefs.Clear();
            profile.sum1 = 0;
            SaveConfig();
        }

        private void isReload_Checked(object sender, RoutedEventArgs e)
        {
            config.isReload = isReload.IsChecked.Value;
            SaveConfig(false);
        }

        private void surebetReset_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            string id = b.Tag.ToString();
            var profile = config.profiles.First(q => q.id == id);
            profile.hrefs.Clear();
            profile.sum_surebet = 0;
            SaveConfig();
        }

        private void isTest_Checked(object sender, RoutedEventArgs e)
        {
            config.isTest = isTest.IsChecked.Value;
            SaveConfig(false);
        }

        private void reloadMins_TextChanged(object sender, TextChangedEventArgs e)
        {
            config.reloadMins = Helper.IntParse(reloadMins.Text);
            SaveConfig(false);
        }
        #endregion

        #region Таймеры
        Timer timer;
        bool busy = false;

        public async void Start()
        {
            try
            {
                timer = new Timer(DoWork, null, 0, (long)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
        }

        public void Stop()
        {
            try
            {
                if (timer != null) timer.Dispose();
                //foreach (var wch in wchs) wch.Dispose(); wchs.Clear();
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
        }

        async void DoWork(object state)
        {
            if (busy) return;
            busy = true;
            await Go();
            busy = false;
        }
        #endregion

        #region Разное
        void LoadConfig()
        {
            dev = Directory.Exists(@"d:\Projects\Me\_\");
            config = Deserialize<Set>("config");
            if (config == null) config = new Set();

            foreach (var profile in config.profiles) profile.started = false;
            profiles.ItemsSource = config.profiles;

            isReload.IsChecked = config.isReload;
            isTest.IsChecked = config.isTest;
            if (config.reloadMins == 0) config.reloadMins = 60;
            reloadMins.Text = config.reloadMins.ToString();
        }

        bool SaveConfig(bool render = true)
        {
            if (render) Render();
            Serialize(config, "config");
            return true;
        }

        void Render()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                profiles.Items.Refresh();
            }));
        }

        public async void Serialize(object o, string fileName)
        {
            while (true)
            {
                try
                {
                    string s = JsonConvert.SerializeObject(o);
                    File.WriteAllText(string.Format("{0}{1}.json", Helper.PathExe, fileName), s, Encoding.UTF8);
                    break;
                }
                catch { await Task.Delay(100); }
            }
        }

        T Deserialize<T>(string fileName)
        {
            T r = default(T);

            string path = string.Format("{0}{1}.json", Helper.PathExe, fileName);
            if (File.Exists(path))
            {
                string s = File.ReadAllText(path, Encoding.UTF8);
                r = JsonConvert.DeserializeObject<T>(s);
            }

            return r;
        }

        public void Log(object s, Brush b = null)
        {
            s = Helper.Log(s);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (b == null) b = Brushes.Gray;
                string text = new TextRange(log.Document.ContentStart, log.Document.ContentEnd).Text;
                if (text.Split('\n').Length > 1000) log.Document.Blocks.Clear();
                TextRange tr = new TextRange(log.Document.ContentEnd, log.Document.ContentEnd);
                tr.Text = s + "\r\n";
                if (string.IsNullOrEmpty(text)) tr.Text += "\r\n";
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, b);
                log.ScrollToEnd();
            }));
        }
        #endregion

        #region go
        async Task Go()
        {
            SaveConfig(false);
            if (wbusy) return;
            wbusy = true;

            if (config.isReload && sw.Elapsed.TotalMinutes > config.reloadMins)
            {
                sw.Restart();
                foreach (var w in wchs) w.Dispose();
                wchs.Clear();
            }

            List<Task> tasks = new List<Task>();
            foreach (var profile in config.profiles.Where(q => q.started).ToList())
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        #region Init
                        var w = wchs.FirstOrDefault(q => q.name == profile.id);
                        if (w == null)
                        {
                            if (profile.type == 0 && profile.eventsCurrentCount >= profile.eventsCount)
                            {
                                Log($"{profile.id}: Достигнут лимит количества событий");
                                profile.started = false;
                                SaveConfig();
                                return;
                            }

                            var web = new Web(delay: true);
                            while (true) { await web.Go($"http://localhost:3001/v1.0/browser_profiles/{profile.id}/start?automation=1"); if (web.j != null) break; }
                            if (web.j != null)
                            {
                                if (web.j["error"] != null) { Log($"{profile.id}: {web.j["error"]}", Brushes.Red); return; }
                                else
                                {
                                    string port = web.j["automation"]["port"].ToString();
                                    w = await WebCh.Create(debuggerAddress: "127.0.0.1:" + port);
                                    w.name = profile.id;
                                    wchs.Add(w);

                                    if (profile.type > 0)
                                    {
                                        try
                                        {
                                            w.driver.SwitchTo().Window(w.driver.WindowHandles[0]);
                                            if (w.driver.WindowHandles.Count > 1)
                                            {
                                                for (int i = 0; i < w.driver.WindowHandles.Count; i++)
                                                {
                                                    w.driver.SwitchTo().Window(w.driver.WindowHandles[i]);
                                                    w.driver.Close();
                                                }
                                                w.driver.SwitchTo().Window(w.driver.WindowHandles[0]);
                                            }
                                            w.driver.SwitchTo().NewWindow(WindowType.Tab);
                                            if (profile.type == 1) await w.Go("https://bcgame.st/ru/sports?bt-path=%2Fbets");
                                            else if (profile.type == 2) { await w.Go("https://bcgame.st/ru/sports?bt-path=%2Ffifa-300"); }
                                            else if (profile.type == 3 && profile.fifa_surebet) { await w.Go("https://bcgame.st/ru/sports?bt-path=%2Ffifa-300"); }
                                            else if (profile.type == 3 && !profile.fifa_surebet) w.driver.Close();
                                            w.driver.SwitchTo().Window(w.driver.WindowHandles[0]);
                                            if (profile.type == 3) await w.Go(profile.link_surebet);
                                            else await w.Go("https://bcgame.st/ru/sports?bt-path=%2Flive%2Fpenalty-shootout-307");
                                        }
                                        catch { }
                                    }
                                    else await w.Go("https://bcgame.st/ru/sports?bt-path=%2Flive%2Fpenalty-shootout-307");
                                    Log($"{profile.id}: Открыт профиль на порту {port}");
                                    if (profile.type == 0)
                                    {
                                        await QuickBet(profile.amount1, w);
                                        sw1 = Stopwatch.StartNew();
                                    }
                                }
                            }
                        }
                        #endregion

                        await w.Wait(100);
                        var shadowRoot = w.GetShadow("//div[@id='bt-inner-page']");

                        #region История ставок
                        if (profile.type == 1 && profile.bets.Where(q => q.result == Result.none).Count() >= profile.betsCount)
                        {
                            while (true)
                            {
                                var pillButton = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='pillButton']")).FirstOrDefault(q => q.Text == "Все");
                                if (pillButton == null) { w.driver.SwitchTo().Window(w.driver.WindowHandles[1]); await Task.Delay(3000); }
                                else { pillButton.Click(); await Task.Delay(1000); break; }
                            }
                            var bets = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='bet']"));
                            if (bets != null)
                            {
                                for (int i = 0; i < profile.bets.Count * 2; i++)
                                {
                                    try
                                    {
                                        var bet = bets[i];
                                        var names = bet.FindElements(By.XPath(".//div[@data-editor-id='betSelection']/div[3]/div[1]/div[1]/span"));
                                        string name = $"{names[0].Text} - {names[2].Text}";
                                        var pbet = profile.bets.FirstOrDefault(q => q.name == name);
                                        if (pbet != null)
                                        {
                                            var betEventId = bet.FindElement(By.XPath(".//div[@data-editor-id='betEventId']/span")).Text;
                                            if (string.IsNullOrEmpty(pbet.id)) pbet.id = betEventId;
                                            else if (pbet.id != betEventId) continue;
                                            IWebElement result = null;
                                            try { result = bet.FindElement(By.XPath(".//span[@data-editor-id='betsLostStatus']")); } catch { }
                                            if (result != null) { pbet.result = Result.lost; Log($"{profile.id}: Проиграна ставка {pbet.name}", Brushes.IndianRed); }
                                            else
                                            {
                                                try { result = bet.FindElement(By.XPath(".//span[@data-editor-id='betsWonStatus']")); } catch { }
                                                if (result != null) { profile.bets.Remove(pbet); Log($"{profile.id}: Выиграна ставка {pbet.name}", Brushes.Green); }
                                            }
                                            //Render();
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                        #region Новые ставки
                        else
                        {
                            if (profile.type == 0 && sw1.Elapsed.TotalMinutes > profile.reload)
                            {
                                await w.Go("https://bcgame.st/ru/sports?bt-path=%2Flive%2Fpenalty-shootout-307");
                                sw1.Restart();
                            }
                            if (profile.type == 2 || profile.type == 3)
                            {
                                await w.Wait(10000); shadowRoot = w.GetShadow("//div[@id='bt-inner-page']");
                                GetBalance(w, profile);
                            }

                            var eventCards = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='eventCard']"));
                            if (profile.type == 3) eventCards = shadowRoot.FindElements(By.XPath(".//div[@data-cy='sport-page']/div[3]/div[3]//div[@data-editor-id='eventCard']"));
                            if (eventCards == null || eventCards.Count == 0)
                            {
                                w.driver.SwitchTo().Window(w.driver.WindowHandles[0]);
                                await Task.Delay(2000);
                                eventCards = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='eventCard']"));
                            }
                            if (eventCards != null)
                            {
                                int surebetIndex = -1;
                                foreach (var eventCard in eventCards)
                                {
                                    try
                                    {
                                        var eventCardContent = eventCard.FindElement(By.XPath(".//a[@data-editor-id='eventCardContent']"));
                                        if ((profile.type == 3) && !eventCardContent.Text.Contains("Сегодня")) continue;
                                        surebetIndex++;
                                        string href = eventCardContent.GetAttribute("href");
                                        if (!profile.hrefs.Contains(href))
                                        {
                                            var simpleMarketTitle = eventCard.FindElement(By.XPath(".//div[@data-editor-id='simpleMarketTitle']"));
                                            if (!string.IsNullOrEmpty(simpleMarketTitle.Text))
                                            {
                                                var names = eventCard.FindElements(By.XPath(".//a[@data-editor-id='eventCardContent']/div[2]/div[1]/div"));
                                                string name = $"{names[0].Text} - {names[1].Text}";
                                                if (profile.type == 0) await PlaceBet(profile, w, shadowRoot, eventCard, href, name);
                                                else if (profile.type == 1 && profile.bets.Where(q => q.result == Result.none).Count() < profile.betsCount)
                                                {
                                                    if (profile.toggle)
                                                    {
                                                        decimal balance = Helper.DecimalParse(w.SelectSingleNode("//div[@class='font-extrabold flex items-center w-0 flex-auto truncate']").InnerText);
                                                        if (balance >= profile.toggleBalance)
                                                        {
                                                            profile.type = 2;
                                                            profile.sum1 = profile.sum;
                                                            SaveConfig();
                                                            return;
                                                        }
                                                    }
                                                    decimal amount = profile.amount;
                                                    var pbet = profile.bets.FirstOrDefault(q => q.result == Result.lost);
                                                    if (pbet != null) amount = pbet.amount * 2;
                                                    if (profile.sum > profile.limit) { Log($"{profile.id}: Достигнута максимальная сумма"); profile.started = false; SaveConfig(); return; }
                                                    await PlaceBet(profile, w, shadowRoot, eventCard, href, name, amount, pbet);
                                                }
                                                else if (profile.type == 2)
                                                {
                                                    if (profile.sum1 >= profile.limit1) { Log($"{profile.id}: Достигнута максимальная сумма"); profile.started = false; SaveConfig(); return; }
                                                    await PenaltyHandicap(profile, w, href, name, eventCard);
                                                }
                                                else if (profile.type == 3 && surebetIndex == profile.index_surebet - 1)
                                                {
                                                    if (!config.isTest && profile.type_surebet == 0 && profile.balance < 50) continue;
                                                    if (!config.isTest && profile.type_surebet == 1 && profile.balance < profile.amount_surebet + 2) continue;
                                                    if (profile.sum_surebet >= profile.limit_surebet) { Log($"{profile.id}: Достигнута максимальная сумма"); profile.started = false; SaveConfig(); return; }
                                                    Log($"{profile.id}: Ожидание {profile.delay_surebet} сек");
                                                    await Task.Delay(profile.delay_surebet * 1000);
                                                    await Surebet(profile, w, href, name, eventCard);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        #endregion
                    }
                    catch { }
                }));
            }
            await Task.WhenAll(tasks);
            wbusy = false;
        }

        async Task PlaceBet(Profile profile, WebCh w, IWebElement shadowRoot, IWebElement eventCard, string href, string name, decimal amount = 0, Bet pbet = null)
        {
            if (profile.type == 0) await Task.Delay(Helper.random.Next(profile.randomSecs1 * 1000, profile.randomSecs2 * 1000));
            else if (profile.type == 1)
            {
                //Log($"{profile.id}: Делаем ставку ${amount.ToString(true)} на {name}");
                await Task.Delay(1000);
            }
            var button = eventCard.FindElement(By.XPath("./div[2]/div[2]/div[2]"));
            IJavaScriptExecutor js = (IJavaScriptExecutor)w.driver;
            js.ExecuteScript("arguments[0].click();", button);
            await Task.Delay(500);
            bool found = false;
            string market = "";
            if (profile.type == 0)
            {
                market = "меньше 6.5";
                if (profile.market == 1) market = "больше 5.5";
            }
            else if (profile.type == 1)
            {
                market = "больше 6.5";
                if (profile.marketM == 1) market = "меньше 5.5";
            }
            var outcomePlates = eventCard.FindElements(By.XPath(".//div[@data-editor-id='outcomePlate']"));
            foreach (var outcomePlate in outcomePlates)
            {
                try
                {
                    var outcomePlateName = outcomePlate.FindElement(By.XPath(".//div[@data-editor-id='outcomePlateName']"));
                    if (outcomePlateName.Text.Contains(market))
                    {
                        found = true;
                        if (profile.type == 1 && profile.miss2odds)
                        {
                            try
                            {
                                var odd = Helper.DecimalParse(outcomePlate.FindElement(By.XPath("./div[1]/div[3]/span")).Text);
                                if (odd < 2) return;
                            }
                            catch { }
                        }

                        js.ExecuteScript("arguments[0].click();", outcomePlate);
                        if (profile.type == 0)
                        {
                            Log($"{profile.id}: Нажато событие {name}");
                            profile.hrefs.Add(href);
                            profile.eventsCurrentCount++;
                            SaveConfig();
                            if (profile.eventsCurrentCount >= profile.eventsCount)
                            {
                                Log($"{profile.id}: Достигнут лимит количества событий");
                                profile.started = false;
                                SaveConfig();
                                wchs.RemoveAll(q => q.name == profile.id);
                                return;
                            }
                        }
                        else if (profile.type == 1)
                        {
                            await Task.Delay(500);
                            var input = shadowRoot.FindElement(By.XPath(".//label[@data-editor-id='betslipStakeInput']//input"));
                            input.Clear(); input.SendKeys(" "); for (int i = 0; i < 10; i++) input.SendKeys(Keys.Backspace);
                            input.SendKeys(amount.ToString(true));
                            await Task.Delay(100);
                            if (config.isTest) { try { shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipSelectionRemoveButton']")).Click(); } catch { } return; }
                            else
                            {
                                var placeButton = shadowRoot.FindElement(By.XPath(".//button[@data-editor-id='betslipPlaceBetButton']"));
                                placeButton.Click();
                                for (int i = 0; i < 10; i++)
                                {
                                    await Task.Delay(1000);
                                    try
                                    {
                                        var noti = shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipNotification']/div[1]"));
                                        if (noti != null && noti.Text == "Ваша ставка успешно поставлена!")
                                        {
                                            try { shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipNotification']/svg")).Click(); } catch { }
                                            Log($"{profile.id}: Сделана ставка ${amount.ToString(true)} {market} {name}");
                                            profile.hrefs.Add(href);
                                            profile.bets.Add(new Bet { name = name, amount = amount });
                                            profile.sum += amount;
                                            if (pbet != null) profile.bets.Remove(pbet);
                                            SaveConfig();
                                            break;
                                        }
                                    }
                                    catch { }
                                }
                                try { shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipSelectionRemoveButton']")).Click(); } catch { }
                            }
                        }
                    }
                }
                catch { }
            }
            //if (!found) Log($"{profile.id}: Не найден исход {market} {name}");
            if (profile.type == 0)
            {
                await Task.Delay(500);
                js.ExecuteScript("arguments[0].click();", button);
            }
        }

        bool pin = false;
        async Task PenaltyHandicap(Profile profile, WebCh w, string href, string name, IWebElement eventCard)
        {
            try
            {
                {
                    {
                        /*var odds = new List<double>();
                        var outcomePlates = eventCard.FindElements(By.XPath(".//div[@data-editor-id='outcomePlate']"));
                        foreach (var outcomePlate in outcomePlates)
                        {
                            odds.Add(Helper.DoubleParse(eventCard.FindElement(By.XPath("./div[1]/div[3]/span")).Text));
                        }*/
                        string eventCardContent = eventCard.FindElement(By.XPath(".//a[@data-editor-id='eventCardContent']/div[1]/div[1]/div")).Text.Trim();
                        if (eventCardContent == "В ожидании пенальти") { Log($"{profile.id}: Пропущено {name}: В ожидании пенальти"); return; }

                        w.driver.SwitchTo().NewWindow(WindowType.Tab);
                        try
                        {
                            string url = $"https://bcgame.st/ru/sports?bt-path={href.Replace("https://bcgame.st", "")}";
                            await w.Go(url);
                            await w.Wait("bt-inner-page");
                            await w.Wait(8000);
                            var shadowRoot = w.GetShadow("//div[@id='bt-inner-page']");

                            var markets = new List<Market>();
                            var groups = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='tableMarketWrapper']"));
                            for (int i = 0; i < 10; i++)
                            {
                                if (groups.Count == 0)
                                {
                                    await w.Wait(1000);
                                    groups = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='tableMarketWrapper']"));
                                }
                                else break;
                            }
                            foreach (var group in groups)
                            {
                                try
                                {
                                    var marketTitle = group.FindElement(By.XPath("./div[1]/div[1]/div[2]"));
                                    string groupName = marketTitle.Text.Trim();
                                    if (groupName == "Серия пенальти фора" || groupName == "Серия пенальти - победитель")
                                    {
                                        if (groupName == "Серия пенальти фора" && !pin)
                                        {
                                            try
                                            {
                                                IJavaScriptExecutor js = (IJavaScriptExecutor)w.driver;
                                                js.ExecuteScript("arguments[0].click();", marketTitle.FindElement(By.XPath("./div[0]")));
                                            }
                                            catch { }
                                            pin = true;
                                        }

                                        var tableOutcomePlates = group.FindElements(By.XPath(".//div[@data-editor-id='tableOutcomePlate']"));
                                        foreach (var tableOutcomePlate in tableOutcomePlates)
                                        {
                                            try
                                            {
                                                var market = new Market { group = groupName };
                                                var names = tableOutcomePlate.FindElements(By.XPath(".//div[@data-editor-id='tableOutcomePlateName']/span"));
                                                market.name = string.Join(" - ", names.Select(q => q.Text));
                                                if (groupName == "Серия пенальти фора" && !market.name.StartsWith("(0.5)")) continue;
                                                market.odd = Helper.DoubleParse(tableOutcomePlate.FindElement(By.XPath("./div/div[3]/span")).Text);
                                                market.button = tableOutcomePlate;
                                                markets.Add(market);
                                            }
                                            catch (Exception ex) { Log(ex, Brushes.Red); }
                                        }
                                    }
                                }
                                catch { }
                            }

                            var pairs = new List<PairMarket>();
                            foreach (var market in markets.Where(q => q.group == "Серия пенальти фора" && q.odd >= profile.downOdd))
                            {
                                var split = Regex.Split(market.name, " - ");
                                var market1 = markets.FirstOrDefault(q => q.group == "Серия пенальти - победитель" && q.name != split[1] && q.odd >= profile.downOdd);
                                if (market1 != null) pairs.Add(new PairMarket { markets = new List<Market> { market, market1 } });
                            }
                            if (pairs.Count > 0)
                            {
                                var balanceNode = w.SelectSingleNode("//div[@class='font-extrabold flex items-center w-0 flex-auto truncate']");
                                if (balanceNode == null) { Log($"{profile.id}: Не найден баланс", Brushes.Red); return; }
                                else
                                {
                                    profile.balance = Helper.DecimalParse(balanceNode.InnerText);
                                    Render();
                                    if (profile.balance >= profile.amount2 * 2 + 2 || dev)
                                    {
                                        Log($"{profile.id}: Начало итерации {name}, баланс ${profile.balance}");
                                        var pair = pairs.OrderBy(q => q.dif).First();
                                        await Task.Delay(4000);
                                        if (!await PlaceBet(profile, w, pair.markets[0].button, shadowRoot, profile.amount2, "Пенальти " + pair.markets[0].name, false)) { Log($"Не поставлена первая ставка {name}"); return; }
                                        await PenaltyHandicapFifaRandom(profile, w);
                                        while (true) { if (await PlaceBet(profile, w, pair.markets[1].button, shadowRoot, profile.amount2, "Пенальти " + pair.markets[1].name, false)) break; }
                                        await PenaltyHandicapFifaRandom(profile, w);
                                        Log($"{profile.id}: Выполнена итерация {name}");
                                    }
                                }
                            }
                            else Log($"{profile.id}: Пропущено {string.Join("; ", markets.Select(q => $"{q.name}: {q.odd}"))}");
                            profile.hrefs.Add(href);
                            SaveConfig(false);
                        }
                        catch (Exception ex) { Log(ex, Brushes.Red); }
                        w.driver.Close();
                        w.driver.SwitchTo().Window(w.driver.WindowHandles[0]);
                    }
                }
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
        }

        async Task<bool> PlaceBet(Profile profile, WebCh w, IWebElement button, IWebElement shadowRoot, decimal amount, string name, bool quick)
        {
            bool b = false;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var el = shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipHeader']"));
                    if (el != null) { b = true; break; }
                }
                catch { }
            }
            if (!b) return false;
            while (true)
            {
                try
                {
                    var el = shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipSelectionRemoveButton']"));
                    if (el != null) el.Click(); else break;
                }
                catch { break; }
                await Task.Delay(1000);
            }
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)w.driver;
                js.ExecuteScript("arguments[0].click();", button);
                if (quick)
                {
                    Log($"{profile.id}: Сделана ставка ${amount.ToString(true)} {name}");
                    profile.sum1 += amount;
                    SaveConfig(false);
                    return true;
                }
                else 
                {
                    await Task.Delay(2000);
                    var input = shadowRoot.FindElement(By.XPath(".//label[@data-editor-id='betslipStakeInput']//input"));
                    input.Clear(); input.SendKeys(" "); for (int i = 0; i < 10; i++) input.SendKeys(Keys.Backspace);
                    input.SendKeys(amount.ToString(true));
                    await Task.Delay(2000);
                    if (config.isTest) { Log($"{profile.id}: Сделана ставка ${amount.ToString(true)} {name}"); try { shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipSelectionRemoveButton']")).Click(); } catch { } return true; }
                    else
                    {
                        var placeButton = shadowRoot.FindElement(By.XPath(".//button[@data-editor-id='betslipPlaceBetButton']"));
                        placeButton.Click();
                        for (int i = 0; i < 20; i++)
                        {
                            await Task.Delay(1000);
                            try
                            {
                                var noti = shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipNotification']/div[1]"));
                                if (noti != null)
                                {
                                    if (noti.Text == "Ваша ставка успешно поставлена!")
                                    {
                                        try { shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipNotification']/svg")).Click(); } catch { }
                                        Log($"{profile.id}: Сделана ставка ${amount.ToString(true)} {name}");
                                        profile.sum1 += amount;
                                        profile.sum_surebet += amount;
                                        SaveConfig();
                                        await Task.Delay(1000);
                                        GetBalance(w, profile);
                                        return true;
                                    }
                                    else if (noti.Text.Contains("Ставка была отклонена")) break;
                                }
                            }
                            catch { }
                        }
                        try { shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='betslipSelectionRemoveButton']")).Click(); } catch { }
                    }
                }
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
            return false;
        }

        async Task PenaltyHandicapFifaRandom(Profile profile, WebCh w)
        {
            w.driver.SwitchTo().Window(w.driver.WindowHandles[1]);

            try
            {
                var shadowRoot = w.GetShadow("//div[@id='bt-inner-page']");
                var fifas = new List<string>();
                var names = new List<string>();
                var outcomePlates = new List<IWebElement>();

                var eventCards = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='eventCard']"));
                if (eventCards != null)
                {
                    foreach (var eventCard in eventCards)
                    {
                        try
                        {
                            var eventCardContent = eventCard.FindElement(By.XPath(".//a[@data-editor-id='eventCardContent']"));
                            if (!eventCardContent.Text.Contains("Сегодня")) continue;
                            string href = eventCardContent.GetAttribute("href");
                            var simpleMarketTitle = eventCard.FindElement(By.XPath(".//div[@data-editor-id='simpleMarketTitle']"));
                            if (!string.IsNullOrEmpty(simpleMarketTitle.Text))
                            {
                                var _names = eventCard.FindElements(By.XPath(".//a[@data-editor-id='eventCardContent']/div[2]/div[1]/div"));
                                string name = $"{_names[0].Text} - {_names[1].Text}";
                                var _outcomePlates = eventCard.FindElements(By.XPath(".//div[@data-editor-id='outcomePlate']"));
                                foreach (var outcomePlate in _outcomePlates)
                                {
                                    try
                                    {
                                        var outcomePlateName = outcomePlate.FindElement(By.XPath(".//div[@data-editor-id='outcomePlateName']"));
                                        fifas.Add($"{href} {outcomePlateName.Text}");
                                        names.Add($"{name} {outcomePlateName.Text}");
                                        outcomePlates.Add(outcomePlate);
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    }

                    while (true)
                    {
                        try
                        {
                            int index = Helper.random.Next(fifas.Count);
                            string fifa = fifas[index];
                            if (!profile.fifas.Contains(fifa))
                            {
                                if (await PlaceBet(profile, w, outcomePlates[index], shadowRoot, 1, "Fifa " + names[index], false))
                                {
                                    profile.fifas.Add(fifa);
                                    SaveConfig(false);
                                }
                                break;
                            }
                        }
                        catch { }
                        await Task.Delay(1000);
                    }

                }
            }
            catch { }

            w.driver.SwitchTo().Window(w.driver.WindowHandles[2]);
        }

        async Task QuickBet(decimal amount, WebCh w)
        {
            try
            {
                await w.Wait("bt-inner-page");
                await Task.Delay(5000);
                var shadowRoot = w.GetShadow("//div[@id='bt-inner-page']");
                var quick = shadowRoot.FindElement(By.XPath(".//div[@data-editor-id='quickBetSwitcherButton']"));
                if (quick != null)
                {
                    string cl = quick.GetAttribute("class");
                    int spaceCount = cl.Count(q => q == ' ');
                    if (spaceCount == 1)
                    {
                        quick.Click();
                        await Task.Delay(1000);
                        var input = shadowRoot.FindElement(By.XPath(".//label[@data-editor-id='betslipStakeInput']//input"));
                        string text = input.GetAttribute("value");
                        if (text != amount.ToString(true))
                        {
                            input.Clear(); input.SendKeys(" "); for (int i = 0; i < 10; i++) input.SendKeys(Keys.Backspace);
                            input.SendKeys(amount.ToString(true));
                            await Task.Delay(100);
                            shadowRoot.FindElement(By.XPath(".//label[@data-editor-id='betslipStakeInput']/div/button")).Click();
                            await Task.Delay(100);
                        }
                    }
                }
            }
            catch { }
        }

        async Task Surebet(Profile profile, WebCh w, string href, string name, IWebElement eventCard)
        {
            try
            {
                w.driver.SwitchTo().NewWindow(WindowType.Tab);
                try
                {
                    string url = $"https://bcgame.st/ru/sports?bt-path={href.Replace("https://bcgame.st", "")}";
                    await w.Go(url);
                    await w.Wait("bt-inner-page");
                    await w.Wait(7000);
                    IWebElement shadowRoot = null;
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            await w.Wait(1000);
                            shadowRoot = w.GetShadow("//div[@id='bt-inner-page']");
                            if (shadowRoot != null) break;
                        }
                        catch { }
                    }

                    var pairs = new List<PairMarketSure>();
                    var groups = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='tableMarketWrapper']"));
                    for (int i = 0; i < 10; i++)
                    {
                        if (groups.Count == 0)
                        {
                            await w.Wait(1000);
                            groups = shadowRoot.FindElements(By.XPath(".//div[@data-editor-id='tableMarketWrapper']"));
                        }
                        else break;
                    }
                    foreach (var group in groups)
                    {
                        try
                        {
                            var marketTitle = group.FindElement(By.XPath("./div[1]/div[1]/div[2]"));
                            string groupName = marketTitle.Text.Trim();
                            if (groupName.ToLower().Contains("total") || groupName.ToLower().Contains("тотал") || groupName.ToLower().Contains("фора")
                                || groupName.ToLower().Contains("нечет") || groupName.ToLower().Contains("монеты"))
                            {
                                var tableOutcomePlates = group.FindElements(By.XPath(".//div[@data-editor-id='tableOutcomePlate']"));
                                for (int i = 0; i < tableOutcomePlates.Count; i += 2)
                                {
                                    try
                                    {
                                        var pair = new PairMarketSure();
                                        var tableOutcomePlate1 = tableOutcomePlates[i];
                                        var tableOutcomePlate2 = tableOutcomePlates[i + 1];

                                        var market1 = new Market { group = groupName };
                                        var names1 = tableOutcomePlate1.FindElements(By.XPath(".//div[@data-editor-id='tableOutcomePlateName']/span"));
                                        market1.odd = Helper.DoubleParse(tableOutcomePlate1.FindElement(By.XPath("./div/div[3]/span")).Text);
                                        market1.name = string.Join(" - ", names1.Select(q => q.Text)) + " " + market1.odd;
                                        market1.button = tableOutcomePlate1;
                                        pair.market1 = market1;

                                        var market2 = new Market { group = groupName };
                                        var names2 = tableOutcomePlate2.FindElements(By.XPath(".//div[@data-editor-id='tableOutcomePlateName']/span"));
                                        market2.odd = Helper.DoubleParse(tableOutcomePlate2.FindElement(By.XPath("./div/div[3]/span")).Text);
                                        market2.name = string.Join(" - ", names2.Select(q => q.Text)) + " " + market2.odd;
                                        market2.button = tableOutcomePlate2;
                                        pair.market2 = market2;

                                        if (market1.odd < 1.5 || market2.odd < 1.5) continue;
                                        pairs.Add(pair);
                                    }
                                    catch (Exception ex) { }
                                }
                            }
                        }
                        catch { }
                    }

                    if (pairs.Count > 0) 
                    {
                        GetBalance(w, profile);
                        PairMarketSure bestPair = null;
                        double maxProfit = double.MinValue, amount1 = 0, amount2 = 0;
                        foreach (var pair in pairs)
                        {
                            var amount = profile.balance;
                            if (profile.type_surebet == 1) amount = profile.amount_surebet;
                            if (profile.fifa_surebet) amount -= 2;
                            var (_amount1, _amount2, profit) = pair.GetEqualWinStakes((double)amount);
                            if (profit > maxProfit) { maxProfit = profit; bestPair = pair; amount1 = _amount1; amount2 = _amount2; }
                        }
                        
                        if (maxProfit > profile.minprofit_surebet)
                        {
                            Log($"{profile.id}: Начало итерации {name}, доходность ${maxProfit}");
                            await Task.Delay(4000);
                            if (!await PlaceBet(profile, w, bestPair.market1.button, shadowRoot, (decimal)amount1, "surebet " + bestPair.market1.name, false)) Log($"Не поставлена первая ставка {name}");
                            else
                            {
                                if (profile.fifa_surebet) await PenaltyHandicapFifaRandom(profile, w);
                                if (profile.delay_between_bets_surebet > 0)
                                {
                                    Log($"{profile.id}: Ожидание между ставками {profile.delay_between_bets_surebet} сек");
                                    await Task.Delay(profile.delay_between_bets_surebet * 1000);
                                }
                                for (int i = 0; i < 10; i++) { if (await PlaceBet(profile, w, bestPair.market2.button, shadowRoot, (decimal)amount2, "surebet " + bestPair.market2.name, false)) break; else await Task.Delay(1000); }
                                if (profile.fifa_surebet) await PenaltyHandicapFifaRandom(profile, w);
                                Log($"{profile.id}: Выполнена итерация {name}");
                                profile.hrefs.Add(href);
                                SaveConfig(false);
                            }
                        }
                        else Log($"{profile.id}: Пропущено {name}: Максимальная доходность {maxProfit}");
                    }
                    else Log($"{profile.id}: Пропущено {name}: Не найдены исходы");
                }
                catch (Exception ex) { Log(ex, Brushes.Red); }
                w.driver.Close();
                w.driver.SwitchTo().Window(w.driver.WindowHandles[0]);
            }
            catch (Exception ex) { Log(ex, Brushes.Red); }
        }

        void GetBalance(WebCh w, Profile profile)
        {
            try
            {
                var balanceNode = w.SelectSingleNode("//div[@class='font-extrabold flex items-center w-0 flex-auto truncate']");
                if (balanceNode == null) { Log($"{profile.id}: Не найден баланс", Brushes.Red); return; }
                else
                {
                    profile.balance = Helper.DecimalParse(balanceNode.InnerText);
                    SaveConfig();
                }
            }
            catch { }
        }
        #endregion
    }
}


/*
 * Style="{StaticResource WindowStyled}" Title="Настройки" Height="600" Width="800" ShowInTaskbar="False" WindowStartupLocation="CenterScreen" WindowStyle="ToolWindow" ResizeMode="NoResize" Loaded="Window_Loaded"
 * 
        MainWindow main;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            main = this.Owner as MainWindow;
        }

        public void ShowSettings()
        {
            var window = new Settings();
            window.Owner = this;
            window.ShowDialog();
        }
*/
