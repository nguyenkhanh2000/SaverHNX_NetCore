using SaverHNX_NetCore2.BLL;
using SaverHNX_NetCore2.Extensions;
using SaverHNX_NetCore2.Settings;

class Program
{
    static Mutex _mutex = new Mutex(true,"HNX_SAVER_NETCORE2");   
    static void Main(string[] args)
    {
        try
        {
            // start app
            Console.WriteLine("START APP => " + CConfig.DateTimeNow);

            //khong cho chay 2 instance
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                Console.WriteLine("CAN NOT RUN 2 INSTANCES !!! => " + CConfig.DateTimeNow);
                return;
            }
            //Đọc cấu hình từ appseting
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  //Thiết lập đường dẫn cơ sở
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) //Đọc tệp cấu hình
                .Build();
            AppSetting _appsetting = AppSetting.MapValue(configuration);

            CBroker _broker = new CBroker(_appsetting.BrokerSetting);

            CRedis _redis = new CRedis(_appsetting.RedisSetting);

            CSaverHNXLib _cSaverHNXLib = new CSaverHNXLib(_appsetting, _broker, _redis);

            // Perform text input
            while (true)
            {
                Console.WriteLine("PRESS KEY 'Y' TO EXIT...");

                // an nut Y roi ENTER thi exit app
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Y)
                    System.Environment.Exit(CSaverHNXLib.__CODE_EXIT_APP_FROM_CONSOLE_PRESS_Y);
            }
        }
        catch(Exception ex) 
        {
            throw ex;
        }
    }
}
