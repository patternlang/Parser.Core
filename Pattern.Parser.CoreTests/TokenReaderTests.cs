using Xunit;
using Xunit.Abstractions;
using Pattern.Parser.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FluentAssertions;

namespace Pattern.Parser.Core.Tests
{
    public class TokenReaderTests
    {
        public ITestOutputHelper OutputHelper { get; }

        public TokenReaderTests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        [Fact()]
        public void TokenReaderTest()
        {

        }

        [Theory]
        [InlineData("One", 1)]
        [InlineData("\"One\"", 1)]
        [InlineData("'One'", 1)]
        [InlineData("$\"{One}\"", 1)]
        [InlineData("@$\"{One}\"", 1)]
        [InlineData("$@\"{One}\"", 1)]
        [InlineData("One Two", 2)]
        [InlineData("\"One\" Two", 2)]
        [InlineData("'One' Two", 2)]
        [InlineData("$\"{One}\" Two", 2)]
        [InlineData("@$\"{One}\" Two", 2)]
        [InlineData("$@\"{One}\" Two", 2)]
        [InlineData("'One Two'", 2)]
        [InlineData("$\"{One} Two\"", 1)]
        [InlineData("@$\"{One} Two\"", 1)]
        [InlineData("$@\"{One} Two\"", 1)]
        [InlineData("One Two Three", 3)]
        [InlineData("'One Two Three'", 3)]
        [InlineData("$\"{One} Two\" Three", 2)]
        [InlineData("@$\"{One} Two\" Three", 2)]
        [InlineData("$@\"{One} Two\" Three", 2)]
        [InlineData(@"$@""{One} Two""
Three", 2)]
        [InlineData(@"$@""{One} Two""

Three", 2)]
        [InlineData(@"
$@""{One} Two""
Three", 2)]
        [InlineData(@"
$@""{One}
Two""

Three", 2)]
        public void MoveNextTest(string toParse, int expectedTokenCount)
        {
            var stream = new MemoryStream(Encoding.Default.GetBytes(toParse));

            var reader = new TokenReader(stream);

            var tokens = reader.ToList();

            tokens.Should().NotBeNull();

            tokens.ForEach(t => OutputHelper.WriteLine(t.ToString()));
            tokens.ForEach(t => {
                var expectedLength = t.TextRange.GetOffsetAndLength(toParse.Length).Length;
                t.Text.Length.Should().Be(expectedLength);
                t.Text.Should().Be(toParse[t.TextRange]);
            });

            tokens.Count.Should().Be(expectedTokenCount);
        }

        [Fact()]
        public void ResetTest()
        {

        }
    }
}