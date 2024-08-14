using AuctionServiceClassLibrary;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/legal/auctions")]
public class LegalController : ControllerBase
{
    private readonly ILogger<LegalController> _logger; // Logger to log messages
    private readonly ILegalRepository _LegalRepo; // Auction for repository for database operations, legal

    public LegalController(ILogger<LegalController> logger, ILegalRepository legalRepo)
    {
        //Assigning the logger instance passed as a parameter to the local field 
        _logger = logger;
        _LegalRepo = legalRepo;

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"AuctionService responding from {_ipaddr}");
    }

    // get only permitted by the local authorithes
    [Authorize(Policy = "LegalOnlyPolicy")]
    [HttpGet]
    public async Task<ActionResult<Auction>> GetAuctionFromDate(DateTime startDate)
    {
        try
        {
            //Retrieveing the auction from repository
            _logger.LogInformation($"Filtering auctions starting from: {startDate}");
            List<Auction> result = await _LegalRepo.GetAuctionsFromDate(startDate);

            if (result.Any() != true)
            {
                //if auction by startDate is empty
                _logger.LogWarning($"No auctions starting from: {startDate} found");
                return NotFound("No auctions found");
            }
            //If the auctions were found, returning OK response with auction data
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            // If the user is not authorized, return a 403 Forbidden response
            return StatusCode(403, "You are not authorized to perform this action.");
        }
        catch (Exception ex)
        {
            //Returning a 500 internal serve error response with the error message
            _logger.LogError($"An error occurred while getting auctions starting from: {startDate}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // get only permitted by the local authorithes
    [Authorize(Policy = "LegalOnlyPolicy")]
    [HttpGet("{id}")]
    public async Task<ActionResult> GetAuctionById(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
            {
                _logger.LogWarning($"Invalid or null ID: {id}");
                return BadRequest("Invalid or null ID");
            }

            _logger.LogInformation($"Getting auction by ID: {id}");
            Auction result = await _LegalRepo.GetAuctionById(id);

            if (result == null)
            {
                string message = $"Auction with ID '{id}' not found";

                _logger.LogWarning(message);

                return NotFound(message);
            }
            //If the auction was found, returning OK response with auction data
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            // If the user is not authorized, return a 403 Forbidden response
            return StatusCode(403, "You are not authorized to perform this action.");
        }
        catch (Exception ex)
        {
            //Returning a 500 internal serve error response with the error message
            _logger.LogError($"An error occurred while getting auction with Id: {id}. Error: {ex.Message}");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
