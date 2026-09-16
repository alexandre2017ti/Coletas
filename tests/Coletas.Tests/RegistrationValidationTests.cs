using Coletas.Domain.Identity;

namespace Coletas.Tests;

public sealed class RegistrationValidationTests
{
    [Theory]
    [InlineData("person@example.test", true)]
    [InlineData(" PERSON@EXAMPLE.TEST ", true)]
    [InlineData("person+tag@example.test", true)]
    [InlineData("person@", false)]
    [InlineData("@example.test", false)]
    [InlineData("a@@example.test", false)]
    [InlineData("a b@example.test", false)]
    [InlineData("Name <a@example.test>", false)]
    [InlineData("a@localhost", false)]
    [InlineData(null, false)]
    public void EmailRejectsMalformedAddresses(string? value, bool expected)
        => Assert.Equal(expected, RegistrationValidation.IsEmail(value));

    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("11144477735", true)]
    [InlineData("52998224726", false)]
    [InlineData("00000000000", false)]
    [InlineData("11111111111", false)]
    [InlineData("529982247250", false)]
    [InlineData("5299822472", false)]
    [InlineData("529!98224725", false)]
    [InlineData(null, false)]
    public void CpfRequiresElevenDigitsAndValidCheckDigits(string? value, bool expected)
        => Assert.Equal(expected, RegistrationValidation.IsCpf(value));

    [Theory]
    [InlineData("(65) 99999-9999", true)]
    [InlineData("+55 (65) 3333-4444", true)]
    [InlineData("5565999999999", true)]
    [InlineData("65999999999", true)]
    [InlineData("6533334444", true)]
    [InlineData("659999999999", false)]
    [InlineData("abc65999999999", false)]
    [InlineData("00999999999", false)]
    [InlineData("659999999", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PhoneEnforcesBrazilianLengthAndDigits(string? phone, bool valid)
        => Assert.Equal(valid, RegistrationValidation.IsPhone(phone));

    [Theory]
    [InlineData("abc-1234", true)]
    [InlineData("ABC1234", true)]
    [InlineData("abc1d23", true)]
    [InlineData("AB12345", false)]
    [InlineData("ABC12345", false)]
    [InlineData("ABC1DD3", false)]
    [InlineData("ABC!1234", false)]
    [InlineData(null, false)]
    public void PlateSupportsBothModels(string? plate, bool valid)
        => Assert.Equal(valid, RegistrationValidation.IsPlate(plate));

    [Theory]
    [InlineData("19.131.243/0001-97", true)]
    [InlineData("19131243000197", true)]
    [InlineData("00.000.000/E08G-12", true)]
    [InlineData("00000000e08g12", true)]
    [InlineData("19131243000198", false)]
    [InlineData("00000000000000", false)]
    [InlineData("11111111111111", false)]
    [InlineData("123", false)]
    [InlineData("19131243000197X", false)]
    [InlineData("19!131243000197", false)]
    [InlineData(null, false)]
    public void CnpjChecksBothDigitsAndAlphaFormat(string? cnpj, bool valid)
        => Assert.Equal(valid, RegistrationValidation.IsCnpj(cnpj));

    [Fact]
    public void StorageValuesDoNotContainPresentationPunctuation()
    {
        Assert.Equal("65999999999", RegistrationValidation.NormalizePhone("+55 (65) 99999-9999"));
        Assert.Equal("ABC1234", RegistrationValidation.NormalizePlate("abc-1234"));
        Assert.Equal("19131243000197", RegistrationValidation.NormalizeCnpj("19.131.243/0001-97"));
    }
}
