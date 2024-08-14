using AuctionServiceClassLibrary;

namespace ConsumerServices;
public class BidProcessor
{
    public int ProcessMessageAsync(Bid messageObj)
    {
        if (messageObj == null)
            return -1; // Invalid message

        if (messageObj.DateSent < DateTime.UtcNow.AddMinutes(-5))
            return 0; // Message is too old

        if (messageObj.Id != null)
            return 1; // Save message to database and CSV
        else
            return 1; // Requeue modified message
    }
}