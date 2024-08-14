using AuctionServiceClassLibrary;

namespace Interfaces;
public interface ILegalRepository
{
    Task<List<Auction>> GetAuctionsFromDate(DateTime startDate); //Retrieves auction starting from specified date
    Task<Auction> GetAuctionById(string auctionId); //Retrieves auction by Id
}