using RabbitMQ.Client;

namespace Interfaces;

//Interface for creating RabbitMQ connections
public interface IRabbitMQConnectionFactory
{
    //Creates a RabbitMQ connection
    IConnection CreateConnection();
}