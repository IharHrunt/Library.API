using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System;

namespace Library.API.Services
{
    public class SNSPublishService
    {
        public static AmazonSimpleNotificationServiceClient SNSClient { get; set; }

        public static ListTopicsRequest ListOfTopicsRequest { get; set; }

        public SNSPublishService(string serverName)
        {
            try
            {
                // null-coalescing-operator
                SNSClient = SNSClient ?? new AmazonSimpleNotificationServiceClient();
                ListOfTopicsRequest = ListOfTopicsRequest ?? new ListTopicsRequest();
            }
            catch (Exception ex)
            {
                // Logger.logException(ex.Message, ex);
            }
        }

        public void PublishMessageToTopic(string subject, string message, string topicArn)
        {
            SNSClient.PublishAsync(new PublishRequest()
            {
                Subject = subject,
                Message = message,
                TopicArn = topicArn
            });
        }

        public void SubscribeTopic(string topicArn, string protocol, string endPoint)
        {
            SNSClient.SubscribeAsync(new SubscribeRequest()
            {
                TopicArn = topicArn,
                Protocol = protocol,
                Endpoint = endPoint
            });
        }
    }
}