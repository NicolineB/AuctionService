using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using System.Text.Json.Serialization;

namespace AuctionServiceClassLibrary;


public enum AuctionBidStatus
{
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Ongoing,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    ToBeStarted,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Annulled,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Finished
}
public class AuctionBidDTO
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

 [BsonRepresentation(BsonType.ObjectId)]
    public string? ProductId { get; set; }

    public DateTime AuctionEndDate { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? BidderId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuctionStatus Status { get; set; }

    public int Amount { get; set; }
}

