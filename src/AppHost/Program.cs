using Amazon;

var builder = DistributedApplication.CreateBuilder(args);

var awsConfig = builder.AddAWSSDKConfig().WithProfile("default").WithRegion(RegionEndpoint.USEast1);

var awsResources = builder
    .AddAWSCloudFormationTemplate("DocumentSubmissionAppResources", "aws-resources.template")
    .WithReference(awsConfig);

builder.AddProject<Projects.Api>("api").WithReference(awsResources);

builder.AddProject<Projects.Processor>("processor").WithReference(awsResources);

builder.Build().Run();
