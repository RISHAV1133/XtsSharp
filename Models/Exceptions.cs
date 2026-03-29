using System;

namespace XtsCsharpClient.Models
{
    public class XtsException : Exception
    {
        public int Code { get; }
        public XtsException(string message, int code = 500) : base(message)
        {
            Code = code;
        }
    }

    public class XtsGeneralException : XtsException
    {
        public XtsGeneralException(string message, int code = 500) : base(message, code) { }
    }

    public class XtsTokenException : XtsException
    {
        public XtsTokenException(string message, int code = 400) : base(message, code) { }
    }

    public class XtsPermissionException : XtsException
    {
        public XtsPermissionException(string message, int code = 400) : base(message, code) { }
    }

    public class XtsOrderException : XtsException
    {
        public XtsOrderException(string message, int code = 400) : base(message, code) { }
    }

    public class XtsInputException : XtsException
    {
        public XtsInputException(string message, int code = 400) : base(message, code) { }
    }

    public class XtsDataException : XtsException
    {
        public XtsDataException(string message, int code = 500) : base(message, code) { }
    }

    public class XtsNetworkException : XtsException
    {
        public XtsNetworkException(string message, int code = 500) : base(message, code) { }
    }
}
