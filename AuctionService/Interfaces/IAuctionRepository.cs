using AuctionServiceClassLibrary;

namespace Interfaces;

//Acessing the auction related data.
public interface IAuctionRepository
{
    Task<Auction> GetAuctionById(string auctionId); //Retrieves auction by Id
    Task<List<Auction>> GetAuctionsByStatus(string status); //Retrieves auction by status
    Task<List<Auction>> GetAllAuctions(); //Retrieves all auctions 
    Task AddAuction(Auction auction); //Adds a new auction
    Task UpdateAuction(string auctionId, Auction auction);// updating an existing auction 
    Task DeleteAuction(string auctionId);// deleting an existing auction
    Task UpdateAuctionStatus(string auctionId, AuctionStatus status);// updating auction status
    Task InsertBidToDatabase(Bid message);//Inserts a bid message into repository
    Task<Bid> GetHighestBidByAuctionId(string auctionId);//Retrieves bid by auction Id
}