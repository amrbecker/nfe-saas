using Reqnroll;

namespace NfeSaas.Tests.BDD.Support;

[Binding]
public class Hooks
{
    private static TestWebApplication? _app;
    public static TestWebApplication App => _app ?? throw new InvalidOperationException("TestWebApplication not initialized");

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _app = new TestWebApplication();
        await _app.InitializeAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_app != null)
            await _app.DisposeAsync();
    }
}
