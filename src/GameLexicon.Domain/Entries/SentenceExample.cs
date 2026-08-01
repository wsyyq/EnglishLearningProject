namespace GameLexicon.Domain.Entries;

public sealed class SentenceExample
{
    public SentenceExample(
        Guid id,
        Guid? captureId,
        Guid? ocrRegionId,
        string sentenceText,
        string normalizedSentence,
        int targetStart,
        int targetLength,
        string? screenshotCropPath,
        string? gameTitle,
        DateTimeOffset createdAt)
    {
        EntryGuard.NotEmpty(id, nameof(id));
        ValidateSource(captureId, ocrRegionId);
        ValidateTextAndTarget(
            sentenceText,
            normalizedSentence,
            targetStart,
            targetLength);
        EntryGuard.Utc(createdAt, nameof(createdAt));

        Id = id;
        CaptureId = captureId;
        OcrRegionId = ocrRegionId;
        SentenceText = sentenceText;
        NormalizedSentence = normalizedSentence;
        TargetStart = targetStart;
        TargetLength = targetLength;
        ScreenshotCropPath = screenshotCropPath;
        GameTitle = gameTitle;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid? CaptureId { get; }
    public Guid? OcrRegionId { get; }
    public string SentenceText { get; private set; }
    public string NormalizedSentence { get; private set; }
    public int TargetStart { get; private set; }
    public int TargetLength { get; private set; }
    public string? ScreenshotCropPath { get; private set; }
    public string? GameTitle { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public string TargetText => SentenceText.Substring(TargetStart, TargetLength);

    public void UpdateTextAndTarget(
        string sentenceText,
        string normalizedSentence,
        int targetStart,
        int targetLength,
        string? screenshotCropPath,
        string? gameTitle)
    {
        ValidateTextAndTarget(
            sentenceText,
            normalizedSentence,
            targetStart,
            targetLength);

        SentenceText = sentenceText;
        NormalizedSentence = normalizedSentence;
        TargetStart = targetStart;
        TargetLength = targetLength;
        ScreenshotCropPath = screenshotCropPath;
        GameTitle = gameTitle;
    }

    private static void ValidateSource(Guid? captureId, Guid? ocrRegionId)
    {
        if (captureId.HasValue)
        {
            EntryGuard.NotEmpty(captureId.Value, nameof(captureId));
        }

        if (ocrRegionId.HasValue)
        {
            EntryGuard.NotEmpty(ocrRegionId.Value, nameof(ocrRegionId));

            if (!captureId.HasValue)
            {
                throw new ArgumentException(
                    "OCR region requires a capture identifier.",
                    nameof(ocrRegionId));
            }
        }
    }

    private static void ValidateTextAndTarget(
        string sentenceText,
        string normalizedSentence,
        int targetStart,
        int targetLength)
    {
        EntryGuard.Required(sentenceText, nameof(sentenceText));
        EntryGuard.Required(normalizedSentence, nameof(normalizedSentence));

        if (targetStart < 0 || targetStart > sentenceText.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetStart),
                "Target start must be within the UTF-16 string.");
        }

        if (targetLength <= 0 || targetLength > sentenceText.Length - targetStart)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetLength),
                "Target length must identify a non-empty UTF-16 range.");
        }

        var targetEnd = targetStart + targetLength;
        if (SplitsSurrogatePair(sentenceText, targetStart) ||
            SplitsSurrogatePair(sentenceText, targetEnd))
        {
            throw new ArgumentException(
                "Target range must not split a UTF-16 surrogate pair.",
                nameof(targetStart));
        }
    }

    private static bool SplitsSurrogatePair(string value, int index) =>
        index > 0 &&
        index < value.Length &&
        char.IsHighSurrogate(value[index - 1]) &&
        char.IsLowSurrogate(value[index]);
}
