using CafeOrders.WebUI.Services;

namespace CafeOrders.Tests;

public sealed class UploadValidationTests
{
    [Theory]
    [InlineData("urun.PNG", "")]
    [InlineData("urun", "image/webp")]
    [InlineData("urun.jpeg", "application/octet-stream")]
    public void IsAllowedImage_AcceptsValidExtensionOrImageContentType(string fileName, string contentType)
    {
        Assert.True(UploadValidation.IsAllowedImage(fileName, contentType));
    }

    [Theory]
    [InlineData("siparis.mp3", "application/octet-stream")]
    [InlineData("siparis", "audio/mpeg")]
    [InlineData("siparis.WAV", "")]
    public void IsAllowedSound_AcceptsValidExtensionOrAudioContentType(string fileName, string contentType)
    {
        Assert.True(UploadValidation.IsAllowedSound(fileName, contentType));
    }

    [Theory]
    [InlineData("payload.exe", "application/octet-stream")]
    [InlineData("notes.txt", "text/plain")]
    public void UploadValidation_RejectsUnknownMedia(string fileName, string contentType)
    {
        Assert.False(UploadValidation.IsAllowedImage(fileName, contentType));
        Assert.False(UploadValidation.IsAllowedSound(fileName, contentType));
    }

    [Fact]
    public void ResolveExtensions_UsesContentTypeFallbackWhenFileNameHasNoExtension()
    {
        Assert.Equal(".webp", UploadValidation.ResolveImageExtension("image", "image/webp"));
        Assert.Equal(".m4a", UploadValidation.ResolveSoundExtension("sound", "audio/mp4"));
    }
}
