using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
	.WithImage("postgres", "16")
	.WithDataVolume("spectrum-adventure-postgres")
	.WithPassword(postgresPassword);
var adventureDb = postgres.AddDatabase("AdventureDb", "adventure");

var storage = builder.AddAzureStorage("storage")
	.ConfigureInfrastructure(infrastructure =>
	{
		var storageAccount = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
		storageAccount.AllowBlobPublicAccess = true;
	})
	.RunAsEmulator(emulator => emulator.WithDataVolume("spectrum-adventure-azurite"));
var visualAssets = storage.AddBlobs("VisualAssets");

var dataProtectionKeyRingPath = Path.Combine(
	Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
	"AI.SpectrumAdventure",
	"data-protection-keys");

builder.AddProject<Projects.AI_SpectrumAdventure_Web>("web")
	.WithReference(adventureDb)
	.WithReference(visualAssets, "AzureStorage")
	.WaitFor(adventureDb)
	.WaitFor(visualAssets)
	.WithEnvironment("AzureStorage__AllowPublicBlobAccess", "true")
	.WithEnvironment("AzureStorage__PublicBlobEndpoint", storage.GetEndpoint("blob"))
	.WithEnvironment("DataProtection__KeyRingPath", dataProtectionKeyRingPath);

builder.Build().Run();