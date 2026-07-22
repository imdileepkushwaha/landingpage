namespace SoftflipSolutions.Models;

/// <summary>
/// Same options as the enquiry/demo "Requirement" dropdown on the public index page.
/// </summary>
public static class EnquiryRequirements
{
    public static readonly string[] All =
    [
        "Binary MLM Software",
        "Matrix MLM Software",
        "Unilevel MLM Software",
        "Generation MLM Software",
        "Trading Based MLM Software",
        "ROI Based MLM Software",
        "Centralized MLM Software",
        "De-Centralized MLM Software",
        "Token Based MLM Software",
        "Product Based MLM Software",
        "Ecommerce + MLM Software",
        "School Management Software",
        "Hospital Management Software",
        "OPD + IPD Billing Software",
        "CRM Software",
        "Billing Software",
        "Website Development",
        "Library Management Software",
        "Microfinance Management Software",
        "Multi Recharge Software",
        "Credit Cooperative Software",
        "Real Estate Software",
        "Crypto Based MLM Software",
    ];

    public static bool IsValid(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        All.Any(x => string.Equals(x, name.Trim(), StringComparison.OrdinalIgnoreCase));
}
