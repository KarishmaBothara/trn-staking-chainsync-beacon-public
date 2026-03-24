# MatrixEngine Worker Service

This worker service is responsible for running the MatrixEngine.Core system on a daily basis.

## Schedule

The service runs on a daily schedule using the following cron expression:
```
10 02 * * *
```

This means the service runs at 2:10 AM every day. The cron expression breaks down as:
- `10` - At 10 minutes past the hour
- `02` - At 2 AM
- `*` - Every day of the month
- `*` - Every month
- `*` - Every day of the week

## Running the Service

### Development
```bash
dotnet run --project MatrixEngine.WorkerService
```

Make sure all required environment variables are set before running the service. 
