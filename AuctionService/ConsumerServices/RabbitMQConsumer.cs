using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MongoDB.Driver;
using AuctionServiceClassLibrary;
using System.Text.Json;
using System.Text;
using Interfaces;

namespace AuctionService;

/// <summary>
/// This is where the RabbitMQConnectionFactory is injected for the Worker class (from the IRabbitMQConnectionFactory interface). 
/// This enables the Worker class to create a connection to RabbitMQ from RabbitMQConnection facotyr class VIA the interface 
/// </summary>
public class RabbitMQConsumer : BackgroundService
{
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly IRabbitMQConnectionFactory _factory;
    private readonly IBidHandler _bidHandler;


    //everytime the Worker class is called upon (from program.cs on app start) it is constructed with the following parameters
    //This provides the Worker class with the necessary dependencies to interact with RabbitMQ, MongoDB and the MessageHandler class
    public RabbitMQConsumer(ILogger<RabbitMQConsumer> logger, IRabbitMQConnectionFactory factory, IBidHandler bidHandler)
    {
        _logger = logger;
        _factory = factory;
        _bidHandler = bidHandler;

        // Get the host name of the current machine
        var hostName = System.Net.Dns.GetHostName();

        // Get the IP addresses associated with the host name
        var ips = System.Net.Dns.GetHostAddresses(hostName);

        // Get the first IPv4 address from the list of IP addresses
        var _ipaddr = ips.First().MapToIPv4().ToString();

        // Log the information about the service's IP address
        _logger.LogInformation(1, $"Consumer Service responding from {_ipaddr}");

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //Creating a connection to RabbitMQ
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        //Declaring the queue
        await DeclareQueue(channel);

        //Consuming the message
        await ConsumeMessage(channel, stoppingToken);

    }


    //Declaring a queue on the provided model
    public async Task DeclareQueue(IModel channel)
    {
        //Declares a bid_queue 
        channel.QueueDeclare(queue: "bidqueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
    }


    public async Task ConsumeMessage(IModel channel, CancellationToken stoppingToken)
    {
        //Creating a consumer
        var consumer = new EventingBasicConsumer(channel);

        //Receiving the message from the queue
        consumer.Received += async (model, ea) =>
        {
            //Extracting message body and converting it to string 
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            //Deserialises the message intot Bid object.
            var bid = JsonSerializer.Deserialize<Bid>(message);

            _logger.LogInformation("Received bid with amount: " + bid.Amount);

            _logger.LogInformation($"Received bid in full {message}");

            //After consuming, calling the MessageHandler class to handle the message
            await _bidHandler.HandleMessageAsync(bid, ea.DeliveryTag, channel);
        };

        //start consuming messages from bid_queue
        channel.BasicConsume(queue: "bidqueue", autoAck: true, consumer: consumer);

        //loops to keep it running until cancellation is requested
        while (!stoppingToken.IsCancellationRequested)
        {
            //Delay the loop 
            await Task.Delay(1000, stoppingToken);
        }
    }
}
