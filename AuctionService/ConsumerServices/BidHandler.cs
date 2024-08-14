using RabbitMQ.Client;
using AuctionServiceClassLibrary;
using Interfaces;
using MongoDB.Driver;

namespace ConsumerServices
{
    public class BidHandler : IBidHandler
    {
        //List of dependencies
        private readonly ILogger<BidHandler> _logger; //Logger instance for logging bid handling operations
        private readonly IAuctionRepository _auctionRepository; //Repository for acessing auction data

        // Initializes a new instance of the BidHandler class w/ dependencies
        public BidHandler(ILogger<BidHandler> logger, IAuctionRepository auctionRepository)
        {
            _logger = logger;
            _auctionRepository = auctionRepository;
        }

        //Method to handle incoming messages
        public async Task HandleMessageAsync(Bid message, ulong deliveryTag, IModel channel)
        {
            if (message == null)
            {
                _logger.LogError("Received null bid message");
                return;
            }

            _logger.LogInformation($"Handling bid message: {message}");

            try
            {
                //Check if the message is valid based on the properties
                await SaveBidToDatabaseAsync(message);
                _logger.LogInformation("Bid saved to the database");

                //Acknowledge the messages, which means the message has been processed
                channel.BasicAck(deliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while processing the bid message: {ex.Message}");
            }
        }

        //Method for saving bid to the database
        private async Task SaveBidToDatabaseAsync(Bid bid)
        {
            try
            {
                //Inserting the bid into the database using auction repository
                await _auctionRepository.InsertBidToDatabase(bid);
            }
            catch (MongoConnectionException)
            {
                _logger.LogError("Error occurred while connecting to the database");
                throw;
            }
            catch (Exception ex)
            {
                //Log an error if an exception occurs during the database operation
                _logger.LogError($"Error occurred while saving bid to the database: {ex.Message}");
                // Handle database error accordingly
                throw;
            }
        }
    }
}
