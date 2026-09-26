namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Waits for a condition that background work makes true.
/// </summary>
public static class Eventually
{
    /// <summary>Polls <paramref name="condition"/> until true or fails after <paramref name="timeout"/> (default 5s).</summary>
    public static async Task TrueAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail("Condition not met in time.");
            }

            await Task.Delay(10);
        }
    }

    /// <summary>Gives background work a moment, then asserts the condition still holds.</summary>
    public static async Task StillTrueAsync(Func<bool> condition)
    {
        await Task.Delay(150);
        Assert.True(condition());
    }
}
