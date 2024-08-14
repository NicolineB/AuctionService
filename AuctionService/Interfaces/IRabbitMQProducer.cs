
using AuctionServiceClassLibrary;
using RabbitMQ.Client;

namespace Interfaces;

//Interface for RabbitMQ message producer
public interface IRabbitMQProducer
{
    Task CreateMessageAsync(AuctionBidDTO auctionObject); //Creates message from auction
    public void DeclareQueue(IModel channel);//Declares a queue on the provided channel
    public void PublishMessage(AuctionBidDTO auction, IModel channel); //Publishes a message from auction on the provided channel

}