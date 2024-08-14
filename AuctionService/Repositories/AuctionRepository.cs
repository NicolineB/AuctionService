using MongoDB.Driver;
using Interfaces;
using AuctionServiceClassLibrary;

namespace Repositories;
public class AuctionRepository : IAuctionRepository
{
    private readonly ILogger _logger; //logger to log messages
    private readonly IMongoCollection<Bid> _bidCollection; // Collection to interact with MongoDB
    private readonly IMongoCollection<Auction> _auctionCollection; // Collection to interact with MongoDB
    private readonly IMongoDBContext _context; // MongoDB context

    public AuctionRepository(ILogger<AuctionRepository> logger, IMongoDBContext context)
    {
        _logger = logger;
        _context = context;
        _bidCollection = _context.Bids;
        _auctionCollection = _context.Auctions;
    }

    // Get auction by ID from the database
    public async Task<Auction> GetAuctionById(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id), "ID cannot be null or empty");
            }

            _logger.LogInformation($"Getting auction from database by Id: {id}");

            return await _auctionCollection.Find(z => z.Id == id).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while getting auction from database with Id: {id}. Error: {ex.Message}");
            throw; // Propagate the exception to the calling method
        }
    }

    // Get list of auctions by status from the database - USER
    public async Task<List<Auction>> GetAuctionsByStatus(string status)
    {
        try
        {
            if (string.IsNullOrEmpty(status))
            {
                throw new ArgumentNullException(nameof(status), "Status cannot be null or empty");
            }

            _logger.LogInformation($"Getting auctions by status from database");

            //Find the auction with status and return them 
            Enum.TryParse(status, out AuctionStatus enumstatus);
            var activeAuctions = await _auctionCollection.Find(a =>
            a.Status == enumstatus).ToListAsync();

            foreach (var auction in activeAuctions)
            {
                _logger.LogInformation($"Auction ID: {auction.Id} - Status: {auction.Status}");
            }
            return activeAuctions;
        }
        catch (Exception ex)
        {
            //Log if there is an error while retrieving auction
            _logger.LogError($"An error occurred while getting auction by status from database. Error: {ex.Message}");
            throw;
        }
    }

    // Get list of auctions from the database - ONLY FOR ADMIN
    public async Task<List<Auction>> GetAllAuctions()
    {
        try
        {
            _logger.LogInformation($"Getting auctions from database");
            //Get all auctions from collection and return them
            return await _auctionCollection.Find(a => true).ToListAsync();
        }
        catch (Exception ex)
        {
            //If occurs an error while getting auction 
            _logger.LogError($"An error occurred while getting auctions from database. Error: {ex.Message}");
            throw;
        }
    }

    // Add new auction to the database - ONLY FOR ADMIN
    public async Task AddAuction(Auction newAuction)
    {
        try
        {
            if (newAuction == null)
            {
                throw new ArgumentNullException(nameof(newAuction), "Auction Id cannot be null or empty");
            }
            _logger.LogInformation($"Add auction (repo): {newAuction.Id}");
            //Adding new auction into auction collection 
            await _auctionCollection.InsertOneAsync(newAuction);
        }
        catch (Exception ex)
        {
            //If an error occurs adding the new auction 
            _logger.LogError($"An error occurred while adding auction (repo): {newAuction.Id}. Error: {ex.Message}");
            throw;
        }
    }

    //Update auction in the database by its ID - ONLY FOR ADMIN
    public async Task UpdateAuction(string Id, Auction updatedAuction)
    {
        try
        {
            if (updatedAuction == null)
            {
                throw new ArgumentNullException(nameof(updatedAuction), "Auction Id cannot be null or empty");
            }

            _logger.LogInformation($"Update auction with Id:  {Id}");
            //Updating the auction with Id into collection 
            await _auctionCollection.ReplaceOneAsync(z => z.Id == Id, updatedAuction);
        }
        catch (Exception ex)
        {
            //If an error occurs while updating auction 
            _logger.LogError($"An error occurred while updating auction (repo): {updatedAuction.Id}. Error: {ex.Message}");
            throw;
        }
    }

    //Update auction in the database by its ID - ONLY FOR ADMIN
    public async Task UpdateAuctionStatus(string Id, AuctionStatus auctionStatus)
    {
        try
        {
            _logger.LogInformation($"Update auction (repo) by Id: {Id}");

            Auction auctionToBeUpdated = _auctionCollection.Find(z => z.Id == Id).FirstOrDefault();
            _logger.LogInformation("Auction to be updated: " + auctionToBeUpdated.Id);
            auctionToBeUpdated.Status = auctionStatus;

            // Updating the auction with Id into collection 
            await _auctionCollection.ReplaceOneAsync(z => z.Id == auctionToBeUpdated.Id, auctionToBeUpdated);
        }
        catch (Exception ex)
        {
            //If an error occurs while updating auction 
            _logger.LogError($"An error occurred while updating auction (repo): {Id}. Error: {ex.Message}");
            throw;
        }
    }

    // Delete auction from the database by its ID - ONLY FOR ADMIN 
    public async Task DeleteAuction(string Id)
    {
        try
        {
            _logger.LogInformation($"Delete user (repo) by Id: {Id}");
            //Deleting the aution with Id into the auction collection 
            await _auctionCollection.DeleteOneAsync(z => z.Id == Id);
        }
        catch (Exception ex)
        {
            //If an error ocurs while deleting auction 
            _logger.LogError($"An error occurred while deleting auction (repo) by Id: {Id}. Error: {ex.Message}");
            throw;
        }
    }

    //Inserting bid message
    public async Task InsertBidToDatabase(Bid messageObj)
    {
        try
        {
            //Insert the bid message into bid collection 
            await _bidCollection.InsertOneAsync(messageObj);
            _logger.LogInformation("Bid object inserted successfully into the database.");
        }
        catch (Exception ex)
        {
            //If and error occurs while inserting bid intot database 
            _logger.LogError($"Error occurred while inserting bid object into the database: {ex.Message}");
            throw; // Re-throw the exception to propagate it back to the caller
        }
    }

    public async Task<Bid> GetHighestBidByAuctionId(string auctionId)
    {
        try
        {
            //Find the bid with auction Id and return it
            var bids = await _bidCollection.Find(z => z.AuctionId == auctionId).ToListAsync();
            // Get the highest bid
            var highestBid = bids.OrderByDescending(z => z.Amount).FirstOrDefault();

            _logger.LogInformation("Highest bid for auction." + highestBid.Amount);

            return highestBid;
        }
        catch (NullReferenceException ex)
        {
            _logger.LogError($"Error occured probably because no bids were to be found {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            //If and error occurs while inserting bid intot database 
            _logger.LogError($"Error occurred while inserting bid object into the database: {ex.Message}");
            throw; // Re-throw the exception to propagate it back to the caller
        }
    }
}