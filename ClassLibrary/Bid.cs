using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuctionServiceClassLibrary;

public class Bid
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AuctionId { get; set; }

    public DateTime DateSent { get; set; } = DateTime.Now;
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? BidderId { get; set; }

    public int Amount { get; set; }
}

