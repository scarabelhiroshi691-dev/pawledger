namespace PetShop.Domain.Members;

public sealed record LedgerEntry(
    Guid Id,
    Guid AccountId,
    FundBucket Bucket,
    LedgerEntryType Type,
    decimal Amount,
    Guid SourceDocumentId,
    Guid? OriginalEntryId,
    Guid PerformedByUserId,
    string RequestId,
    DateTimeOffset OccurredAt);
