using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "UrlShortener";
});

// Add DynamoDB
var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

var dynamoDbConfig = builder.Configuration.GetSection("DynamoDb");
var runLocalDynamoDb = dynamoDbConfig.GetValue<bool>("LocalMode");

if (runLocalDynamoDb)
{
    builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
    {
        var clientConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = dynamoDbConfig.GetValue<string>("LocalServiceUrl")
        };
        return new AmazonDynamoDBClient(clientConfig);
    });
}
else
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
