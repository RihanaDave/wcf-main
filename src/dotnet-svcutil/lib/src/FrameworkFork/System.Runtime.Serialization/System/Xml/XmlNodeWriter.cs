// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Xml
{
    internal abstract class XmlNodeWriter
    {
        public abstract void Flush();
        public virtual Task FlushAsync()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(NotImplemented.ByDesign);
        }
        public abstract void Close();
        public abstract void WriteDeclaration();
        public abstract void WriteComment(string text);
        public abstract void WriteCData(string text);
        public abstract void WriteStartElement(string prefix, string localName);
        public virtual Task WriteStartElementAsync(string prefix, string localName)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(NotImplemented.ByDesign);
        }
        public virtual void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteStartElement(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        }
        public abstract void WriteStartElement(string prefix, XmlDictionaryString localName);
        public abstract void WriteEndStartElement(bool isEmpty);
        public virtual Task WriteEndStartElementAsync(bool isEmpty)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(NotImplemented.ByDesign);
        }
        public abstract void WriteEndElement(string prefix, string localName);
        public virtual Task WriteEndElementAsync(string prefix, string localName)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(NotImplemented.ByDesign);
        }
        public virtual void WriteEndElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteEndElement(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        }
        public abstract void WriteXmlnsAttribute(string prefix, string ns);
        public virtual void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength)
        {
            WriteXmlnsAttribute(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(nsBuffer, nsOffset, nsLength));
        }
        public abstract void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns);
        public abstract void WriteStartAttribute(string prefix, string localName);
        public virtual void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteStartAttribute(Encoding.UTF8.GetString(prefixBuffer, prefixOffset, prefixLength), Encoding.UTF8.GetString(localNameBuffer, localNameOffset, localNameLength));
        }
        public abstract void WriteStartAttribute(string prefix, XmlDictionaryString localName);
        public abstract void WriteEndAttribute();
        public virtual Task WriteEndAttributeAsync()
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(NotImplemented.ByDesign);
        }
        public abstract void WriteCharEntity(int ch);
        public abstract void WriteEscapedText(string value);
        public abstract void WriteEscapedText(XmlDictionaryString value);
        public abstract void WriteEscapedText(char[] chars, int offset, int count);
        public abstract void WriteEscapedText(byte[] buffer, int offset, int count);
        public abstract void WriteText(string value);
        public abstract void WriteText(XmlDictionaryString value);
        public abstract void WriteText(char[] chars, int offset, int count);
        public abstract void WriteText(byte[] buffer, int offset, int count);
        public abstract void WriteInt32Text(int value);
        public abstract void WriteInt64Text(Int64 value);
        public abstract void WriteBoolText(bool value);
        public abstract void WriteUInt64Text(UInt64 value);
        public abstract void WriteFloatText(float value);
        public abstract void WriteDoubleText(double value);
        public abstract void WriteDecimalText(decimal value);
        public abstract void WriteDateTimeText(DateTime value);
        public abstract void WriteUniqueIdText(UniqueId value);
        public abstract void WriteTimeSpanText(TimeSpan value);
        public abstract void WriteGuidText(Guid value);
        public abstract void WriteStartListText();
        public abstract void WriteListSeparator();
        public abstract void WriteEndListText();
        public abstract void WriteBase64Text(byte[] trailBuffer, int trailCount, byte[] buffer, int offset, int count);
        public virtual Task WriteBase64TextAsync(byte[] trailBuffer, int trailCount, byte[] buffer, int offset, int count)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(NotImplemented.ByDesign);
        }
        public abstract void WriteQualifiedName(string prefix, XmlDictionaryString localName);
    }
}
