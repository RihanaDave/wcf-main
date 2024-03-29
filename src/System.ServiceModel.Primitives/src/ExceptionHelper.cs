// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ServiceModel.Channels;

namespace System.ServiceModel
{
    internal class ExceptionHelper
    {
        public static Exception AsError(Exception exception)
        {
            return exception;
        }

        public static PlatformNotSupportedException PlatformNotSupported()
        {
            return new PlatformNotSupportedException();
        }

        public static PlatformNotSupportedException PlatformNotSupported(string message)
        {
            return new PlatformNotSupportedException(message);
        }

        public static Exception CreateMaxReceivedMessageSizeExceededException(long maxMessageSize)
        {
            return MaxMessageSizeStream.CreateMaxReceivedMessageSizeExceededException(maxMessageSize);
        }
    }
}
