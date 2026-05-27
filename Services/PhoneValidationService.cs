using PhoneNumbers;

namespace SoftflipSolutions.Services;

public interface IPhoneValidationService
{
    bool TryNormalize(string? phone, out string normalizedPhone);
}

public class PhoneValidationService : IPhoneValidationService
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    public bool TryNormalize(string? phone, out string normalizedPhone)
    {
        normalizedPhone = string.Empty;

        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        try
        {
            var parsed = PhoneUtil.Parse(phone.Trim(), null);
            if (!PhoneUtil.IsValidNumber(parsed))
            {
                return false;
            }

            normalizedPhone = PhoneUtil.Format(parsed, PhoneNumberFormat.E164);
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }
}
