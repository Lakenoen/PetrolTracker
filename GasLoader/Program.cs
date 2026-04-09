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
            GlobalSettings.UpdateDB = bool.Parse(args[2]);
            GlobalSettings.ConnectionDB = args[0];
            string from = args[1];

            Loader loader = new Loader(DbManager.Context.Instance, logger);
            switch (from.ToLower())
            {
                case "russiabase": loader.LoadFromRussiabaseAsync(90); break;
                default: throw new ApplicationException("Unknown source");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("GasLoader <connection> <source> <update>");
            logger.LogError(ex.Message);
        }
    }
}
