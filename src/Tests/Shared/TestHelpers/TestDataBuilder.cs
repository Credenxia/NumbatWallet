using Bogus;

namespace NumbatWallet.Tests.Shared;

/// <summary>
/// Test data builder using Bogus library
/// Generates realistic fake data for testing
/// NOTE: Specific entity builders should be added as needed by individual test projects
/// This provides only common utility methods
/// </summary>
public static class TestDataBuilder
{
    private static readonly Faker Faker = new("en_AU");

    /// <summary>
    /// Create a random valid email address
    /// </summary>
    public static string CreateEmail() => Faker.Internet.Email();

    /// <summary>
    /// Create a random person's name
    /// </summary>
    public static string CreatePersonName() => Faker.Person.FullName;

    /// <summary>
    /// Create a random company name
    /// </summary>
    public static string CreateCompanyName() => Faker.Company.CompanyName();

    /// <summary>
    /// Create a random phone number (Australian format)
    /// </summary>
    public static string CreatePhoneNumber() => Faker.Phone.PhoneNumber("04## ### ###");

    /// <summary>
    /// Create a random Australian address
    /// </summary>
    public static string CreateAddress() => Faker.Address.FullAddress();

    /// <summary>
    /// Create a random date of birth (18-65 years old)
    /// </summary>
    public static DateTime CreateDateOfBirth() => Faker.Date.Past(65 - 18, DateTime.Now.AddYears(-18));

    /// <summary>
    /// Create a random license number
    /// </summary>
    public static string CreateLicenseNumber() => Faker.Random.AlphaNumeric(10).ToUpper();

    /// <summary>
    /// Create a random DID
    /// </summary>
    public static string CreateDid() => $"did:web:numbatwallet.wa.gov.au:{Faker.Random.Guid()}";

    /// <summary>
    /// Create a random GUID
    /// </summary>
    public static Guid CreateGuid() => Faker.Random.Guid();

    /// <summary>
    /// Create random alphanumeric string
    /// </summary>
    public static string CreateAlphanumeric(int length = 10) => Faker.Random.AlphaNumeric(length);

    /// <summary>
    /// Create random sentence
    /// </summary>
    public static string CreateSentence() => Faker.Lorem.Sentence();

    /// <summary>
    /// Create random word
    /// </summary>
    public static string CreateWord() => Faker.Lorem.Word();

    /// <summary>
    /// Get the underlying Faker instance for custom generation
    /// </summary>
    public static Faker GetFaker() => Faker;
}
