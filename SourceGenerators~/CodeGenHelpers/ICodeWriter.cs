using System;

namespace CodeGenHelpers
{
    public interface ICodeWriter
    {
        void Append(string value);
        void AppendUnindented(string value);
        void NewLine();
        void AppendLine(string value);
        void AppendUnindentedLine(string value);
        IDisposable Block(string value, params string[] constraints);
        IDisposable BlockWithDelimiter(string value, params string[] constraints);
        ICodeWriter BlockWriter(string originalLine);
    }
}
