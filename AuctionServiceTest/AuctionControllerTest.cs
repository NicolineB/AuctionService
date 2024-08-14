using AuctionServiceClassLibrary;
using Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Interfaces;

namespace AuctionServiceTest;

[TestClass]
public class AuctionControllerTest
{
    private Mock<ILogger<AuctionController>> _loggerMock;
    private Mock<IAuctionRepository> _auctionRepoMock;
    private ILogger<AuctionController> _consoleLogger;
    private Mock<IRabbitMQProducer> _rabbitMQProducerMock;
    private IRabbitMQProducer _rabbitMQProducer; //For integration testing

    [TestInitialize]
    public void SetupBeforeEachTest()
    {
        _loggerMock = new Mock<ILogger<AuctionController>>();
        _auctionRepoMock = new Mock<IAuctionRepository>();
        _rabbitMQProducerMock = new Mock<IRabbitMQProducer>();

        // Create a console logger for additional logging visibility
        _consoleLogger = LoggingServiceProvider.CreateLogger<AuctionController>();
    }

    //GetAuctionById Tests
    
    [TestMethod]
    public async Task GetAuctionById_ReturnsAuctionById()
    {
        _consoleLogger.LogInformation("GetAuction_ReturnsAuctionById test starting...");

        // Arrange
        string auctionId = "664607e52c72ebe1ed6be47a"; // Valid auctionId format 
        var auction = new Auction { Id = auctionId, StartPrice = 200 };

        // Setup the mock to return the expected auction
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(auctionId)).ReturnsAsync(auction);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAuctionById(auctionId);

        // Assert
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));
        var okResult = response as OkObjectResult;
        Assert.IsNotNull(okResult);
        var auctionResult = okResult.Value as Auction;
        Assert.IsNotNull(auction);
        Assert.AreEqual(auction.Id, auctionResult.Id);
        Assert.AreEqual(auction.StartPrice, auctionResult.StartPrice);

        _consoleLogger.LogInformation("GetAuction_ReturnsAuctionById test completed!");
    }

    [TestMethod]
    public async Task GetAuctionById_ReturnsNotFoundForNonExistingResource()
    {
        _consoleLogger.LogInformation("GetAuctionById_ReturnsNotFoundForNonExistingResource test starting...");

        // Arrange
        string auctionId = "664607e52c72ebe1ed6be88a"; // Non-existing auction

        // Setup the mock to return null, indicating the auction doesn't exist
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(auctionId)).ReturnsAsync((Auction)null);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAuctionById(auctionId);

        _consoleLogger.LogInformation("Response type: " + response.GetType());

        // Assert
        Assert.IsInstanceOfType(response, typeof(NotFoundObjectResult));
        var notFoundObject = response as NotFoundObjectResult;
        Assert.AreEqual(404, notFoundObject.StatusCode);

        _consoleLogger.LogInformation("GetAuctionById_ReturnsNotFoundForNonExistingResource test completed!");
    }

    [TestMethod]
    public async Task GetAuctionById_ReturnsBadRequestForInvalidId()
    {
        _consoleLogger.LogInformation("GetAuction_ReturnsBadRequestForInvalidId starting..");
        // Arrange
        string invalidId = "1234"; // Invalid ID

        // Setup the mock to return null, indicating the auction doesn't exist
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(invalidId)).ReturnsAsync((Auction?)null);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAuctionById(invalidId);

        _consoleLogger.LogInformation("Response type: " + response.GetType());

        // Assert
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));

        _consoleLogger.LogInformation("GetAuction_ReturnsBadRequestForInvalidId test completed..");
    }



    //GetAuctionsByStatus Tests

    [TestMethod]
    public async Task GetAuctionsByStatus_ReturnsAuctionsByStatus()
    {
        _consoleLogger.LogInformation("GetAuctionsByStatus_ReturnsAuctionsByStatus test starting...");

        // Arrange
        string auctionStatus = "Ongoing";
        var auctions = new List<Auction>
            {
                new Auction { Id = "1", StartDate = DateTime.Now.AddDays(3), StartPrice = 200, Status = AuctionStatus.Ongoing},
                new Auction { Id = "2", StartDate = DateTime.Now.AddDays(4), StartPrice = 1000, Status = AuctionStatus.Ongoing}
            };

        // Setup the mock to return the expected auctions
        _auctionRepoMock.Setup(repo => repo.GetAuctionsByStatus(auctionStatus))
                        .ReturnsAsync(auctions);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAuctionsByStatus(auctionStatus);

        // Assert
        var okResult = response as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);
        var auctionResult = okResult.Value as List<Auction>;
        Assert.IsNotNull(auctions);
        Assert.AreEqual(auctions.Count, auctionResult.Count);

        _consoleLogger.LogInformation(" GetAuctionsByStatus_ReturnsAuctionsByStatus test completed!");
    }

    [TestMethod]
    public async Task GetAuctionsByStatus_ReturnsNotFoundForInvalidStatus()
    {
        _consoleLogger.LogInformation("GetAuctionsByStatus_ReturnsNotFoundForInvalidStatus test completed...");

        // Arrange
        string NonValidAuctionStatus = "NonValidStatus";

        // Setup the mock to return the expected auctions
        _auctionRepoMock.Setup(repo => repo.GetAuctionsByStatus(NonValidAuctionStatus))
                        .ReturnsAsync((List<Auction>)null);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAuctionsByStatus(NonValidAuctionStatus);

        // Assert
        var notFoundResult = response as NotFoundObjectResult;

        _consoleLogger.LogInformation("GetAuctionsByStatus_ReturnsNotFoundForInvalidStatus test completed!");
    }



    //GetAllAuctions Tests

    [TestMethod]
    public async Task GetAllAuctions_ReturnsListOfAuctions()
    {
        _consoleLogger.LogInformation("GetAllAuctions_ReturnsListOfAuctions test starting...");

        // Arrange
        var auctions = new List<Auction>
            {
                new Auction { Id = "1", StartPrice = 200 },
                new Auction { Id = "2", StartPrice = 1000 }
            };

        // Setup the mock to return the expected auctions
        _auctionRepoMock.Setup(repo => repo.GetAllAuctions()).ReturnsAsync(auctions);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAllAuctions();

        // Assert
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));
        var okResult = response as OkObjectResult;
        Assert.IsNotNull(okResult);
        var auctionsList = okResult.Value as List<Auction>;
        Assert.IsNotNull(auctionsList);
        Assert.AreEqual(2, auctionsList.Count);
        Assert.IsTrue(auctionsList[0].Id == "1" && auctionsList[0].StartPrice == 200);

        _consoleLogger.LogInformation("GetAllAuctions_ReturnsListOfAuctions test completed!");
    }

    [TestMethod]
    public async Task GetAllAuctions_ReturnsNotFoundForNonExistingResource()
    {
        _consoleLogger.LogInformation("GetAllAuctions_ReturnsNotFoundForNonExistingResource test starting...");

        // Setup mock repo to use GetAllAuctions() with an empty/non-existing list
        _auctionRepoMock.Setup(repo => repo.GetAllAuctions()).ReturnsAsync(new List<Auction>());

        // Setting up the controller with the mocked objects 
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var result = await controller.GetAllAuctions();

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        _consoleLogger.LogInformation("GetAllAuctions_ReturnsNotFoundForNonExistingResource test completed!");
    }

    [TestMethod]
    public async Task GetAllAuctions_ReturnsNotFoundForNoAuctions()
    {
        _consoleLogger.LogInformation("GetAllAuctions_ReturnsNotFoundForNoAuctions test completed...");

        // Setup the mock to return the expected auctions
        _auctionRepoMock.Setup(repo => repo.GetAllAuctions())
                        .ReturnsAsync((List<Auction>)null);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.GetAllAuctions();

        // Assert
        var notFoundResult = response as NotFoundObjectResult;

        _consoleLogger.LogInformation("GetAllAuctions_ReturnsNotFoundForNoAuctions test completed!");
    }



    //AddAuction Tests

    [TestMethod]
    public async Task AddAuction_ReturnsOkObjectResult()
    {
        _consoleLogger.LogInformation("AddAuction_ReturnsOkResult test starting...");

        // Arrange
        var newAuction = new Auction { Id = "3", StartPrice = 500, StartDate = new DateTime(2024, 12, 12), EndDate = new DateTime(2024, 12, 13)};

        // Setup mock to return a task when AddAuction is called
        _auctionRepoMock.Setup(repo => repo.AddAuction(It.IsAny<Auction>()))
                        .Returns(Task.CompletedTask)
                        .Verifiable();

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.AddAuction(newAuction);

        // Assert
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));

        _consoleLogger.LogInformation("AddAuction_ReturnsOkResult test completed!");
    }


    //UpdateAuction Tests

    [TestMethod]
    public async Task UpdateAuction_ReturnsOkForValidId()
    {

        _consoleLogger.LogInformation("UpdateAuction_ReturnsOkForValidId test starting...");

        //Arrange
        string auctionId = "664607e52c72ebe1ed6be47a";
        var updatedAuction = new Auction { Id = auctionId, StartPrice = 500 };

        // Setup mock to return the auction by Id
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(auctionId))
                        .ReturnsAsync(new Auction { Id = auctionId, StartPrice = 200 });

        //Setup mock to update the auction
        _auctionRepoMock.Setup(repo => repo.UpdateAuction(auctionId, updatedAuction))
                        .Returns(Task.CompletedTask)
                        .Verifiable();

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        //Act
        var response = await controller.UpdateAuction(auctionId, updatedAuction);

        //Assert
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));

        _consoleLogger.LogInformation("UpdateAuction_ReturnsOkForValidId test completed!");
    }


    [TestMethod]
    public async Task UpdateAuction_ReturnsNotFoundForNonExistingResource()
    {

        _consoleLogger.LogInformation("UpdateAuction_ReturnsNotFoundForNonExistingResource test starting...");

        //Arrange
        string auctionId = "664607e52c72ebe1ed6be47a";
        var updatedAuction = new Auction { Id = auctionId, StartPrice = 500 };

        // Setup the mock to return null, indicating the auction doesn't exist
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(auctionId))
                        .ReturnsAsync((Auction)null);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        //Act
        var response = await controller.UpdateAuction(auctionId, updatedAuction);

        //Assert
        Assert.IsInstanceOfType(response, typeof(NotFoundObjectResult));

        _consoleLogger.LogInformation("UpdateAuction_ReturnsNotFoundForNonExistingResource test completed!");
    }

    [TestMethod]
    public async Task UpdateAuction_ReturnsBadRequestForInvalidId()
    {

        _consoleLogger.LogInformation("UpdateAuction_ReturnsBadRequestForInvalidId test starting...");

        //Arrange
        string InvalidAuctionId = "1234"; //Invalid ID format
        var updatedAuction = new Auction { Id = InvalidAuctionId, StartPrice = 500 };

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        //Act
        var response = await controller.UpdateAuction(InvalidAuctionId, updatedAuction);

        //Assert
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));

        _consoleLogger.LogInformation("UpdateAuction_ReturnsBadRequestForInvalidId test completed!");
    }

    //DeleteAuction Tests

    [TestMethod]
    public async Task DeleteAuction_ReturnsOkResult()
    {
        _consoleLogger.LogInformation("DeleteAuction_ReturnsOkResult test starting...");

        // Arrange
        string auctionId = "664607e52c72ebe1ed6be47a";

        // Create a list of auctions
        var auctions = new List<Auction>
        {
            new Auction { Id = auctionId, StartPrice = 200 },
            new Auction { Id = "2664607e52c72ebe1ed6be47a", StartPrice = 1000 }
        };

        // Setup the mock to return the auction by ID
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(auctionId))
                        .ReturnsAsync(auctions.Find(a => a.Id == auctionId));

        // Setup the mock to delete the auction by ID and remove it from the list
        _auctionRepoMock.Setup(repo => repo.DeleteAuction(auctionId))
                        .Callback(() => auctions.RemoveAll(a => a.Id == auctionId))
                        .Returns(Task.CompletedTask)
                        .Verifiable();

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.DeleteAuction(auctionId);

        // Assert
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));
        Assert.IsNotNull(response);
        Assert.IsTrue(auctions.Count == 1); // Should return 1 auction after deleting one

        _consoleLogger.LogInformation("DeleteAuction_ReturnsOkResult test completed!");
    }

    [TestMethod]
    public async Task DeleteAuction_ReturnsBadRequestForInvalidId()
    {

        _consoleLogger.LogInformation("DeleteAuction_ReturnsBadRequestForInvalidId test starting..");

        // Arrange
        string NonExistingAuctionId = "1234"; //this is a non-existing auction and invalid format

        // Setup the mock to return null, indicating the auction doesn't exist
        _auctionRepoMock.Setup(repo => repo.GetAuctionById(It.IsAny<string>())).ReturnsAsync((Auction)null);

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, null);

        // Act
        var response = await controller.DeleteAuction(NonExistingAuctionId);

        // Assert
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));

        _consoleLogger.LogInformation("DeleteAuction_ReturnsBadRequestForInvalidId test completed!");
    }

    //SendAuctionData Tests

    [TestMethod]
    public async Task SendAuctionData_ReturnsOkResult()
    {
        _consoleLogger.LogInformation("SendAuctionData_ReturnsOkResult test starting...");

        // Arrange
        var auctionBidDTO = new AuctionBidDTO
        {
            Id = "664607e52c72ebe1ed6be47a",
            ProductId = "664607e52c72ebe1ed6be47b",
            AuctionEndDate = DateTime.Today,
            BidderId = "664607e52c72ebe1ed6be47c",
            Status = AuctionStatus.Finished,
            Amount = 500
        };

        // Setup the mock to simulate successful message sending
        var rabbitMQProducerMock = new Mock<IRabbitMQProducer>();
        rabbitMQProducerMock.Setup(producer => producer.CreateMessageAsync(It.IsAny<AuctionBidDTO>()))
                            .Returns(Task.CompletedTask)
                            .Verifiable();

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, _rabbitMQProducerMock.Object);

        // Act
        var response = await controller.SendAuctionData(auctionBidDTO);
        Console.WriteLine(response.GetType().Name);

        // Assert
        Assert.IsInstanceOfType(response, typeof(OkObjectResult));

        _consoleLogger.LogInformation("SendAuctionData_ReturnsOkResult test completed!");
    }

    [TestMethod]
    public async Task SendAuctionData_ReturnsBadRequestForInvalidAuctionBidDTO()
    {
        _consoleLogger.LogInformation("SendAuctionData_ReturnsBadRequestForInvalidAuctionBidDTO test starting...");

        // Arrange
        var InvalidAuctionBidDTO = new AuctionBidDTO
        {
            Id = null, //Null or empty string for ID
            ProductId = "664607e52c72ebe1ed6be47b",
            AuctionEndDate = DateTime.Today,
            BidderId = "664607e52c72ebe1ed6be47c",
            Status = AuctionStatus.Finished,
            Amount = 500
        };

        // Setup the mock to simulate successful message sending
        var _rabbitMQProducerMock = new Mock<IRabbitMQProducer>();

        // Create a new instance of the controller with the mock dependencies
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, _rabbitMQProducerMock.Object);

        // Act
        var response = await controller.SendAuctionData(InvalidAuctionBidDTO);
        Console.WriteLine(response.GetType().Name);

        // Assert
        Assert.IsInstanceOfType(response, typeof(BadRequestObjectResult));

        _consoleLogger.LogInformation("SendAuctionData_ReturnsBadRequestForInvalidAuctionBidDTO test completed.");
    }

    [TestMethod]
    public async Task SendAuctionData_IntegrationTest()
    {
        _consoleLogger.LogInformation("SendAuctionData_IntegrationTest test starting...");

        // Arrange
        var auctionBidDTO = new AuctionBidDTO
        {
            Id = "664607e52c72ebe1ed6be47a",
            ProductId = "664607e52c72ebe1ed6be47b",
            AuctionEndDate = DateTime.Today,
            BidderId = "664607e52c72ebe1ed6be47c",
            Status = AuctionStatus.Finished,
            Amount = 500
        };

        // Act
        var controller = new AuctionController(_loggerMock.Object, _auctionRepoMock.Object, _rabbitMQProducer);
        var response = await controller.SendAuctionData(auctionBidDTO);

        // Assert
        Assert.IsInstanceOfType(response, typeof(ObjectResult));

        _consoleLogger.LogInformation("SendAuctionData_IntegrationTest test completed!");
    }
}