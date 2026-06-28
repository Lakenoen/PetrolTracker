using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DbManager;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.Script;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace PetrolLoader;
public class Loader
{
    private readonly Context _ctx;
    private readonly ChromeDriver _driver;
    private readonly ILogger? _logger = null;
    private readonly Geocoder _geocoder;

    public int Delay { get; set; } = 2000;
    public Loader(Context ctx, string mapApiKey, ILogger? logger)
    {
        _geocoder = new Geocoder(mapApiKey);

        this._logger = logger;
        this._ctx = ctx;
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddExcludedArgument("enable-automation");

        _driver = new ChromeDriver(options);
    }

    private double[] GetCoord(string address)
    {
        var coords = _geocoder.GetCoord(address);

        if (coords.latitude == 0 && coords.longitude == 0)
            throw new ApplicationException("Addresses is empty");

        return new double[2]{coords.latitude, coords.longitude};
    }

    private void GoToPageRussiabase(int region, int page)
    {
        _driver.Navigate().GoToUrl($"https://russiabase.ru/prices?region={region}&page={page}");

        Thread.Sleep(this.Delay);

        IWebElement frame = _driver.FindElement(By.XPath("//*[@id=\"__next\"]/div/div/iframe"));
        _driver.SwitchTo().Frame(frame);

        IWebElement btn = _driver.FindElement(By.Id("js-button"));
        btn.Click();

        _driver.SwitchTo().DefaultContent();

        Thread.Sleep(this.Delay);
    }
    public void LoadFromRussiabaseAsync(int region)
    {
        _logger?.LogInformation("Starting parse...");
        GoToPageRussiabase(region, 1);

        int lastPage = int.Parse(_driver.FindElement(By.ClassName("Pagination_pagination__yfQ_d"))
            .FindElements(By.TagName("a"))
            .Select(a => a.FindElement(By.TagName("button")).Text).ToList().Last() );

        try
        {
            int p = 1;
            while(p <= lastPage)
            {
                try
                {
                    foreach (var el in _driver.FindElements(By.ClassName("ListingCard_orgCard__xCwyi")).ToList())
                    {
                        string? name = el.FindElement(By.ClassName("ListingCard_heading___st_D"))
                            .FindElement(By.ClassName("ListingCard_headingContent__HQRWs"))
                            .FindElement(By.TagName("a")).GetAttribute("innerText")?.ToLower();

                        if (name == null)
                            throw new ApplicationException("GasStation name has been null");

                        List<string> names = el.FindElements(By.ClassName("PricesListNew_blockLabel__FyFeq"))
                            .Select(a => a.Text.ToLower()).ToList();

                        List<double> prices = el.FindElements(By.ClassName("PricesListNew_pricing__m0s8Y"))
                            .Select(a => a.FindElement(By.TagName("p")).Text).Where(a => (a[0] != '+' && a[0] != '-') ? true : false)
                            .Select(a => double.Parse(Regex.Match(a, @"\d+.\d+").Value.Replace('.', ',')))
                            .ToList();

                        if (names.Count != prices.Count)
                            throw new ApplicationException("names count not equal prices count");

                        string location = el.FindElement(By.ClassName("ListingCard_iconBlockText___egMo")).Text.ToLower();

                        double[] coord = new double[2] {double.MinValue, double.MinValue};
                        try
                        {
                            coord = GetCoord(location);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex.Message + " Skip station");
                            continue;
                        }

                        string update = el.FindElement(By.ClassName("ListingCard_info__SfFLf")).Text;

                        update = Regex.Match(update, @"\d{2}.\d{2}.\d{4}").Value;

                        GasStation station = new GasStation { Address = location, Name = name, Latitude = coord[0], Longitude = coord[1] };
                        _ctx.GasStations.Add(station);

                        for (int i = 0; i < names.Count; i++)
                        {
                            var petrls = _ctx.Petrols.Where( p => p.Name == names[i] && p.Price == prices[i]).ToList();
                            Petrol? petrol = null;
                            if (petrls.Count == 0)
                            {
                                petrol = new Petrol { Name = names[i], Price = prices[i]};
                                _ctx.Petrols.Add(petrol);
                            }
                            else
                            {
                                petrol = petrls.First();
                            }

                            var upd = DateTime.Parse(update);
                            upd = DateTime.SpecifyKind(upd, DateTimeKind.Utc);
                            station.GasStationPetrols.Add(new GasStationPetrol { Petrol = petrol!, GasStation = station, Update = upd });
                        }

                    }

                    lock(_ctx.Locker)
                    {
                        _ctx.SaveChanges();
                    }

                    _logger?.LogInformation($"Page loaded, page: {p}");
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex.Message);
                    _logger?.LogWarning(ex.InnerException?.Message);
                }

                if(p + 1 <= lastPage)
                    GoToPageRussiabase(region, p + 1);

                ++p;
            }

        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
        }

        _driver.Quit();
    }

}