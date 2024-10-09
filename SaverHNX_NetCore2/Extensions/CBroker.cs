using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SaverHNX_NetCore2.Settings;
using System.Text;

namespace SaverHNX_NetCore2.Extensions
{
    public class CBroker
    {
        //vars
        private readonly BrokerSetting _settingBroker;
        private ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        // props
        public int ReceivedMsgCount { get; set; }
        public long ReceivedMsgLength { get; set; }
        public int SentMsgCount { get; set; }
        public long SentMsgLength { get; set; }
        public delegate void OnMessageEventHandler(string messageBlock);
        public OnMessageEventHandler OnMessage { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public CBroker(BrokerSetting _setting)
        {
            this._settingBroker = _setting;
            this.InitBroker();
        }
        /// <summary>
        /// init broker obj
        /// chu y: can bind exchange/route voi queue name ko thi send mai ko vao queue
        /// </summary>
        /// <returns></returns>
        public bool InitBroker()
        {
            try
            {
                // log
                //CLog.LogEx("CBroker.txt", $"InitBroker => this._eBrokerConfig={Newtonsoft.Json.JsonConvert.SerializeObject(this._eBrokerConfig)}");

                this._factory = new ConnectionFactory()
                {
                    HostName = this._settingBroker.Host,
                    Port = this._settingBroker.Port,
                    UserName = this._settingBroker.Username,
                    Password = this._settingBroker.Password
                };
                this._connection = this._factory.CreateConnection();
                this._channel = this._connection.CreateModel();
                this._channel.ExchangeDeclare(
                    this._settingBroker.ExchangeName,
                    ExchangeType.Fanout,
                    Convert.ToBoolean(this._settingBroker.Durable));

                this._channel.QueueDeclare(
                    queue: this._settingBroker.QueueName,
                    durable: Convert.ToBoolean(this._settingBroker.Durable), // false, // 
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // From	"ExchangeMDDSHSX" voi Routing key "RoutingMDDSHSX" thi send vao queue "QueueMDDSHSX"
                this._channel.QueueBind(
                    this._settingBroker.QueueName,
                    this._settingBroker.ExchangeName,
                    this._settingBroker.RoutingKey
                    );

                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }

        }
        public bool SetupOnReceivedEventHandler()
        {
            try
            {
                // log
                CLog.LogEx("CBroker.txt", $"SetupOnReceivedEventHandler => _eBrokerConfig.QueueName={_settingBroker.QueueName}");

                this._channel.QueueDeclare(
                        queue: _settingBroker.QueueName,   //"hello"
                        durable: Convert.ToBoolean(this._settingBroker.Durable),
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                var consumer = new EventingBasicConsumer(this._channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    //Console.WriteLine(" [x] Received {0}", message); // debug

                    //statistic
                    ReceivedMsgCount++;
                    ReceivedMsgLength += message.Length;

                    OnMessage(message);
                };
                this._channel.BasicConsume(
                        queue: _settingBroker.QueueName,    //"hello"
                        autoAck: true,
                        consumer: consumer);
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        /// <summary>
		/// send msg vao rabbit queue
		/// </summary>
		/// <param name="messageBlock"></param>
		/// <returns></returns>
		public bool SendMessageToQueue(string messageBlock)
        {
            try
            {
                SentMsgCount++;
                SentMsgLength += messageBlock.Length;

                var body = Encoding.UTF8.GetBytes(messageBlock);

                // send to queue
                this._channel.BasicPublish(
                    exchange: this._settingBroker.ExchangeName,
                    routingKey: this._settingBroker.RoutingKey,
                    basicProperties: null,
                    body: body);
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
        public bool SendMessageToQueue(string[] messageBlock)
        {
            try
            {
                SentMsgCount++;
                SentMsgLength += messageBlock.Length;

                foreach (var message in messageBlock)
                {
                    var body = Encoding.UTF8.GetBytes(message);

                    // Send each message to the queue
                    this._channel.BasicPublish(
                        exchange: this._settingBroker.ExchangeName,
                        routingKey: this._settingBroker.RoutingKey,
                        basicProperties: null,
                        body: body);
                }
                return true;
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
                return false;
            }
        }
    }
}
