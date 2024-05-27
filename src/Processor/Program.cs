using Amazon.Comprehend;
using Amazon.S3;
using Amazon.Textract;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Processor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var awsResources = builder.AddAwsResources();

builder.Services.AddAWSService<IAmazonTextract>();
builder.Services.AddAWSService<IAmazonComprehend>();
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddAWSMessageBus(builder =>
{
    builder.AddSQSPoller(awsResources.DocumentQueueUrl);

    builder.AddMessageHandler<DocumentSubmissionHandler, DocumentSubmission>();
});

builder.Build().Run();
