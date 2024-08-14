using System;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace AuctionServiceClassLibrary;

public enum Role
{
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Bidder,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Seller,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Admin,
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    Legal
}

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string? GivenName { get; set; }

    public string? Address { get; set; }

    public DateTime? BirthDate { get; set; }

    public int? Telephone { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Role Role { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public User(){}

   
}
