using System.ComponentModel.DataAnnotations;
using ClinicScheduler.Core.Entities;
using ClinicScheduler.Web.Contracts.Appointments;
using ClinicScheduler.Web.Contracts.Therapists;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace ClinicScheduler.Core.Tests;

/// <summary>
/// Property-based tests for DTO validation attributes.
/// Feature: api-authorization-security
/// </summary>
public class DtoValidationPropertyTests
{
    /// <summary>
    /// Validates a model using DataAnnotations and returns whether it's valid
    /// along with the validation results.
    /// </summary>
    private static (bool IsValid, List<ValidationResult> Results) ValidateModel(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return (isValid, results);
    }

    /// <summary>
    /// Creates an UpdateAppointmentRequest with the given IDs and valid values
    /// for all other required fields so they don't interfere with ID validation.
    /// </summary>
    private static UpdateAppointmentRequest CreateRequest(int patientId, int therapistId, int roomId)
    {
        return new UpdateAppointmentRequest
        {
            PatientId = patientId,
            TherapistId = therapistId,
            RoomId = roomId,
            StartTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Status = AppointmentStatus.Scheduled
        };
    }

    /// <summary>
    /// Constrains a PositiveInt to a valid ID (>= 1).
    /// </summary>
    private static int ToValidId(PositiveInt pi) => pi.Get;

    /// <summary>
    /// Constrains an int to an invalid ID (&lt; 1) by mapping to the range [int.MinValue, 0].
    /// </summary>
    private static int ToInvalidId(int raw) => -(Math.Abs(raw % int.MaxValue));

    // ---------------------------------------------------------------
    // Property 1: UpdateAppointmentRequest ID field validation
    // ---------------------------------------------------------------

    /// <summary>
    /// **Property 1: UpdateAppointmentRequest ID field validation**
    ///
    /// When all IDs (PatientId, TherapistId, RoomId) are >= 1, model validation passes.
    ///
    /// **Validates: Requirements 8.1, 8.2, 8.3**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 1: UpdateAppointmentRequest ID field validation")]
    public bool AllValidIds_ShouldPassValidation(PositiveInt patientId, PositiveInt therapistId, PositiveInt roomId)
    {
        var request = CreateRequest(ToValidId(patientId), ToValidId(therapistId), ToValidId(roomId));
        var (isValid, results) = ValidateModel(request);

        return isValid && results.Count == 0;
    }

    /// <summary>
    /// **Property 1: UpdateAppointmentRequest ID field validation**
    ///
    /// When PatientId is &lt; 1 (0 or negative), model validation fails and reports
    /// an error for the PatientId field.
    ///
    /// **Validates: Requirements 8.1**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 1: UpdateAppointmentRequest ID field validation")]
    public bool InvalidPatientId_ShouldFailValidation(int rawPatientId, PositiveInt therapistId, PositiveInt roomId)
    {
        var invalidPatientId = ToInvalidId(rawPatientId);
        var request = CreateRequest(invalidPatientId, ToValidId(therapistId), ToValidId(roomId));
        var (isValid, results) = ValidateModel(request);

        return !isValid
            && results.Any(r => r.MemberNames.Contains(nameof(UpdateAppointmentRequest.PatientId)));
    }

    /// <summary>
    /// **Property 1: UpdateAppointmentRequest ID field validation**
    ///
    /// When TherapistId is &lt; 1 (0 or negative), model validation fails and reports
    /// an error for the TherapistId field.
    ///
    /// **Validates: Requirements 8.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 1: UpdateAppointmentRequest ID field validation")]
    public bool InvalidTherapistId_ShouldFailValidation(PositiveInt patientId, int rawTherapistId, PositiveInt roomId)
    {
        var invalidTherapistId = ToInvalidId(rawTherapistId);
        var request = CreateRequest(ToValidId(patientId), invalidTherapistId, ToValidId(roomId));
        var (isValid, results) = ValidateModel(request);

        return !isValid
            && results.Any(r => r.MemberNames.Contains(nameof(UpdateAppointmentRequest.TherapistId)));
    }

    /// <summary>
    /// **Property 1: UpdateAppointmentRequest ID field validation**
    ///
    /// When RoomId is &lt; 1 (0 or negative), model validation fails and reports
    /// an error for the RoomId field.
    ///
    /// **Validates: Requirements 8.3**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 1: UpdateAppointmentRequest ID field validation")]
    public bool InvalidRoomId_ShouldFailValidation(PositiveInt patientId, PositiveInt therapistId, int rawRoomId)
    {
        var invalidRoomId = ToInvalidId(rawRoomId);
        var request = CreateRequest(ToValidId(patientId), ToValidId(therapistId), invalidRoomId);
        var (isValid, results) = ValidateModel(request);

        return !isValid
            && results.Any(r => r.MemberNames.Contains(nameof(UpdateAppointmentRequest.RoomId)));
    }

    /// <summary>
    /// **Property 1: UpdateAppointmentRequest ID field validation**
    ///
    /// For any combination of IDs, model validation passes if and only if all IDs >= 1.
    /// This is the bidirectional property that ties the valid and invalid cases together.
    ///
    /// **Validates: Requirements 8.1, 8.2, 8.3**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 1: UpdateAppointmentRequest ID field validation")]
    public bool AnyIdCombination_ValidationMatchesIdValidity(int patientId, int therapistId, int roomId)
    {
        bool allValid = patientId >= 1 && therapistId >= 1 && roomId >= 1;

        var request = CreateRequest(patientId, therapistId, roomId);
        var (isValid, _) = ValidateModel(request);

        return isValid == allValid;
    }

    // ---------------------------------------------------------------
    // Property 2: UpdateTherapistRequest required name validation
    // ---------------------------------------------------------------

    /// <summary>
    /// Creates an UpdateTherapistRequest with the given names and a valid email
    /// so that email validation does not interfere with name validation.
    /// </summary>
    private static UpdateTherapistRequest CreateTherapistRequest(string? firstName, string? lastName)
    {
        return new UpdateTherapistRequest
        {
            FirstName = firstName!,
            LastName = lastName!,
            Email = "valid@example.com"
        };
    }

    /// <summary>
    /// Generates a valid name string: non-whitespace, length between 1 and 100.
    /// Ensures at least one non-whitespace character so [Required] passes.
    /// </summary>
    private static Arbitrary<string> ValidNameArbitrary()
    {
        return Arb.From(
            from len in Gen.Choose(1, 100)
            from chars in Gen.ArrayOf(Gen.Choose(0x21, 0x7E).Select(i => (char)i), len)
            select new string(chars));
    }

    /// <summary>
    /// **Property 2: UpdateTherapistRequest required name validation**
    ///
    /// When both FirstName and LastName are non-whitespace and within 100 characters,
    /// model validation passes.
    ///
    /// **Validates: Requirements 9.1, 9.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 2: UpdateTherapistRequest required name validation")]
    public Property BothValidNames_ShouldPassValidation()
    {
        return Prop.ForAll(ValidNameArbitrary(), ValidNameArbitrary(), (firstName, lastName) =>
        {
            var request = CreateTherapistRequest(firstName, lastName);
            var (isValid, results) = ValidateModel(request);

            return isValid && results.Count == 0;
        });
    }

    /// <summary>
    /// **Property 2: UpdateTherapistRequest required name validation**
    ///
    /// When FirstName is empty, model validation fails and reports an error
    /// for the FirstName field.
    ///
    /// **Validates: Requirements 9.1**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 2: UpdateTherapistRequest required name validation")]
    public Property EmptyFirstName_ShouldFailValidation()
    {
        return Prop.ForAll(ValidNameArbitrary(), validLast =>
        {
            var request = CreateTherapistRequest(string.Empty, validLast);
            var (isValid, results) = ValidateModel(request);

            return !isValid
                && results.Any(r => r.MemberNames.Contains(nameof(UpdateTherapistRequest.FirstName)));
        });
    }

    /// <summary>
    /// **Property 2: UpdateTherapistRequest required name validation**
    ///
    /// When LastName is empty, model validation fails and reports an error
    /// for the LastName field.
    ///
    /// **Validates: Requirements 9.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 2: UpdateTherapistRequest required name validation")]
    public Property EmptyLastName_ShouldFailValidation()
    {
        return Prop.ForAll(ValidNameArbitrary(), validFirst =>
        {
            var request = CreateTherapistRequest(validFirst, string.Empty);
            var (isValid, results) = ValidateModel(request);

            return !isValid
                && results.Any(r => r.MemberNames.Contains(nameof(UpdateTherapistRequest.LastName)));
        });
    }

    /// <summary>
    /// **Property 2: UpdateTherapistRequest required name validation**
    ///
    /// When a name exceeds 100 characters, model validation fails and reports
    /// an error for that field.
    ///
    /// **Validates: Requirements 9.1, 9.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 2: UpdateTherapistRequest required name validation")]
    public Property NameExceeding100Chars_ShouldFailValidation()
    {
        var longNameGen = from len in Gen.Choose(101, 200)
                          from chars in Gen.ArrayOf(Gen.Choose(0x41, 0x5A).Select(i => (char)i), len)
                          select new string(chars);

        return Prop.ForAll(Arb.From(longNameGen), ArbMap.Default.ArbFor<bool>(), (longName, testFirstName) =>
        {
            var validName = "ValidName";

            var request = testFirstName
                ? CreateTherapistRequest(longName, validName)
                : CreateTherapistRequest(validName, longName);

            var (isValid, results) = ValidateModel(request);

            var expectedField = testFirstName
                ? nameof(UpdateTherapistRequest.FirstName)
                : nameof(UpdateTherapistRequest.LastName);

            return !isValid
                && results.Any(r => r.MemberNames.Contains(expectedField));
        });
    }

    /// <summary>
    /// **Property 2: UpdateTherapistRequest required name validation**
    ///
    /// For any combination of FirstName and LastName strings, model validation passes
    /// if and only if both names are non-empty and within 100 characters.
    /// This is the bidirectional property that ties the valid and invalid cases together.
    ///
    /// **Validates: Requirements 9.1, 9.2**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "api-authorization-security")]
    [Trait("Property", "Property 2: UpdateTherapistRequest required name validation")]
    public bool AnyNameCombination_ValidationMatchesNameValidity(string? firstName, string? lastName)
    {
        // A name is valid when it is non-null, non-empty, not whitespace-only,
        // and at most 100 characters. The [Required] attribute rejects whitespace-only
        // strings, and [StringLength(100, MinimumLength = 1)] enforces the length bounds.
        bool firstNameValid = !string.IsNullOrWhiteSpace(firstName) && firstName!.Length <= 100;
        bool lastNameValid = !string.IsNullOrWhiteSpace(lastName) && lastName!.Length <= 100;
        bool bothValid = firstNameValid && lastNameValid;

        var request = CreateTherapistRequest(firstName ?? string.Empty, lastName ?? string.Empty);
        var (isValid, _) = ValidateModel(request);

        return isValid == bothValid;
    }
}
