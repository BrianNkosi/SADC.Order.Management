var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure containers (managed by Podman / Docker via Aspire) ──

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sqlServer = builder.AddSqlServer("sql", password: sqlPassword)
    .WithDataVolume("sadc-sqlserver-data")
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("DefaultConnection", "SadcOrderManagement");

var rabbitMq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin()
    .WithDataVolume("sadc-rabbitmq-data")
    .WithLifetime(ContainerLifetime.Persistent);

// ── Application services ──

var api = builder.AddProject<Projects.SADC_Order_Management_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq)
    .WithEnvironment("Frontend__Url", "http://localhost:5173");

builder.AddProject<Projects.SADC_Order_Management_Worker>("worker")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(rabbitMq)
    .WaitFor(rabbitMq);

// ── React frontend (npm dev server) ──

builder.AddNpmApp("web", "../Web", "dev")
    .WithReference(api)
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
