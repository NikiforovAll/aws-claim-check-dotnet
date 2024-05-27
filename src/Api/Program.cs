using Amazon.S3;
using Amazon.S3.Model;
using AWS.Messaging;
using Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(
    (context, options) =>
    {
        options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
        options.ValidateOnBuild = true;
    }
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddServiceDefaults();
var awsResources = builder.AddAwsResources();

builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddAWSMessageBus(messageBuilder =>
{
    messageBuilder.AddMessageSource("DocumentSubmissionApi");

    messageBuilder.AddSNSPublisher<DocumentSubmission>(awsResources.DocumentTopicArn);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost(
    "/upload",
    async Task<Results<Created, BadRequest<string>>> (
        IFormFile file,
        [FromServices] IOptions<AwsResources> resources,
        [FromServices] IAmazonS3 s3Client,
        [FromServices] IMessagePublisher publisher,
        [FromServices] TimeProvider timeProvider,
        [FromServices] ILogger<Program> logger
    ) =>
    {
        if (file is null or { Length: 0 })
        {
            return TypedResults.BadRequest("No file uploaded.");
        }

        using var stream = file.OpenReadStream();
        var bucketName = resources.Value.DocumentBucketName;
        var key = Guid.NewGuid().ToString();

        await s3Client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream
            }
        );

        var response = await publisher.PublishAsync(
            new DocumentSubmission { CreatedAt = timeProvider.GetLocalNow(), Location = key }
        );

        logger.LogInformation("Published message with id {MessageId}", response.MessageId);

        return TypedResults.Created();
    }
);

app.Run();
