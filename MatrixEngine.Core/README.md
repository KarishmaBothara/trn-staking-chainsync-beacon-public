# Setting up a mongodb instance
### Install mongodb
https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-ubuntu/

### Start the mongodb service
```bash
sudo systemctl start mongod
sudo systemctl enable mongod
```

### Test the mongodb service
```bash
systemctl status mongod
mongosh
```

# Running MatrixEngine.Core
### Install the correct version of dotnet
```bash
./dotnet-install.sh --channel 6.0
```

### Build the project
```bash
dotnet build
```

### Run the project
```bash
dotnet run
```

