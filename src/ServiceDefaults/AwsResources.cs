namespace ServiceDefaults;

using System.ComponentModel.DataAnnotations;

public class AwsResources
{
    [Required]
    [Url]
    public string DocumentQueueUrl { get; set; } = default!;

    [Required]
    public string? DocumentTopicArn { get; set; }

    [Required]
    public string? DocumentBucketName { get; set; }

}
