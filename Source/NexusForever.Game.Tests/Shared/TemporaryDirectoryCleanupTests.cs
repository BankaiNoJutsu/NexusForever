using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Shared;

public class TemporaryDirectoryCleanupTests
{
    [Theory]
    [InlineData(32)] [InlineData(33)]
    public void ATransientWindowsSharingViolationCanSettleWithoutLosingCleanup(int code)
    {
        int attempts = 0, delays = 0;
        TemporaryDirectoryCleanup.Run(() =>
        {
            if (++attempts < 3) throw new IOException("Sharing violation", unchecked((int)0x80070000) | code);
        }, () => TimeSpan.FromMilliseconds(delays * 25), () => ++delays, true);
        Assert.Equal(3, attempts); Assert.Equal(2, delays);
    }

    [Fact]
    public void ASharingViolationThatOutlastsTheGracePeriodStillFails()
    {
        int attempts = 0, delays = 0;
        var original = new IOException("Persistent sharing violation", unchecked((int)0x80070020));
        Assert.Same(original, Assert.Throws<IOException>(() => TemporaryDirectoryCleanup.Run(() => { ++attempts; throw original; },
            () => TimeSpan.FromSeconds(delays), () => ++delays, true)));
        Assert.InRange(attempts, 1, 3); Assert.InRange(delays, 0, 2);
    }

    [Theory]
    [InlineData(5, true)] [InlineData(112, true)] [InlineData(32, false)]
    public void OtherIoErrorsAndNonWindowsErrorsAreNeverRetried(int code, bool windows)
    {
        int attempts = 0;
        var original = new IOException("Different I/O error", unchecked((int)0x80070000) | code);
        Assert.Same(original, Assert.Throws<IOException>(() => TemporaryDirectoryCleanup.Run(() => { ++attempts; throw original; },
            () => TimeSpan.Zero, () => Assert.Fail("Unexpected retry"), windows)));
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void AuthorizationErrorsPropagateWithoutRetry()
    {
        var original = new UnauthorizedAccessException();
        Assert.Same(original, Assert.Throws<UnauthorizedAccessException>(() => TemporaryDirectoryCleanup.Run(() => throw original,
            () => TimeSpan.Zero, () => Assert.Fail("Unexpected retry"), true)));
    }
}
