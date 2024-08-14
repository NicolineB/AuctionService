using MongoDB.Driver;
using AuctionServiceClassLibrary;
using Interfaces;
using NLog.LayoutRenderers;

namespace DBContexts;

public class MongoDBContext : IMongoDBContext
{
    private readonly IMongoDatabase _database;
    private readonly string _userCollection;

    // Constructor that takes in a MongoClient and a database name
    public MongoDBContext(IMongoClient client, string databaseName)
    {
        // Get the database from the client
        _database = client.GetDatabase(databaseName);
    }

    // Retrieve the collection namees from environment variables
    private string auctionCollectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION_NAME_AUCTIONS");
    private string bidsCollectionName = Environment.GetEnvironmentVariable("MONGODB_COLLECTION_NAME_BIDS");

    // Get the auction collection from the database
    public virtual IMongoCollection<Auction> Auctions => _database.GetCollection<Auction>(auctionCollectionName);

    // Get the bid collection from the database
    public virtual IMongoCollection<Bid> Bids => _database.GetCollection<Bid>(bidsCollectionName);
}

