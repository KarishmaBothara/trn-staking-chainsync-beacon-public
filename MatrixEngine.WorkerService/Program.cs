using MatrixEngine.WorkerService;

var builder = Host.CreateDefaultBuilder(args);

IHost host = builder 
    .ConfigureServices((context, services) =>
    {
        services.ConfigureWorkerServices(context.Configuration);
    })
    .Build();

await host.RunAsync();