using RabbitMQ.Client;
using System.Text.Json;
using Interfaces;
using AuctionServiceClassLibrary;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client.Exceptions;

namespace ProducerServices;
public class RabbitMQProducer : IRabbitMQProducer
{
    private readonly IRabbitMQConnectionFactory _factory; //Dependency for creating Rabbit connections

    private readonly ILogger _logger;

    //Initializes a new instance of the Rabbitproducer
    public RabbitMQProducer(IRabbitMQConnectionFactory factory, ILogger logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task CreateMessageAsync(AuctionBidDTO auctionBidDTO)
    {

        try{
        _logger.LogInformation("Creating message " + auctionBidDTO.Id);

        //Creating a connection and channel 
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        _logger.LogInformation("Creating message " + auctionBidDTO.Id);
        // Declare the queue
        DeclareQueue(channel);

        // Publish the message with a short delay
        PublishMessage(auctionBidDTO, channel);
        Thread.Sleep(1500);
        }
        catch (RabbitMQClientException ex)
        {
            _logger.LogError(ex, "An error occurred while creating message");
            throw;
        }

        // Close the connection and channel
        return;
    }

    //Declares the queue
    public void DeclareQueue(IModel channel)
    {
        channel.QueueDeclare(queue: "auctionqueue",
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);
    }

    //Publishes a message with auction data
    public void PublishMessage(AuctionBidDTO auctionBidDTO, IModel channel)
    {

        var body = JsonSerializer.SerializeToUtf8Bytes(auctionBidDTO);

        _logger.LogInformation($"Publishing message with value: {auctionBidDTO}");

        //Publishes the message to auctionqueue
        channel.BasicPublish(exchange: string.Empty,
                            routingKey: "auctionqueue",
                            basicProperties: null,
                            body: body);
        //meesage is published
        Console.WriteLine($"Published message with value: {auctionBidDTO}");
    }
}

