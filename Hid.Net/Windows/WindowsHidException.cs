using System;

namespace Hid.Net
{
    public class WindowsHidException : Exception
    {
        public WindowsHidException(string message) : base(message)
        {

        }
    }
}
