using GameLexicon.Domain.Entries;

namespace GameLexicon.Application.Entries.Queries;

public sealed class SentenceExampleDetails
{
    public SentenceExampleDetails(
        SentenceExample example,
        EntryExampleLink link)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(link);

        if (example.Id != link.ExampleId)
        {
            throw new ArgumentException(
                "Example and link identifiers must match.",
                nameof(link));
        }

        EntryId = link.EntryId;
        Id = example.Id;
        CaptureId = example.CaptureId;
        OcrRegionId = example.OcrRegionId;
        SentenceText = example.SentenceText;
        NormalizedSentence = example.NormalizedSentence;
        TargetStart = example.TargetStart;
        TargetLength = example.TargetLength;
        ScreenshotCropPath = example.ScreenshotCropPath;
        GameTitle = example.GameTitle;
        CreatedAt = example.CreatedAt;
        IsPrimary = link.IsPrimary;
        SortOrder = link.SortOrder;
        TargetText = example.TargetText;
    }

    public Guid EntryId { get; }
    public Guid Id { get; }
    public Guid? CaptureId { get; }
    public Guid? OcrRegionId { get; }
    public string SentenceText { get; }
    public string NormalizedSentence { get; }
    public int TargetStart { get; }
    public int TargetLength { get; }
    public string? ScreenshotCropPath { get; }
    public string? GameTitle { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsPrimary { get; }
    public int SortOrder { get; }
    public string TargetText { get; }
}
