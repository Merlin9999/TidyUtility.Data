﻿ #nullable disable
  namespace TidyUtility.Data.Storage
{
    [Serializable]
    public class SnapshotNotFoundException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SnapshotNotFoundException()
        {
        }

        public SnapshotNotFoundException(string message) 
            : base(message)
        {
        }

        public SnapshotNotFoundException(string message, Exception inner) 
            : base(message, inner)
        {
        }
    }
}
