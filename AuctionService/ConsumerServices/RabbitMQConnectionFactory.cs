using RabbitMQ.Client;
using Interfaces;

// This class represents a concrete implementation of the IRabbitMQConnectionFactory interface.
// By implementing this interface, any other class that depends on IRabbitMQConnectionFactory can use the methods provided by this class.
// Using an interface allows us to create mock objects for testing purposes and promotes loose coupling between components.

namespace ConsumerServices
{
    // The RabbitMQConnectionFactory class is responsible for creating connections to a RabbitMQ server.
    public class RabbitMQConnectionFactory : IRabbitMQConnectionFactory
    {
        private readonly ConnectionFactory _factory;

        // Constructs a new instance of the RabbitMQConnectionFactory class with the specified host name.
        public RabbitMQConnectionFactory(string hostName)
        {
            _factory = new ConnectionFactory { HostName = hostName };
        }

        // Creates a new connection to the RabbitMQ server.
        // Returns an instance of IConnection, which represents the connection to the server.
        public IConnection CreateConnection()
        {
            return _factory.CreateConnection();
        }
    }
}
