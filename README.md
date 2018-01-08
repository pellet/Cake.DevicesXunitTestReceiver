## Cake.DevicesXunitTestReceiver

Cake Build addin for receiving test results from Devices.Xunit test hosts.

## Installation

Add the following reference to your cake build script:

```
#addin "Cake.DevicesXunitTestReceiver"
```

## Usage

```csharp
var allTestsPassed = LaunchEmbeddedTestsReceiver(port:7777);

await LaunchAndRunTestsOnDevice();

if (!await allTestsPassed)
{
    const string message = "Not all tests passed :(";
    Error(message);

    //We return a bad code so jenkins knows the tests/job failed
    throw new Exception(message);
}
Information("All tests passed :)");
```
