namespace SampleHttpTrgFunction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.ServiceBus;
using Azure.Core;

public class HttpTrigger
{
    private readonly ILogger<HttpTrigger> _logger;

    public HttpTrigger(ILogger<HttpTrigger> logger)
    {
        _logger = logger;
    }

    [Function("HttpToServiceBus")]
    public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
    {
        _logger.LogInformation("HTTP trigger function received a request.");

       // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //dynamic data = JsonConvert.DeserializeObject(requestBody);

        string connectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");

        var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender("simplemessagequeue");
        string messageId = Guid.NewGuid().ToString();
        string messageBody = $"Sample Message From Http Trigger With message Id = {messageId}";
        var message = new ServiceBusMessage(messageBody);
        message.MessageId = messageId;
        await sender.SendMessageAsync(message);
        
        return new OkObjectResult($"Message added to Service Bus queue: {messageId}");
    }

    [Function("HttpToServiceBusBatch")]
    public async Task<IActionResult> SendBatch(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
    {
        _logger.LogInformation("HTTP trigger function received a request.");

        // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //dynamic data = JsonConvert.DeserializeObject(requestBody);

        string connectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");

        var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender("simplemessagequeue");
        
        List <string> messagesToSend = new List<string>();
        messagesToSend.Add("First Message From Http Trigger With message Id =");
        messagesToSend.Add("Second Message From Http Trigger With message Id =");
        messagesToSend.Add("Third Message From Http Trigger With message Id =");
        messagesToSend.Add("Fourth Message From Http Trigger With message Id =");
        messagesToSend.Add("Fifth Message From Http Trigger With message Id =");
        Queue<ServiceBusMessage> messages = new Queue<ServiceBusMessage>();
        foreach (var msg in messagesToSend)
        {
            string messageId = Guid.NewGuid().ToString();
            string messageBody = $"{msg} {messageId}";
            var message = new ServiceBusMessage(messageBody);
            message.MessageId = messageId;
            
            messages.Enqueue(new ServiceBusMessage(msg));
        }

        // Keep sending batches until the queue is empty
        while (messages.Count > 0)
        {
            // Create a new batch. The library automatically handles the maximum size constraint.
            ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            // Try to add messages to the batch until it's full or all messages are added
            while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
            {
                // Dequeue the message after it's successfully added to the batch
                messages.Dequeue();
            }

            // Send the current batch of messages
            await sender.SendMessagesAsync(messageBatch);
            Console.WriteLine($"Sent a batch of {messageBatch.Count} messages.");

            // If the loop breaks and there are still messages left, the next iteration will create a new batch.
            // If the very first message is too large for an empty batch, TryAddMessage will return false, 
            // and an exception should be handled.
            if (messageBatch.Count == 0 && messages.Count > 0)
            {
                throw new Exception($"Message is too large to fit in a single batch. Max size: {messageBatch.MaxSizeInBytes} bytes.");
            }
        }
        return new OkObjectResult($"Message added to Service Bus queue in Batch");
    }

}