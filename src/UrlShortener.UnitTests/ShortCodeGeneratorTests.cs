using UrlShortener.Application;

namespace UrlShortener.UnitTests;

public class ShortCodeGeneratorTests
{
    [Fact]
    public void Generate_uses_seven_characters_from_unambiguous_alphabet()
    {
        var generator = new ShortCodeGenerator();

        for (var i = 0; i < 500; i++)
        {
            var code = generator.Generate();

            Assert.Equal(LinkCodes.AutoLength, code.Length);
            Assert.All(code, character => Assert.Contains(character, LinkCodes.Alphabet));
            Assert.DoesNotContain('0', code);
            Assert.Equal(code.ToUpperInvariant(), code);
        }
    }
}
