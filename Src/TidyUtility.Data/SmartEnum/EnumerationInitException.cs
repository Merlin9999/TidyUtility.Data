#nullable enable
namespace TidyUtility.Data.SmartEnum;

[Serializable]
public class EnumerationInitException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public EnumerationInitException()
    {
    }

    public EnumerationInitException(string message) : base(message)
    {
    }

    public EnumerationInitException(string message, Exception inner) : base(message, inner)
    {
    }
}