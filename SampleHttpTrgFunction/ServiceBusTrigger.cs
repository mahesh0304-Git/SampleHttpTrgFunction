using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleHttpTrgFunction;

public class ServiceBusTrigger
{
    private readonly ILogger<ServiceBusTrigger> _logger;

    public ServiceBusTrigger(ILogger<ServiceBusTrigger> logger)
    {
        _logger = logger;
    }

    //[Function(nameof(ServiceBusTrigger))]
    //public async Task Run(
    //    [ServiceBusTrigger("simplemessagequeue", Connection = "ServiceBusConnection", AutoCompleteMessages = true)]
    //    ServiceBusReceivedMessage message,
    //    ServiceBusMessageActions messageActions)
    //{
    //    _logger.LogInformation("Message ID: {id}", message.MessageId);
    //    _logger.LogInformation("Message Body: {body}", message.Body);
    //    _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

    //    // Complete the message
    //    await messageActions.CompleteMessageAsync(message);
    //}

    [Function("BatchServiceBusTrigger")]
    public async Task RunBatch(
        [ServiceBusTrigger("simplemessagequeue", Connection = "ServiceBusConnection", 
                            IsBatched = true)]
        ServiceBusReceivedMessage[] messages,
        ServiceBusMessageActions messageActions)
               
    {
        foreach (var message in messages)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}