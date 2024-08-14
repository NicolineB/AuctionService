using Interfaces;
using AuctionServiceClassLibrary;
using RabbitMQ.Client.Exceptions;

public class AuctionScheduler : BackgroundService
{
    private readonly ILogger<AuctionScheduler> _logger;
    private readonly IAuctionRepository _auctionRepository; // Assuming you have an AuctionService to handle auctions
    private readonly IRabbitMQProducer _rabbitMQProducer; // RabbitMQ producer to send messages

    public AuctionScheduler(ILogger<AuctionScheduler> logger, IAuctionRepository auctionRepository, IRabbitMQProducer rabbitMQProducer)
    {
        _logger = logger;
        _auctionRepository = auctionRepository;
        _rabbitMQProducer = rabbitMQProducer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auction scheduler is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Your logic to fetch ongoing auctions
                var auctions = await _auctionRepository.GetAllAuctions();

                foreach (var auction in auctions)
                {
                    if (auction.EndDate <= DateTime.UtcNow && auction.Status != AuctionStatus.Finished)
                    {
                        //If the auction end date is less than or equal to the current date and time, the auction has ended
                        auction.Status = AuctionStatus.Finished;
                        
                        // Update auction status to finished
                        _logger.LogInformation($"Auction with ID {auction.Id} has ended and status is updated to finished.");

                        // When an auction ends, call method update the auction object
                        await UpdateAuctionObject(auction.Id, auction);

                        _logger.LogInformation("Auction object updated successfully.  With ID : " + auction.Id);

                        // Trigger the method to send the auction bid DTO to the Product Service
                        await SendAuctionBidDTOToProductService(auction);

                    }
                    else if (auction.StartDate <= DateTime.UtcNow && auction.Status == AuctionStatus.ToBeStarted)
                    {
                        // If the auction start date is less than or equal to the current date and time, and the status is to be started
                        auction.Status = AuctionStatus.Ongoing;
                        await _auctionRepository.UpdateAuctionStatus(auction.Id, auction.Status);
                        _logger.LogInformation($"Auction with ID {auction.Id} has started and status is updated to ongoing.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing auctions.");
                throw;
            }

            // Delay for some time before checking again
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Adjust the delay as needed
        }

        _logger.LogInformation("Auction scheduler is stopping.");
    }


    public async Task UpdateAuctionObject(string Id, Auction updatedAuction)
    {
        try
        {
            _logger.LogInformation($"Update auction by Id: {Id}");
            //Retrieving the auction to be updated from the repository based on the provided Id
            var auction = await _auctionRepository.GetAuctionById(Id);

            //Assigning the Id of the updated auction to match the Id provided in the request
            updatedAuction.Id = auction.Id;

            //Updating the auction by Id
            await _auctionRepository.UpdateAuction(Id, updatedAuction);

            _logger.LogInformation($"Auction with ID {updatedAuction.Id} has been successfully updated.");
        }
        catch (Exception ex)
        {
            //If an error occurs during the updating the auction process
            _logger.LogError($"An error occurred while updating auction: {updatedAuction.Id}. Error: {ex.Message}");
        }
    }


    public async Task SendAuctionBidDTOToProductService(Auction auction)
    {
        // Send a message to the RabbitMQ queue
        try
        {
            if (auction == null)
            {
                _logger.LogError("Auction object is null.");
                return;
            };

            _logger.LogInformation($"Getting the highest bid");

            // Get the highest bid for the auction
            var highestBid = await _auctionRepository.GetHighestBidByAuctionId(auction.Id);

            if (highestBid == null)
            {
                _logger.LogInformation($"No highest bid found for auction with ID: {auction.Id}");
                return; // Exit early if no highest bid found
            }; 

            _logger.LogInformation($"Highest bid for auction with ID: {auction.Id} is {highestBid.Amount}"); 

            if (highestBid != null)
            {
                AuctionBidDTO abDTO = new AuctionBidDTO
                {
                    ProductId = auction.ProductId,
                    AuctionEndDate = auction.EndDate,
                    BidderId = highestBid.BidderId,
                    Status = AuctionStatus.Finished,
                    Amount = highestBid.Amount
                };

                // Call the RabbitMQ producer to send the message
                 await _rabbitMQProducer.CreateMessageAsync(abDTO);

                _logger.LogInformation("Message sent from Worker til Product Service.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while sending message to Product Service for auction with ID: {auction.Id}, most likely because no new data was found. Error: {ex.Message}");
            throw;  // Re-throw the exception to propagate it back to the caller
        }
    }
}
