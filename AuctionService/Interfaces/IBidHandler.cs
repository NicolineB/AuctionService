using RabbitMQ.Client;
using AuctionServiceClassLibrary;
namespace Interfaces;

//Interface for handling bid messages
public interface IBidHandler
{
        //Handles bid message recieved from a queue
        Task HandleMessageAsync(Bid messageObj, ulong deliveryTag, IModel channel);
}