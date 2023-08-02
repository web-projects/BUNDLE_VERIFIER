namespace Helpers.Clipboard
{
    public enum ResultCode
    {
        Success = 0,

        ErrorOpenClipboard = 1,
        ErrorGlobalAlloc = 2,
        ErrorGlobalLock = 3,
        ErrorSetClipboardData = 4,
        ErrorOutOfMemoryException = 5,
        ErrorArgumentOutOfRangeException = 6,
        ErrorException = 7,
        ErrorInvalidArgs = 8,
        ErrorGetLastError = 9
    };

    public class Result
    {
        public ResultCode ResultCode { get; set; }

        public uint LastError { get; set; }

        public bool OK
        {
            // ReSharper disable once RedundantNameQualifier
            get { return ResultCode.Success == ResultCode; }
        }
    }
}
