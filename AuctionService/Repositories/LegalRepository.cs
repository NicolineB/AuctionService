using Interfaces;
using AuctionServiceClassLibrary;
using MongoDB.Driver;

namespace Repositories
{
    public class LegalRepository : ILegalRepository
    {
        private readonly ILogger _logger; //logger to log messages
        private readonly IMongoCollection<Auction> _auctionCollection; // Collection to interact with MongoDB
        private readonly IMongoDBContext _context; // MongoDB context

        public LegalRepository(ILogger<AuctionRepository> logger, IMongoDBContext context)
        {
            _logger = logger;
            _context = context;
            _auctionCollection = _context.Auctions;
        }

        // Get list of auctions - only permitted by admin
        public async Task<List<Auction>> GetAuctionsFromDate(DateTime startDate)
        {
            try
            {
                _logger.LogInformation($"Getting auctions from database");
                //Get all auctions from collection and return them
                return await _auctionCollection.Find(a => a.StartDate >= startDate).ToListAsync();
            }
            catch (Exception ex)
            {
                //If occurs an error while getting auction 
                _logger.LogError($"An error occurred while getting auctions from database. Error: {ex.Message}");
                throw;
            }
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
    }
}
