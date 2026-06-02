using System.ComponentModel.DataAnnotations;

namespace ValueKu.Core.Enums;

/// <summary>
/// Type of cash account / wallet. CreditCard and Loan are treated as liabilities
/// on the balance sheet; the rest are liquid assets.
/// </summary>
public enum AccountType
{
    Checking,
    Savings,
    Investment,
    [Display(Name = "e-Wallet")]
    EWallet,
    [Display(Name = "Credit Card")]
    CreditCard,
    Loan
}
