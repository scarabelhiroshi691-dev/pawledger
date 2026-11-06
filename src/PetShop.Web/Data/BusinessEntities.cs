using System.ComponentModel.DataAnnotations;

namespace PetShop.Web.Data;

public enum PetSex { Unknown = 0, Male = 1, Female = 2 }
public enum LedgerBucket { Principal = 1, Bonus = 2 }
public enum LedgerKind
{
    RechargePrincipal = 1, RechargeBonus = 2, SpendPrincipal = 3, SpendBonus = 4,
    SpendRefundPrincipal = 5, SpendRefundBonus = 6, RechargeRefundPrincipal = 7,
    RechargeBonusReversal = 8, OpeningBalance = 9
}
public enum PaymentMethod { Cash = 1, WeChat = 2, Alipay = 3, BankCard = 4, Other = 5, None = 9 }
public enum GroomingStatus { Scheduled = 1, Arrived = 2, InService = 3, AwaitingPayment = 4, Completed = 5, Cancelled = 6, NoShow = 7 }
public enum InventoryKind { Opening = 1, Purchase = 2, Sale = 3, Return = 4, Adjustment = 5 }
public enum DocumentType { Grooming = 1, Retail = 2 }
public enum MeituanVoucherStatus { Pending = 1, Redeemed = 2, Revoked = 3, Failed = 4, Unknown = 5 }
public enum MeituanProviderMode { Mock = 1, Official = 2 }
public enum ExpenseCategory { Supplies = 1, Utilities = 2, Rent = 3, Wages = 4, Maintenance = 5, Marketing = 6, Other = 9 }
public enum GroupBuySettlementStatus { Pending = 1, Settled = 2 }

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(30)] public required string Phone { get; set; }
    [MaxLength(500)] public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<Pet> Pets { get; set; } = [];
    public MemberCard? MemberCard { get; set; }
}

public sealed class Pet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(30)] public required string Species { get; set; }
    [MaxLength(60)] public string Breed { get; set; } = string.Empty;
    public PetSex Sex { get; set; }
    public DateOnly? Birthday { get; set; }
    public decimal? WeightKg { get; set; }
    [MaxLength(1000)] public string HealthNotes { get; set; } = string.Empty;
    public DateOnly? VaccineDueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MemberCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    [MaxLength(32)] public required string CardNumber { get; set; }
    public decimal PrincipalBalance { get; set; }
    public decimal BonusBalance { get; set; }
    public int Points { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<LedgerTransaction> Ledger { get; set; } = [];
}

public sealed class LedgerTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberCardId { get; set; }
    public MemberCard MemberCard { get; set; } = null!;
    public LedgerBucket Bucket { get; set; }
    public LedgerKind Kind { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public Guid SourceDocumentId { get; set; }
    public Guid? OriginalDocumentId { get; set; }
    [MaxLength(100)] public required string RequestId { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    [MaxLength(300)] public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RechargeOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MemberCardId { get; set; }
    public MemberCard MemberCard { get; set; } = null!;
    public decimal PrincipalAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    [MaxLength(100)] public required string RequestId { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    [MaxLength(300)] public string Note { get; set; } = string.Empty;
    public decimal PrincipalRefunded { get; set; }
    public decimal BonusReversed { get; set; }
    [MaxLength(100)] public string? ReversalRequestId { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StaffMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string? IdentityUserId { get; set; }
    [MaxLength(50)] public required string Name { get; set; }
    [MaxLength(30)] public string Phone { get; set; } = string.Empty;
    public bool IsGroomer { get; set; }
    public bool IsActive { get; set; } = true;
    public bool LoginEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CashShift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal OpeningCash { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    [MaxLength(450)] public required string OpenedByUserId { get; set; }
    [MaxLength(300)] public string OpeningNote { get; set; } = string.Empty;
    public DateTimeOffset? ClosedAt { get; set; }
    [MaxLength(450)] public string? ClosedByUserId { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal? Difference { get; set; }
    [MaxLength(300)] public string ClosingNote { get; set; } = string.Empty;
    public List<StoreExpense> Expenses { get; set; } = [];
}

public sealed class StoreExpense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CashShiftId { get; set; }
    public CashShift? CashShift { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    [MaxLength(80)] public string Payee { get; set; } = string.Empty;
    [MaxLength(300)] public required string Note { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GroupBuyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(80)] public required string PlatformOrderNumber { get; set; }
    [MaxLength(8)] public string VoucherCodeSuffix { get; set; } = string.Empty;
    [MaxLength(160)] public required string DealName { get; set; }
    [MaxLength(50)] public string CustomerName { get; set; } = string.Empty;
    [MaxLength(50)] public string PetName { get; set; } = string.Empty;
    public Guid? GroomerId { get; set; }
    public StaffMember? Groomer { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ExpectedSettlementAmount { get; set; }
    public decimal? ActualSettlementAmount { get; set; }
    public GroupBuySettlementStatus SettlementStatus { get; set; } = GroupBuySettlementStatus.Pending;
    public DateTimeOffset RedeemedAt { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    [MaxLength(450)] public string? ReconciledByUserId { get; set; }
    [MaxLength(500)] public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public List<GroupBuyRefund> Refunds { get; set; } = [];
}

public sealed class GroupBuyRefund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupBuyRecordId { get; set; }
    public GroupBuyRecord GroupBuyRecord { get; set; } = null!;
    public decimal Amount { get; set; }
    [MaxLength(300)] public required string Reason { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ServiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(80)] public required string Name { get; set; }
    public decimal StandardPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GroomingOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(32)] public required string OrderNumber { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid PetId { get; set; }
    public Pet Pet { get; set; } = null!;
    public Guid ServiceItemId { get; set; }
    public ServiceItem ServiceItem { get; set; } = null!;
    public Guid? RequestedGroomerId { get; set; }
    public Guid? ActualGroomerId { get; set; }
    public StaffMember? ActualGroomer { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public GroomingStatus Status { get; set; }
    public decimal ListPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AmountDue { get; set; }
    [MaxLength(1000)] public string Notes { get; set; } = string.Empty;
    [MaxLength(450)] public required string CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(80)] public required string Name { get; set; }
    [MaxLength(50)] public string Sku { get; set; } = string.Empty;
    [MaxLength(80)] public string Barcode { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal LowStockThreshold { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class InventoryTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public InventoryKind Kind { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal QuantityAfter { get; set; }
    public Guid SourceDocumentId { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    [MaxLength(300)] public string Note { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RetailOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(32)] public required string OrderNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal AmountDue { get; set; }
    [MaxLength(450)] public required string CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<RetailOrderItem> Items { get; set; } = [];
}

public sealed class RetailOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RetailOrderId { get; set; }
    public RetailOrder RetailOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    [MaxLength(80)] public required string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class PaymentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DocumentType DocumentType { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? MemberCardId { get; set; }
    public MemberCard? MemberCard { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal BonusPaid { get; set; }
    public decimal ExternalPaid { get; set; }
    public PaymentMethod ExternalMethod { get; set; }
    public decimal PrincipalRefunded { get; set; }
    public decimal BonusRefunded { get; set; }
    public decimal ExternalRefunded { get; set; }
    [MaxLength(100)] public required string RequestId { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PaymentRefund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentRecordId { get; set; }
    public PaymentRecord PaymentRecord { get; set; } = null!;
    public decimal PrincipalAmount { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal ExternalAmount { get; set; }
    [MaxLength(100)] public required string RequestId { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    [MaxLength(300)] public required string Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AuditLog
{
    public long Id { get; set; }
    [MaxLength(80)] public required string Action { get; set; }
    [MaxLength(50)] public required string EntityType { get; set; }
    [MaxLength(80)] public required string EntityId { get; set; }
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    [MaxLength(2000)] public string Detail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MeituanVoucherRedemption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(64)] public required string VoucherCodeHash { get; set; }
    [MaxLength(32)] public required string VoucherCodeMasked { get; set; }
    [MaxLength(100)] public required string RequestId { get; set; }
    [MaxLength(100)] public string DealId { get; set; } = string.Empty;
    [MaxLength(160)] public string DealName { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal MarketPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public MeituanVoucherStatus Status { get; set; } = MeituanVoucherStatus.Pending;
    public MeituanProviderMode ProviderMode { get; set; } = MeituanProviderMode.Mock;
    [MaxLength(120)] public string ProviderTransactionId { get; set; } = string.Empty;
    [MaxLength(450)] public required string OperatorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    [MaxLength(450)] public string? RevokedByUserId { get; set; }
    [MaxLength(300)] public string RevokeReason { get; set; } = string.Empty;
    [MaxLength(1000)] public string LastProviderMessage { get; set; } = string.Empty;
}
