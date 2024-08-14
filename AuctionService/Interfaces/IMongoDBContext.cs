using MongoDB.Driver;
using AuctionServiceClassLibrary;

namespace Interfaces
{
    public interface IMongoDBContext
    {
        // Property to access the collection of auctions
        IMongoCollection<Auction> Auctions { get; }

        // Property to access the collection of bids
        IMongoCollection<Bid> Bids { get; }
    }
}
