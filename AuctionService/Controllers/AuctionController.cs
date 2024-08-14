using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Interfaces;
using AuctionServiceClassLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace Controllers;

[ApiController]
[Route("[controller]/v1/auctions")]
public class AuctionController : ControllerBase
{
    private readonly ILogger<AuctionController> _logger; // Logger to log messages

    private readonly IAuctionRepository _auctionRepo; // Auction repository for database operations

    private readonly IRabbitMQProducer _rabbitMQProducer; // RabbitMQ producer to send messages

    //This constructor initializes an instance of the AuctionController class
    public AuctionController(ILogger<AuctionController> logger, IAuctionRepository auctionRepo, IRabbitMQProducer rabbitMQProducer)
    {
        //Assigning the logger instance passed as a parameter to the local field 
        _logger = logger;
        _auctionRepo = auctionRepo;
        _rabbitMQProducer = rabbitMQProducer;
        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"AuctionService responding from {_ipaddr}");
    }

    // Get auction by Id
    [HttpGet("{Id?}")]
    public async Task<ActionResult> GetAuctionById(string Id)
    {
        try
        {
            if (string.IsNullOrEmpty(Id) || !ObjectId.TryParse(Id, out _))
            {
                _logger.LogWarning($"Invalid or null ID: {Id}");
                return BadRequest("Invalid or null ID");
            }

            _logger.LogInformation($"Getting auction by ID: {Id}");
            Auction result = await _auctionRepo.GetAuctionById(Id);

            if (result == null)
            {
                string message = $"Auction with ID '{Id}' not found";

                _logger.LogWarning(message);

                return NotFound(message);
            }
            //If the auction was found, returning OK response with auction data
            return Ok(result);
        }
        catch (Exception ex)
        {
            //Returning a 500 internal serve error response with the error message
            _logger.LogError($"An error occurred while getting auction with Id: {Id}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

    }

    [HttpGet("status/{status}")]
    // Get auctions by status
    public async Task<ActionResult> GetAuctionsByStatus(string status)
    {
        try
        {

            _logger.LogInformation($"Getting auctions by status");
            //Retrieving auctions by status from repository
            List<Auction> result = await _auctionRepo.GetAuctionsByStatus(status);

            if (result?.Any() != true)

            {
                string message = "Auctions by status not found";
                _logger.LogWarning(message);
                //if the auctions status was now found 
                return NotFound(message);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while getting auctions by status. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }

    }

    //Get all auctions
    [HttpGet]
    public async Task<ActionResult> GetAllAuctions()
    {
        try
        {
            _logger.LogInformation($"Getting all auctions");
            List<Auction> result = await _auctionRepo.GetAllAuctions();

            //Check if auction exists
            if (result?.Any() != true)
            {
                string message = "Auctions not found";
                _logger.LogWarning(message);
                return NotFound(message);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            //If an error occurs while getting alle auctions 
            _logger.LogError($"An error occurred while getting relevant and active auctions. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    //Adding new auction - ADMIN ONLY
    [Authorize(Policy = "AdminOnlyPolicy")]
    [HttpPost]
    public async Task<IActionResult> AddAuction([FromBody] Auction newAuction)
    {
        try
        {
            _logger.LogInformation($"Adding new auction: {newAuction.Id}");
            await _auctionRepo.AddAuction(newAuction);

            if (newAuction == null || newAuction.StartDate > newAuction.EndDate)
            {
                string message = "Auction cannot be created";
                _logger.LogWarning(message);
                return NotFound(message);
            }

            return Ok("Auction successfully created");
        }
        catch (UnauthorizedAccessException)
        {
            // If the user is not authorized, return a 403 Forbidden response
            return StatusCode(403, "You are not authorized to perform this action.");
        }
        catch (Exception ex)
        {
            //If an error occurs during the adding auction process
            _logger.LogError($"An error occurred while adding auction: {newAuction.Id}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    //Update auction by Id - ADMIN ONLY
    [Authorize(Policy = "AdminOnlyPolicy")]
    [HttpPut("{Id}")]
    public async Task<IActionResult> UpdateAuction(string Id, [FromBody] Auction updatedAuction)
    {
        try
        {
            _logger.LogInformation($"Update auction by Id: {Id}");

            if (string.IsNullOrEmpty(Id) || !ObjectId.TryParse(Id, out _))
            {
                string message = "Received an invalid or null ID";
                _logger.LogWarning(message);
                return BadRequest(message);
            }
            
            //Retriveing the auction to be updated from the repository based on the provided Id
            var auction = await _auctionRepo.GetAuctionById(Id);

            // Check if the auction exists
            if (auction == null)
            {
                _logger.LogWarning($"Auction with Id: {Id} not found.");
                return NotFound(new { message = "Auction not found" });
            }

            //Assigning the Id of the updated auction to match the Id provided in the request
            updatedAuction.Id = auction.Id;

            //Updating the auction by Id
            await _auctionRepo.UpdateAuction(Id, updatedAuction);
            return Ok($"Auction with ID {updatedAuction.Id} has been successfully updated.");
        }
        catch (UnauthorizedAccessException)
        {
            // If the user is not authorized, return a 403 Forbidden response
            return StatusCode(403, "You are not authorized to perform this action.");
        }
        catch (Exception ex)
        {
            //If an error occurs during the updating the auction process
            _logger.LogError($"An error occurred while updating auction: {updatedAuction.Id}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    //Delete auction by Id - ADMIN ONLY
    [Authorize(Policy = "AdminOnlyPolicy")]
    [HttpDelete("{Id}")]
    public async Task<IActionResult> DeleteAuction(string Id)
    {
        try
        {
            _logger.LogInformation($"Delete auction by Id: {Id}");

            if (string.IsNullOrEmpty(Id) || !ObjectId.TryParse(Id, out _))
            {
                string message = "Received an invalid or null ID";
                _logger.LogWarning(message);
                return BadRequest(message);
            }
            
            //Retrieving the auction to be deleted
            var auction = await _auctionRepo.GetAuctionById(Id);

            // Check if the auctionId exists
            if (auction == null)
            {
                _logger.LogWarning($"Auction with Id: {Id} not found.");
                return NotFound(new { message = "Auction not found" });
            }

            //To delete the auction through the repository
            await _auctionRepo.DeleteAuction(Id);
            return Ok($"Auction with ID {Id} has been successfully deleted.");
        }
        catch (UnauthorizedAccessException)
        {
            // If the user is not authorized, return a 403 Forbidden response
            return StatusCode(403, "You are not authorized to perform this action.");
        }
        catch (Exception ex)
        {
            //If an error occurs during the deletion the auction process
            _logger.LogError($"An error occurred while deleting auction with Id: {Id}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    //Adding the auction data to ProductService through RabbitMQ
    [HttpPost("data")]
    public async Task<IActionResult> SendAuctionData([FromBody] AuctionBidDTO auction)
    {
        //Logging and sending auction data to RabbitMQ
        _logger.LogInformation(" Sending message to RabbitMQ\n " + auction.Id);

        try
        {
            _logger.LogInformation("Validating request body\n");

            //Checking if the AuctionBidDTO is null or empty
            if (string.IsNullOrEmpty(auction.Id) ||
            string.IsNullOrEmpty(auction.ProductId) ||
            string.IsNullOrEmpty(auction.BidderId) ||
            auction.AuctionEndDate == null ||
            auction.Status == null ||
            auction.Amount == null)
            {
                //Logging and returning Badrequest reponse if the body is invalid 
                _logger.LogError("Invalid request body.");
                return BadRequest("Invalid request body.");
            }

            //If the request
            _logger.LogInformation("Request body is valid.");

            // Call the service method with the extracted values
            await _rabbitMQProducer.CreateMessageAsync(auction);

            _logger.LogInformation("Message sent successfully.");

            //Returning an Ok resposne with a message, indicating sucessful sent message to RabbitMq
            return Ok($"Message sent successfully.");
        }
        catch (Exception ex)
        {
            //If an error occurs during the adding the auction process to RabbitMq
            _logger.LogError($"An error occured while trying to publish message with productid: {auction.ProductId}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
