using GameLexicon.Domain.Entries;

namespace GameLexicon.Domain.Tests.Entries;

public sealed class SentenceExampleTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_AllowsManualExampleWithoutCapture()
    {
        var example = CreateExample(captureId: null, ocrRegionId: null);

        Assert.Null(example.CaptureId);
        Assert.Null(example.OcrRegionId);
        Assert.Equal("Get out", example.TargetText);
    }

    [Fact]
    public void Constructor_AllowsCaptureWithoutOcrRegion()
    {
        var captureId = Guid.NewGuid();

        var example = CreateExample(captureId: captureId, ocrRegionId: null);

        Assert.Equal(captureId, example.CaptureId);
        Assert.Null(example.OcrRegionId);
    }

    [Fact]
    public void Constructor_AllowsCaptureAndOcrRegion()
    {
        var captureId = Guid.NewGuid();
        var ocrRegionId = Guid.NewGuid();

        var example = CreateExample(captureId: captureId, ocrRegionId: ocrRegionId);

        Assert.Equal(captureId, example.CaptureId);
        Assert.Equal(ocrRegionId, example.OcrRegionId);
    }

    [Fact]
    public void Constructor_RejectsOcrRegionWithoutCapture()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateExample(captureId: null, ocrRegionId: Guid.NewGuid()));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Constructor_RejectsEmptySourceIdentifier(
        bool emptyCapture,
        bool emptyOcrRegion)
    {
        Assert.Throws<ArgumentException>(() => CreateExample(
            captureId: emptyCapture ? Guid.Empty : Guid.NewGuid(),
            ocrRegionId: emptyOcrRegion ? Guid.Empty : null));
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => CreateExample(id: Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingSentenceText(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateExample(sentenceText: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingNormalizedSentence(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateExample(normalizedSentence: value!));
    }

    [Theory]
    [InlineData("Get out now", 0, 3, "Get")]
    [InlineData("Please get out", 7, 3, "get")]
    [InlineData("Please get out", 11, 3, "out")]
    [InlineData("Please get out now", 7, 7, "get out")]
    public void Constructor_UsesUtf16TargetRange(
        string text,
        int start,
        int length,
        string expected)
    {
        var example = CreateExample(
            sentenceText: text,
            normalizedSentence: text.ToLowerInvariant(),
            targetStart: start,
            targetLength: length);

        Assert.Equal(expected, example.TargetText);
    }

    [Fact]
    public void Constructor_UsesUtf16IndicesAfterNonBmpCharacter()
    {
        const string text = "🎮 Get out now";

        var example = CreateExample(
            sentenceText: text,
            normalizedSentence: "🎮 get out now",
            targetStart: 3,
            targetLength: 7);

        Assert.Equal(2, "🎮".Length);
        Assert.Equal("Get out", example.TargetText);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    [InlineData(0, -1)]
    [InlineData(11, 1)]
    [InlineData(10, 2)]
    public void Constructor_RejectsInvalidTargetRange(int start, int length)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateExample(
            sentenceText: "Get out now",
            normalizedSentence: "get out now",
            targetStart: start,
            targetLength: length));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    public void Constructor_RejectsRangeThatSplitsSurrogatePair(int start, int length)
    {
        Assert.Throws<ArgumentException>(() => CreateExample(
            sentenceText: "🎮 go",
            normalizedSentence: "🎮 go",
            targetStart: start,
            targetLength: length));
    }

    [Fact]
    public void Constructor_RejectsNonUtcCreatedAt()
    {
        Assert.Throws<ArgumentException>(() => CreateExample(
            createdAt: CreatedAt.ToOffset(TimeSpan.FromHours(8))));
    }

    [Fact]
    public void Constructor_PreservesOptionalFields()
    {
        var example = CreateExample(
            screenshotCropPath: " ",
            gameTitle: null);

        Assert.Equal(" ", example.ScreenshotCropPath);
        Assert.Null(example.GameTitle);
        Assert.Equal(CreatedAt, example.CreatedAt);
    }

    [Fact]
    public void UpdateTextAndTarget_StoresProvidedValuesWithoutNormalizingOrRelocating()
    {
        var example = CreateExample();

        example.UpdateTextAndTarget(
            "Again Get Out now",
            "CUSTOM NORMALIZED",
            6,
            7,
            "new/path",
            "New Game");

        Assert.Equal("Again Get Out now", example.SentenceText);
        Assert.Equal("CUSTOM NORMALIZED", example.NormalizedSentence);
        Assert.Equal(6, example.TargetStart);
        Assert.Equal(7, example.TargetLength);
        Assert.Equal("Get Out", example.TargetText);
        Assert.Equal("new/path", example.ScreenshotCropPath);
        Assert.Equal("New Game", example.GameTitle);
    }

    [Fact]
    public void UpdateTextAndTarget_InvalidRangeLeavesObjectUnchanged()
    {
        var example = CreateExample();

        Assert.ThrowsAny<ArgumentException>(() => example.UpdateTextAndTarget(
            "Changed",
            "changed",
            7,
            1,
            "changed/path",
            "Changed Game"));

        Assert.Equal("Get out now", example.SentenceText);
        Assert.Equal("get out now", example.NormalizedSentence);
        Assert.Equal(0, example.TargetStart);
        Assert.Equal(7, example.TargetLength);
        Assert.Equal("crop/path", example.ScreenshotCropPath);
        Assert.Equal("Game", example.GameTitle);
    }

    private static SentenceExample CreateExample(
        Guid? id = null,
        Guid? captureId = null,
        Guid? ocrRegionId = null,
        string sentenceText = "Get out now",
        string normalizedSentence = "get out now",
        int targetStart = 0,
        int targetLength = 7,
        string? screenshotCropPath = "crop/path",
        string? gameTitle = "Game",
        DateTimeOffset? createdAt = null) =>
        new(
            id ?? Guid.NewGuid(),
            captureId,
            ocrRegionId,
            sentenceText,
            normalizedSentence,
            targetStart,
            targetLength,
            screenshotCropPath,
            gameTitle,
            createdAt ?? CreatedAt);
}
