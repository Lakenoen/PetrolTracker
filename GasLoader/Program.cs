using DbManager;
using PetrolLoader;
using Microsoft.Extensions.Logging;

class Program
{
    public static void Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger("Loader");

        try
        {
            GlobalSettings.UpdateDB = bool.Parse(args[3]);
            GlobalSettings.ConnectionDB = args[0];
            string mapApiKey = args[1];
            string from = args[2];

            Loader loader = new Loader(DbManager.Context.Instance, mapApiKey, logger);
            switch (from.ToLower())
            {
                case "russiabase": loader.LoadFromRussiabaseAsync(90); break;
                default: throw new ApplicationException("Unknown source");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("GasLoader <connection> <mapApiKey> <source> <update>");
            logger.LogError(ex.Message);
        }
    }
}
