using BrainRotDoctor.App.Runtime;
using Xunit;

namespace BrainRotDoctor.App.Tests;

public sealed class UiStartRetryTests
{
    [Fact]
    public void IsNativeLibraryLoadFailure_accepts_dll_was_not_found() =>
        Assert.True(UiStartRetry.IsNativeLibraryLoadFailure(new DllNotFoundException("Dll was not found.")));

    [Fact]
    public void IsNativeLibraryLoadFailure_accepts_a_type_initializer_wrapping_it() =>
        Assert.True(UiStartRetry.IsNativeLibraryLoadFailure(
            new TypeInitializationException("SkiaSharp.SKImageInfo", new DllNotFoundException("Dll was not found."))));

    [Fact]
    public void IsNativeLibraryLoadFailure_rejects_an_ordinary_failure() =>
        Assert.False(UiStartRetry.IsNativeLibraryLoadFailure(new InvalidOperationException("no")));
}
