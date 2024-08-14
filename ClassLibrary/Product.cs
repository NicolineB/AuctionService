using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace AuctionServiceClassLibrary;

public enum ProductCategory
{
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Furniture,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Jewelry,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Art,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Collectibles
}

public enum ProductStatus
{
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Registered,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    PendingAuction,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    InAuction,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Sold,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    NotSold,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Denied
}

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductCategory Category { get; set; }

    public int? ValuationPrice { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? BidderId { get; set; }

    public int? SalePrice { get; set; }

    public string? Description { get; set; }
    
    [BsonRepresentation(BsonType.ObjectId)]
    public string? SellerId { get; set; }

    public bool Evaluation { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProductStatus Status { get; set; }

    public DateTime? SaleDate { get; set; }

    public DateTime? AuctionStartDate { get; set; }
}

