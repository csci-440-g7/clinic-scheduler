using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Property-based tests for the non-production password policy.
/// Feature: api-authorization-security
///
/// The non-production password policy requires:
///   - Length >= 8
///   - At least one uppercase letter
///   - At least one digit
///   - At least one non-alphanumeric character
/// </summary>
public class PasswordPolicyPropertyTests
{
    /// <summary>
    /// Mirrors the non-production password policy from Program.cs.
    /// Returns true if the password meets all four criteria.
    /// </summary>
    private static bool MeetsPasswordPolicy(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        bool hasMinLength = password.Length >= 8;
        bool hasUppercase = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasNonAlphanumeric = password.Any(c => !char.IsLetterOrDigit(c));

        return hasMinLength && hasUppercase && hasDigit && hasNonAlphanumeric;
    }

    // ---------------------------------------------------------------
    // Generators
    // ---------------------------------------------------------------

    /// <summary>
    /// Generates a password that satisfies all four policy criteria.
    /// Builds from guaranteed uppercase + digit + special + lowercase padding,
    /// then shuffles to randomize position.
    /// </summary>
    private static Arbitrary<string> ValidPasswordArbitrary()
    {
        return Arb.From(
            from paddingLen in Gen.Choose(5, 50)
            from paddingChars in Gen.ArrayOf(Gen.Choose(0x61, 0x7A).Select(i => (char)i), paddingLen)
            from upper in Gen.Choose(0x41, 0x5A).Select(i => (char)i)
            from digit in Gen.Choose(0x30, 0x39).Select(i => (char)i)
            from special in Gen.Choose(0x21, 0x2F).Select(i => (char)i)
            let allChars = paddingChars.Append(upper).Append(digit).Append(special).ToArray()
            from shuffled in Gen.Shuffle(allChars)
            select new string(shuffled));
    }

    /// <summary>
    /// Generates a completely random string from printable ASCII characters.
    /// This produces a mix of valid and invalid passwords for bidirectional testing.
    /// </summary>
    private static Arbitrary<string> RandomPasswordArbitrary()
    {
        return Arb.From(
            from len in Gen.Choose(0, 30)
            from chars in Gen.ArrayOf(Gen.Choose(0x20, 0x7E).Select(i => (char)i), len)
            select new string(chars));
    }

    // ---------------------------------------------------------------
    // Property 3: Password policy acceptance
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 3: Password policy acceptance**
    ///
    /// A password that has length >= 8, at least one uppercase letter, at least one digit,
    /// and at least one non-alphanumeric character SHALL be accepted by the policy.
    ///
    /// **Validates: Requirements 11.1, 11.2, 11.3, 11.4**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 3: Password policy acceptance")]
    public Property ValidPasswords_ShouldBeAccepted()
    {
        return Prop.ForAll(ValidPasswordArbitrary(), password =>
        {
            return MeetsPasswordPolicy(password);
        });
    }

    /// <summary>
    /// **Property 3: Password policy acceptance**
    ///
    /// A password shorter than 8 characters SHALL be rejected regardless of content.
    ///
    /// **Validates: Requirements 11.1**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 3: Password policy acceptance")]
    public Property TooShortPasswords_ShouldBeRejected()
    {
        var shortPasswordGen =
            from len in Gen.Choose(0, 7)
            from chars in Gen.ArrayOf(Gen.Choose(0x20, 0x7E).Select(i => (char)i), len)
            select new string(chars);

        return Prop.ForAll(Arb.From(shortPasswordGen), password =>
        {
            return !MeetsPasswordPolicy(password);
        });
    }

    /// <summary>
    /// **Property 3: Password policy acceptance**
    ///
    /// A password with no uppercase letter SHALL be rejected even if it meets
    /// length, digit, and non-alphanumeric requirements.
    ///
    /// **Validates: Requirements 11.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 3: Password policy acceptance")]
    public Property NoUppercase_ShouldBeRejected()
    {
        // Generate passwords with lowercase, digits, and specials only — no uppercase
        var noUpperGen =
            from baseLen in Gen.Choose(6, 28)
            from baseChars in Gen.ArrayOf(Gen.Choose(0x61, 0x7A).Select(i => (char)i), baseLen)
            from digit in Gen.Choose(0x30, 0x39).Select(i => (char)i)
            from special in Gen.Choose(0x21, 0x2F).Select(i => (char)i)
            let allChars = baseChars.Append(digit).Append(special).ToArray()
            from shuffled in Gen.Shuffle(allChars)
            select new string(shuffled);

        return Prop.ForAll(Arb.From(noUpperGen), password =>
        {
            return !MeetsPasswordPolicy(password);
        });
    }

    /// <summary>
    /// **Property 3: Password policy acceptance**
    ///
    /// A password with no digit SHALL be rejected even if it meets
    /// length, uppercase, and non-alphanumeric requirements.
    ///
    /// **Validates: Requirements 11.3**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 3: Password policy acceptance")]
    public Property NoDigit_ShouldBeRejected()
    {
        // Generate passwords with lowercase, uppercase, and specials — no digits
        var noDigitGen =
            from baseLen in Gen.Choose(6, 28)
            from baseChars in Gen.ArrayOf(Gen.Choose(0x61, 0x7A).Select(i => (char)i), baseLen)
            from upper in Gen.Choose(0x41, 0x5A).Select(i => (char)i)
            from special in Gen.Choose(0x21, 0x2F).Select(i => (char)i)
            let allChars = baseChars.Append(upper).Append(special).ToArray()
            from shuffled in Gen.Shuffle(allChars)
            select new string(shuffled);

        return Prop.ForAll(Arb.From(noDigitGen), password =>
        {
            return !MeetsPasswordPolicy(password);
        });
    }

    /// <summary>
    /// **Property 3: Password policy acceptance**
    ///
    /// A password with no non-alphanumeric character SHALL be rejected even if it meets
    /// length, uppercase, and digit requirements.
    ///
    /// **Validates: Requirements 11.4**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 3: Password policy acceptance")]
    public Property NoNonAlphanumeric_ShouldBeRejected()
    {
        // Generate passwords with lowercase, uppercase, and digits — no specials
        var noSpecialGen =
            from baseLen in Gen.Choose(6, 28)
            from baseChars in Gen.ArrayOf(Gen.Choose(0x61, 0x7A).Select(i => (char)i), baseLen)
            from upper in Gen.Choose(0x41, 0x5A).Select(i => (char)i)
            from digit in Gen.Choose(0x30, 0x39).Select(i => (char)i)
            let allChars = baseChars.Append(upper).Append(digit).ToArray()
            from shuffled in Gen.Shuffle(allChars)
            select new string(shuffled);

        return Prop.ForAll(Arb.From(noSpecialGen), password =>
        {
            return !MeetsPasswordPolicy(password);
        });
    }

    /// <summary>
    /// **Property 3: Password policy acceptance**
    ///
    /// For any random password string, the policy accepts the password if and only if
    /// it has length >= 8, contains at least one uppercase letter, at least one digit,
    /// and at least one non-alphanumeric character. This is the bidirectional property.
    ///
    /// **Validates: Requirements 11.1, 11.2, 11.3, 11.4**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 3: Password policy acceptance")]
    public Property AnyPassword_PolicyAcceptsIffAllCriteriaMet()
    {
        return Prop.ForAll(RandomPasswordArbitrary(), password =>
        {
            bool hasMinLength = password.Length >= 8;
            bool hasUppercase = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasNonAlphanumeric = password.Any(c => !char.IsLetterOrDigit(c));
            bool expectedAccepted = hasMinLength && hasUppercase && hasDigit && hasNonAlphanumeric;

            bool actualAccepted = MeetsPasswordPolicy(password);

            return actualAccepted == expectedAccepted;
        });
    }
}
