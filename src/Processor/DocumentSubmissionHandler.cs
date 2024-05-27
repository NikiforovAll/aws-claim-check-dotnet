namespace Processor;

using System.Text;
using System.Text.Json;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Textract;
using Amazon.Textract.Model;
using AWS.Messaging;
using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceDefaults;

public class DocumentSubmissionHandler(
    IAmazonTextract amazonTextractClient,
    IAmazonComprehend amazonComprehendClient,
    IAmazonS3 s3Client,
    IOptions<AwsResources> resources,
    ILogger<DocumentSubmissionHandler> logger
) : IMessageHandler<DocumentSubmission>
{
    public async Task<MessageProcessStatus> HandleAsync(
        MessageEnvelope<DocumentSubmission> messageEnvelope,
        CancellationToken token = default
    )
    {
        logger.LogInformation("Received message - {MessageId}", messageEnvelope.Id);

        var bucketName = resources.Value.DocumentBucketName;
        var key = messageEnvelope.Message.Location;

        var result = await amazonTextractClient.AnalyzeDocumentAsync(
            new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    S3Object = new Amazon.Textract.Model.S3Object
                    {
                        Bucket = bucketName,
                        Name = key
                    }
                },
                FeatureTypes = [FeatureType.FORMS, FeatureType.TABLES]
            },
            token
        );

        var textBlocks = result
            .Blocks.Where(b => b.BlockType == Amazon.Textract.BlockType.LINE)
            .Select(b => b.Text)
            .ToList();

        var keyPhrasesResponse = await amazonComprehendClient.DetectKeyPhrasesAsync(
            new DetectKeyPhrasesRequest
            {
                Text = string.Join('\n', textBlocks),
                LanguageCode = "en"
            },
            token
        );

        var keyPhrases = string.Join(
            '\n',
            keyPhrasesResponse.KeyPhrases.Select(kp => kp.Text.ToLowerInvariant()).Distinct()
        );

        var keyPhrasesBytes = Encoding.UTF8.GetBytes(keyPhrases);

        var kpKey = $"{key}-kp";
        using (var stream = new MemoryStream(keyPhrasesBytes))
        {
            await s3Client.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = kpKey,
                    InputStream = stream
                },
                token
            );
        }

        logger.LogInformation("Saved Document key phrases to {Key}", $"{bucketName}/{kpKey}");

        return MessageProcessStatus.Success();
    }
}
