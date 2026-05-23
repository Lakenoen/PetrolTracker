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
            Settings settings = new Settings
            {
                UpdateDB = bool.Parse(args[3]),
                ConnectionDB = args[0]
            };
            string mapApiKey = args[1];
            string from = args[2];

            Context ctx = new Context(settings);

            Loader loader = new Loader(ctx, mapApiKey, logger);
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