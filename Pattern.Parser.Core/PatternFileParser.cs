using Microsoft.CodeAnalysis;

using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Pattern.Parser.Core;

public class PatternFileParser
{
    public IEnumerable<Token> ParseFile(string fileName)
    {
        return ParseFile(new FileInfo(fileName));
    }

    public IEnumerable<Token> ParseFile(FileInfo fileInfo)
    {
        return ParseFile(new StreamReader(fileInfo.FullName));
    }

    public IEnumerable<Token> ParseFile(StreamReader streamReader)
    {
        return Array.Empty<Token>();
    }
}

public record Token(string Text, Range TextRange)
{
    public static readonly Token Default = new Token(string.Empty, default);
    public bool IsDefault => this.Equals(Default);
}

public class TokenReader : TextReader, IEnumerable<Token>//, IEnumerator<Token>
{
    private readonly StreamReader _streamReader;
    private readonly string _text;
    private readonly string[] _lines;
    private static readonly Token _defaultToken = Token.Default;

    private int _currentLine = -1;
    private int _currentTokenIndex = 0;
    private Token[] _currentLineTokens = Array.Empty<Token>();

    public TokenReader(Stream stream) : base()
    {
        _streamReader = new StreamReader(stream);
        _text = _streamReader.ReadToEnd();
        _lines = new[] { _text };
    }

    //public Token Current { get; private set; } = _defaultToken;
    //object IEnumerator.Current => Current;

    //public bool MoveNext()
    //{
    //    if (_currentLineTokens.Length is not 0 && _currentTokenIndex < _currentLineTokens.Length)
    //    {
    //        Current = _currentLineTokens[_currentTokenIndex++];
    //    }
    //    else
    //    {
    //        _currentTokenIndex = 0;
    //        _currentLine++;
    //        _currentLineTokens = Array.Empty<Token>();
    //        Current = _defaultToken;

    //        while (_currentLine < _lines.Length &&
    //            _currentLineTokens.Length == 0)
    //        {
    //            _currentLineTokens = TokenizeLine(_lines[_currentLine]).ToArray();

    //            if(_currentLineTokens.Length > 0)
    //            {
    //                Current = _currentLineTokens[_currentTokenIndex++];

    //                return true;
    //            }
    //        }
    //    }

    //    return
    //        _currentTokenIndex < _currentLineTokens.Length &&
    //        _currentLine < _lines.Length;
    //}

    private IEnumerable<Token> TokenizeLine(string line)
    {
        line = line.Trim();

        int startLineIndex = _currentLine;

        if (line is "")
        {
            yield break;
        }

        Index index = 0;
        const string UP_TO_SPACE_PATTERN = @"[^\s\t\n]*[\s\t\n]([/]{2})?";
        const string UP_TO_END_STRING_PATTERN = @".*([^\$\@\""]\"")";
        Regex[] findUpToPatternRegexes = new[] {
            new Regex(UP_TO_SPACE_PATTERN, RegexOptions.Compiled),
            new Regex(UP_TO_END_STRING_PATTERN, RegexOptions.Compiled),
            };
        const string SPLIT_ON_SPACE_PATTERN = @"^[\s\t\n]([/]{2})?";
        const string SPLIT_ON_END_STRING_PATTERN = @"^([^\$\@\""]\"")";
        Regex[] findSplitOnPatternRegexes = new[] {
            new Regex(SPLIT_ON_SPACE_PATTERN, RegexOptions.Compiled),
            new Regex(SPLIT_ON_END_STRING_PATTERN, RegexOptions.Compiled),
            };
        const string START_QUOTE_PATTERN = @"^(\$|@\$|\$@|)\""";
        Regex findQuoteStartRegex = new(START_QUOTE_PATTERN, RegexOptions.Compiled);
        Regex currentUpToRegex = findUpToPatternRegexes[0];
        Regex currentSplitOnRegex = findSplitOnPatternRegexes[0];
        Index currentTokenStart = 0;
        StringBuilder currentToken = new();

        while (index.Value < line.Length)
        {
            if (findQuoteStartRegex.IsMatch(line[currentTokenStart..]))
            {
                currentUpToRegex = findUpToPatternRegexes[1];
                currentSplitOnRegex = findSplitOnPatternRegexes[1];
            }

            string toEndOfLine = line[index..].TrimEnd();

            if (currentSplitOnRegex.IsMatch(toEndOfLine))
            {
                var match = currentSplitOnRegex.Match(toEndOfLine);
                if (currentSplitOnRegex.Equals(findSplitOnPatternRegexes[1]))
                {
                    currentUpToRegex = findUpToPatternRegexes[0];
                    currentSplitOnRegex = findSplitOnPatternRegexes[0];

                    if (startLineIndex != _currentLine)
                    {
                        //var length = _lines[startLineIndex].Length - currentTokenStart.Value;
                        //for (int i = startLineIndex + 1; i < _currentLine; ++i)
                        //{
                        //    length += _lines[i].Length;
                        //}
                        index = match.Index + match.Value.Length;
                    }
                }

                var token = currentToken.ToString().Trim();
                Token newToken = new(
                    token,
                    new Range(currentTokenStart, currentTokenStart.Value + token.Length));
                //tokens.Add(newToken);
                yield return newToken;

                if (currentSplitOnRegex.Match(toEndOfLine).Value == "//")
                {
                    yield return new(toEndOfLine, index..(index.Value + toEndOfLine.Length));
                    index = line.Length;
                    continue;
                }

                currentToken.Clear();

                currentTokenStart = index = Index.FromStart(index.Value + 1);

                if (findQuoteStartRegex.IsMatch(line[currentTokenStart..]))
                {
                    currentUpToRegex = findUpToPatternRegexes[1];
                    currentSplitOnRegex = findSplitOnPatternRegexes[1];
                }
                continue;
            }

            var tokenMatch = currentUpToRegex.Match(toEndOfLine);

            if (tokenMatch.Success)
            {
                currentToken.Append(tokenMatch.Value);
                index = tokenMatch.Index + tokenMatch.Value.Trim().Length;
            }
            else
            {
                int beforeLength = toEndOfLine.Length;
                toEndOfLine = toEndOfLine.Trim();
                index = index.Value + (beforeLength - toEndOfLine.Length);
                currentToken.Append(toEndOfLine);
                //while (line[index]==' ') { index = index.Value + 1 + ; }
                var endIndex = Index.FromStart(index.Value + toEndOfLine.Length);
                yield return new(toEndOfLine, index..endIndex);
                index = endIndex;
                continue;
            }

            if (currentSplitOnRegex.Equals(findSplitOnPatternRegexes[1]))
            {
                int beforeLength = currentToken.ToString().Length;
                var token = currentToken.ToString().Trim();
                currentTokenStart = currentTokenStart.Value + (beforeLength - token.Length);
                var endIndex = Index.FromStart(currentTokenStart.Value + token.Length);
                yield return new(token, currentTokenStart..endIndex);
                index = endIndex;
                continue;
            }
        }
    }

    //public void Reset()
    //{
    //    _streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
    //    _currentLine = -1;
    //    _currentTokenIndex = 0;
    //    _currentLineTokens = Array.Empty<Token>();
    //}

    public IEnumerator<Token> GetEnumerator()
    {
        List<Token> tokens = new List<Token>();
        for(_currentLine=0; _currentLine < _lines.Length; ++_currentLine)
        {
            var line = _lines[_currentLine];
            tokens.AddRange(TokenizeLine(line));
        }
        return tokens.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}