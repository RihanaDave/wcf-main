﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime;
using System.ServiceModel;
using System.Xml;

namespace System.ServiceModel.Channels
{
    internal abstract class WsrmFault : MessageFault
    {
        private FaultCode code;
        private string exceptionMessage;
        private bool hasDetail;
        private bool isRemote;
        private FaultReason reason;
        private ReliableMessagingVersion reliableMessagingVersion;
        private string subcode;

        // local
        protected WsrmFault(bool isSenderFault, string subcode, string faultReason, string exceptionMessage)
        {
            if (isSenderFault)
            {
                code = new FaultCode("Sender", "");
            }
            else
            {
                code = new FaultCode("Receiver", "");
            }

            this.subcode = subcode;
            reason = new FaultReason(faultReason, CultureInfo.CurrentCulture);
            this.exceptionMessage = exceptionMessage;
            isRemote = false;
        }

        // remote
        protected WsrmFault(FaultCode code, string subcode, FaultReason reason)
        {
            this.code = code;
            this.subcode = subcode;
            this.reason = reason;
            isRemote = true;
        }

        public override FaultCode Code
        {
            get
            {
                return code;
            }
        }

        public override bool HasDetail
        {
            get
            {
                // The SOAP 1.1 requires body processing error information to be placed in a detail element
                // and header processing error information to be placed in a soap fault header.  
                // Since wsrm header faults relate to header processing, the information in the detail is placed in a 
                // soap fault header in the SOAP 1.1 case.  SOAP 1.2 is not so restrictive. Thus, this flag is set
                // in CreateMessage if the SOAP version is 1.2.
                return hasDetail;
            }
        }

        public bool IsRemote
        {
            get
            {
                return isRemote;
            }
        }

        public override FaultReason Reason
        {
            get
            {
                return reason;
            }
        }

        public string Subcode
        {
            get
            {
                return subcode;
            }
        }

        public virtual CommunicationException CreateException()
        {
            string message;

            if (IsRemote)
            {
                message = FaultException.GetSafeReasonText(reason);
                message = SRP.Format(SRP.WsrmFaultReceived, message);
            }
            else
            {
                if (exceptionMessage == null)
                {
                    throw Fx.AssertAndThrow("Exception message must not be accessed unless set.");
                }

                message = exceptionMessage;
            }

            if (code.IsSenderFault)
                return new ProtocolException(message);
            else
                return new CommunicationException(message);
        }

        public static CommunicationException CreateException(WsrmFault fault)
        {
            return fault.CreateException();
        }

        public Message CreateMessage(MessageVersion messageVersion, ReliableMessagingVersion reliableMessagingVersion)
        {
            SetReliableMessagingVersion(reliableMessagingVersion);
            string action = WsrmIndex.GetFaultActionString(messageVersion.Addressing, reliableMessagingVersion);

            if (messageVersion.Envelope == EnvelopeVersion.Soap11)
            {
                code = Get11Code(code, subcode);
            }
            else if (messageVersion.Envelope == EnvelopeVersion.Soap12)
            {
                if (code.SubCode == null)
                {
                    FaultCode subCode = new FaultCode(subcode,
                        WsrmIndex.GetNamespaceString(reliableMessagingVersion));
                    code = new FaultCode(code.Name, code.Namespace, subCode);
                }

                hasDetail = Get12HasDetail();
            }
            else
            {
                throw Fx.AssertAndThrow("Unsupported MessageVersion.");
            }

            Message message = Message.CreateMessage(messageVersion, this, action);
            OnFaultMessageCreated(messageVersion, message);
            return message;
        }

        protected abstract FaultCode Get11Code(FaultCode code, string subcode);
        protected abstract bool Get12HasDetail();

        protected string GetExceptionMessage()
        {
            if (exceptionMessage == null)
            {
                throw Fx.AssertAndThrow("Exception message must not be accessed unless set.");
            }

            return exceptionMessage;
        }

        protected ReliableMessagingVersion GetReliableMessagingVersion()
        {
            if (reliableMessagingVersion == null)
            {
                throw Fx.AssertAndThrow("Reliable messaging version must not be accessed unless set.");
            }

            return reliableMessagingVersion;
        }

        protected abstract void OnFaultMessageCreated(MessageVersion version, Message message);

        protected void SetReliableMessagingVersion(ReliableMessagingVersion reliableMessagingVersion)
        {
            if (reliableMessagingVersion == null)
            {
                throw Fx.AssertAndThrow("Reliable messaging version cannot be set to null.");
            }

            if (this.reliableMessagingVersion != null)
            {
                throw Fx.AssertAndThrow("Reliable messaging version must not be set twice.");
            }

            this.reliableMessagingVersion = reliableMessagingVersion;
        }

        internal void WriteDetail(XmlDictionaryWriter writer)
        {
            OnWriteDetailContents(writer);
        }
    }

    internal class WsrmRequiredFault : WsrmFault
    {
        // local
        public WsrmRequiredFault(string faultReason)
            : base(true, Wsrm11Strings.WsrmRequired, faultReason, null)
        {
        }

        protected override FaultCode Get11Code(FaultCode code, string subcode)
        {
            return new FaultCode(subcode, WsrmIndex.GetNamespaceString(GetReliableMessagingVersion()));
        }

        protected override bool Get12HasDetail()
        {
            return false;
        }

        protected override void OnFaultMessageCreated(MessageVersion version, Message message)
        {
            // do nothing
        }

        protected override void OnWriteDetailContents(XmlDictionaryWriter writer)
        {
            // do nothing
        }
    }

    internal abstract class WsrmHeaderFault : WsrmFault
    {
        private bool faultsInput;
        private bool faultsOutput;
        private UniqueId sequenceID;
        private string subcode;

        // local
        protected WsrmHeaderFault(bool isSenderFault, string subcode, string faultReason, string exceptionMessage,
            UniqueId sequenceID, bool faultsInput, bool faultsOutput)
            : base(isSenderFault, subcode, faultReason, exceptionMessage)
        {
            this.subcode = subcode;
            this.sequenceID = sequenceID;
            this.faultsInput = faultsInput;
            this.faultsOutput = faultsOutput;
        }

        // remote
        protected WsrmHeaderFault(FaultCode code, string subcode, FaultReason reason, XmlDictionaryReader detailReader,
            ReliableMessagingVersion reliableMessagingVersion, bool faultsInput, bool faultsOutput)
            : this(code, subcode, reason, faultsInput, faultsOutput)
        {
            sequenceID = ParseDetail(detailReader, reliableMessagingVersion);
        }

        // remote
        protected WsrmHeaderFault(FaultCode code, string subcode, FaultReason reason, bool faultsInput,
            bool faultsOutput)
            : base(code, subcode, reason)
        {
            this.subcode = subcode;
            this.faultsInput = faultsInput;
            this.faultsOutput = faultsOutput;
        }

        public bool FaultsInput
        {
            get
            {
                return faultsInput;
            }
        }

        public bool FaultsOutput
        {
            get
            {
                return faultsOutput;
            }
        }

        public UniqueId SequenceID
        {
            get
            {
                return sequenceID;
            }
            protected set
            {
                sequenceID = value;
            }
        }

        private static WsrmHeaderFault CreateWsrmHeaderFault(ReliableMessagingVersion reliableMessagingVersion, FaultCode code,
            string subcode, FaultReason reason, XmlDictionaryReader detailReader)
        {
            // Sender faults.
            if (code.IsSenderFault)
            {
                if (subcode == WsrmFeb2005Strings.InvalidAcknowledgement)
                {
                    return new InvalidAcknowledgementFault(code, reason, detailReader, reliableMessagingVersion);
                }
                else if (subcode == WsrmFeb2005Strings.MessageNumberRollover)
                {
                    return new MessageNumberRolloverFault(code, reason, detailReader, reliableMessagingVersion);
                }
                else if (subcode == WsrmFeb2005Strings.UnknownSequence)
                {
                    return new UnknownSequenceFault(code, reason, detailReader, reliableMessagingVersion);
                }
                else if (reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessagingFebruary2005)
                {
                    if (subcode == WsrmFeb2005Strings.LastMessageNumberExceeded)
                    {
                        return new LastMessageNumberExceededFault(code, reason, detailReader, reliableMessagingVersion);
                    }
                }
                else if (reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessaging11)
                {
                    if (subcode == Wsrm11Strings.SequenceClosed)
                    {
                        return new SequenceClosedFault(code, reason, detailReader, reliableMessagingVersion);
                    }
                }
            }

            // Sender or receiver faults.
            if (code.IsSenderFault || code.IsReceiverFault)
            {
                return new SequenceTerminatedFault(code, reason, detailReader, reliableMessagingVersion);
            }

            return null;
        }

        protected override FaultCode Get11Code(FaultCode code, string subcode)
        {
            return code;
        }

        protected override bool Get12HasDetail()
        {
            return true;
        }

        private static void LookupDetailInformation(ReliableMessagingVersion reliableMessagingVersion, string subcode,
            out string detailName, out string detailNamespace)
        {
            detailName = null;
            detailNamespace = null;
            string wsrmNs = WsrmIndex.GetNamespaceString(reliableMessagingVersion);
            bool wsrmFeb2005 = reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessagingFebruary2005;
            bool wsrm11 = reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessaging11;

            if (subcode == WsrmFeb2005Strings.InvalidAcknowledgement)
            {
                detailName = WsrmFeb2005Strings.SequenceAcknowledgement;
                detailNamespace = wsrmNs;
            }
            else if ((subcode == WsrmFeb2005Strings.MessageNumberRollover)
                || (subcode == WsrmFeb2005Strings.SequenceTerminated)
                || (subcode == WsrmFeb2005Strings.UnknownSequence)
                || (wsrmFeb2005 && (subcode == WsrmFeb2005Strings.LastMessageNumberExceeded))
                || (wsrm11 && (subcode == Wsrm11Strings.SequenceClosed)))
            {
                detailName = WsrmFeb2005Strings.Identifier;
                detailNamespace = wsrmNs;
            }
            else
            {
                detailName = null;
                detailNamespace = null;
            }
        }

        protected override void OnFaultMessageCreated(MessageVersion version, Message message)
        {
            if (version.Envelope == EnvelopeVersion.Soap11)
            {
                WsrmSequenceFaultHeader header = new WsrmSequenceFaultHeader(GetReliableMessagingVersion(), this);
                message.Headers.Add(header);
            }
        }

        protected override void OnWriteDetailContents(XmlDictionaryWriter writer)
        {
            WsrmUtilities.WriteIdentifier(writer, GetReliableMessagingVersion(), sequenceID);
        }

        private static UniqueId ParseDetail(XmlDictionaryReader reader, ReliableMessagingVersion reliableMessagingVersion)
        {
            try
            {
                return WsrmUtilities.ReadIdentifier(reader, reliableMessagingVersion);
            }
            finally
            {
                reader.Close();
            }
        }

        public static bool TryCreateFault11(ReliableMessagingVersion reliableMessagingVersion, Message message,
            MessageFault fault, int index, out WsrmHeaderFault wsrmFault)
        {
            if (index == -1)
            {
                wsrmFault = null;
                return false;
            }

            // All wsrm header faults must be sender or receiver faults.
            if (!fault.Code.IsSenderFault && !fault.Code.IsReceiverFault)
            {
                wsrmFault = null;
                return false;
            }

            string subcodeName = WsrmSequenceFaultHeader.GetSubcode(message.Headers.GetReaderAtHeader(index),
                reliableMessagingVersion);

            if (subcodeName == null)
            {
                wsrmFault = null;
                return false;
            }

            string detailName;
            string detailNamespace;

            LookupDetailInformation(reliableMessagingVersion, subcodeName, out detailName, out detailNamespace);

            XmlDictionaryReader detailReader = WsrmSequenceFaultHeader.GetReaderAtDetailContents(detailName,
                detailNamespace, message.Headers.GetReaderAtHeader(index), reliableMessagingVersion);

            if (detailReader == null)
            {
                wsrmFault = null;
                return false;
            }

            wsrmFault = CreateWsrmHeaderFault(reliableMessagingVersion, fault.Code, subcodeName, fault.Reason,
                detailReader);
            if (wsrmFault != null)
            {
                message.Headers.UnderstoodHeaders.Add(message.Headers[index]);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool TryCreateFault12(ReliableMessagingVersion reliableMessagingVersion, Message message,
            MessageFault fault, out WsrmHeaderFault wsrmFault)
        {
            // All wsrm header faults must be sender or receiver faults.
            if (!fault.Code.IsSenderFault && !fault.Code.IsReceiverFault)
            {
                wsrmFault = null;
                return false;
            }

            if ((fault.Code.SubCode == null)
                || (fault.Code.SubCode.Namespace != WsrmIndex.GetNamespaceString(reliableMessagingVersion)) || !fault.HasDetail)
            {
                wsrmFault = null;
                return false;
            }

            string subcodeName = fault.Code.SubCode.Name;
            XmlDictionaryReader detailReader = fault.GetReaderAtDetailContents();
            wsrmFault = CreateWsrmHeaderFault(reliableMessagingVersion, fault.Code, subcodeName, fault.Reason,
                detailReader);

            return (wsrmFault != null);
        }
    }

    internal sealed class InvalidAcknowledgementFault : WsrmHeaderFault
    {
        private readonly SequenceRangeCollection _ranges;

        public InvalidAcknowledgementFault(UniqueId sequenceID, SequenceRangeCollection ranges)
            : base(true, WsrmFeb2005Strings.InvalidAcknowledgement, SRP.InvalidAcknowledgementFaultReason,
            SRP.InvalidAcknowledgementReceived, sequenceID, true, false)
        {
            _ranges = ranges;
        }

        public InvalidAcknowledgementFault(FaultCode code, FaultReason reason,
            XmlDictionaryReader detailReader, ReliableMessagingVersion reliableMessagingVersion)
            : base(code, WsrmFeb2005Strings.InvalidAcknowledgement, reason, true, false)
        {
            UniqueId sequenceId;

            WsrmAcknowledgmentInfo.ReadAck(reliableMessagingVersion, detailReader, out sequenceId, out _ranges,
                out _);

            SequenceID = sequenceId;

            while (detailReader.IsStartElement())
            {
                detailReader.Skip();
            }

            detailReader.ReadEndElement();
        }

        protected override void OnWriteDetailContents(XmlDictionaryWriter writer)
        {
            WsrmFeb2005Dictionary wsrmFeb2005Dictionary = XD.WsrmFeb2005Dictionary;
            ReliableMessagingVersion reliableMessagingVersion = GetReliableMessagingVersion();
            XmlDictionaryString wsrmNs = WsrmIndex.GetNamespace(reliableMessagingVersion);

            writer.WriteStartElement(wsrmFeb2005Dictionary.SequenceAcknowledgement, wsrmNs);
            WsrmAcknowledgmentHeader.WriteAckRanges(writer, reliableMessagingVersion, SequenceID, _ranges);
            writer.WriteEndElement();
        }
    }

    internal sealed class LastMessageNumberExceededFault : WsrmHeaderFault
    {
        public LastMessageNumberExceededFault(UniqueId sequenceID)
            : base(true, WsrmFeb2005Strings.LastMessageNumberExceeded, SRP.LastMessageNumberExceededFaultReason,
            SRP.LastMessageNumberExceeded, sequenceID, false, true)
        {
        }

        public LastMessageNumberExceededFault(FaultCode code, FaultReason reason,
            XmlDictionaryReader detailReader, ReliableMessagingVersion reliableMessagingVersion)
            : base(code, WsrmFeb2005Strings.LastMessageNumberExceeded, reason, detailReader, reliableMessagingVersion, false,
            true)
        {
        }
    }

    internal sealed class MessageNumberRolloverFault : WsrmHeaderFault
    {
        public MessageNumberRolloverFault(UniqueId sequenceID)
            : base(true, WsrmFeb2005Strings.MessageNumberRollover, SRP.MessageNumberRolloverFaultReason,
            SRP.MessageNumberRollover, sequenceID, true, true)
        {
        }

        public MessageNumberRolloverFault(FaultCode code, FaultReason reason, XmlDictionaryReader detailReader,
            ReliableMessagingVersion reliableMessagingVersion)
            : base(code, WsrmFeb2005Strings.MessageNumberRollover, reason, true, true)
        {
            try
            {
                SequenceID = WsrmUtilities.ReadIdentifier(detailReader, reliableMessagingVersion);

                if (reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessaging11)
                {
                    detailReader.ReadStartElement(DXD.Wsrm11Dictionary.MaxMessageNumber,
                        WsrmIndex.GetNamespace(reliableMessagingVersion));

                    string maxMessageNumberString = detailReader.ReadContentAsString();
                    ulong maxMessageNumber;
                    if (!UInt64.TryParse(maxMessageNumberString, out maxMessageNumber)
                        || (maxMessageNumber <= 0))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(
                            SRP.Format(SRP.InvalidSequenceNumber, maxMessageNumber)));
                    }
                    // otherwise ignore value

                    detailReader.ReadEndElement();
                }
            }
            finally
            {
                detailReader.Close();
            }
        }

        protected override void OnWriteDetailContents(XmlDictionaryWriter writer)
        {
            ReliableMessagingVersion reliableMessagingVersion = GetReliableMessagingVersion();
            WsrmUtilities.WriteIdentifier(writer, reliableMessagingVersion, SequenceID);

            if (reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessaging11)
            {
                writer.WriteStartElement(WsrmFeb2005Strings.Prefix, DXD.Wsrm11Dictionary.MaxMessageNumber,
                    WsrmIndex.GetNamespace(reliableMessagingVersion));
                writer.WriteValue(Int64.MaxValue);
                writer.WriteEndElement();
            }
        }
    }

    internal sealed class SequenceClosedFault : WsrmHeaderFault
    {
        public SequenceClosedFault(UniqueId sequenceID)
            : base(true, Wsrm11Strings.SequenceClosed, SRP.SequenceClosedFaultString,
            null, sequenceID, false, true)
        {
        }

        public SequenceClosedFault(FaultCode code, FaultReason reason, XmlDictionaryReader detailReader,
            ReliableMessagingVersion reliableMessagingVersion)
            : base(code, Wsrm11Strings.SequenceClosed, reason, detailReader, reliableMessagingVersion, false, true)
        {
        }
    }

    internal sealed class SequenceTerminatedFault : WsrmHeaderFault
    {
        private SequenceTerminatedFault(bool isSenderFault, UniqueId sequenceID, string faultReason, string exceptionMessage)
            : base(isSenderFault, WsrmFeb2005Strings.SequenceTerminated, faultReason, exceptionMessage, sequenceID, true, true)
        {
        }

        public SequenceTerminatedFault(FaultCode code, FaultReason reason, XmlDictionaryReader detailReader,
            ReliableMessagingVersion reliableMessagingVersion)
            : base(code, WsrmFeb2005Strings.SequenceTerminated, reason, detailReader, reliableMessagingVersion, true, true)
        {
        }

        public static WsrmFault CreateCommunicationFault(UniqueId sequenceID, string faultReason,
            string exceptionMessage)
        {
            return new SequenceTerminatedFault(false, sequenceID, faultReason, exceptionMessage);
        }

        public static WsrmFault CreateMaxRetryCountExceededFault(UniqueId sequenceId)
        {
            return CreateCommunicationFault(sequenceId, SRP.SequenceTerminatedMaximumRetryCountExceeded, null);
        }

        public static WsrmFault CreateProtocolFault(UniqueId sequenceID, string faultReason,
            string exceptionMessage)
        {
            return new SequenceTerminatedFault(true, sequenceID, faultReason, exceptionMessage);
        }

        public static WsrmFault CreateQuotaExceededFault(UniqueId sequenceID)
        {
            return CreateProtocolFault(sequenceID, SRP.SequenceTerminatedQuotaExceededException, null);
        }
    }

    internal sealed class UnknownSequenceFault : WsrmHeaderFault
    {
        public UnknownSequenceFault(UniqueId sequenceID)
            : base(true, WsrmFeb2005Strings.UnknownSequence, SRP.UnknownSequenceFaultReason,
            SRP.UnknownSequenceMessageReceived, sequenceID, true, true)
        {
        }

        public UnknownSequenceFault(FaultCode code, FaultReason reason, XmlDictionaryReader detailReader,
            ReliableMessagingVersion reliableMessagingVersion)
            : base(code, WsrmFeb2005Strings.UnknownSequence, reason, detailReader, reliableMessagingVersion, true, true)
        {
        }

        public override CommunicationException CreateException()
        {
            string message;

            if (IsRemote)
            {
                message = FaultException.GetSafeReasonText(Reason);
                message = SRP.Format(SRP.UnknownSequenceFaultReceived, message);
            }
            else
            {
                message = GetExceptionMessage();
            }

            return new CommunicationException(message);
        }
    }

    internal class WsrmSequenceFaultHeader : WsrmMessageHeader
    {
        public WsrmSequenceFaultHeader(ReliableMessagingVersion reliableMessagingVersion, WsrmFault fault)
            : base(reliableMessagingVersion)
        {
            Fault = fault;
        }

        public WsrmFault Fault { get; }

        public override XmlDictionaryString DictionaryName
        {
            get
            {
                return XD.WsrmFeb2005Dictionary.SequenceFault;
            }
        }

        public string Subcode
        {
            get
            {
                return Fault.Subcode;
            }
        }

        public static XmlDictionaryReader GetReaderAtDetailContents(string detailName, string detailNamespace,
            XmlDictionaryReader headerReader, ReliableMessagingVersion reliableMessagingVersion)
        {
            if (reliableMessagingVersion == ReliableMessagingVersion.WSReliableMessagingFebruary2005)
            {
                return GetReaderAtDetailContentsFeb2005(detailName, detailNamespace, headerReader);
            }
            else
            {
                return GetReaderAtDetailContents11(detailName, detailNamespace, headerReader);
            }
        }

        public static XmlDictionaryReader GetReaderAtDetailContents11(string detailName, string detailNamespace,
            XmlDictionaryReader headerReader)
        {
            XmlDictionaryString wsrmNs = DXD.Wsrm11Dictionary.Namespace;
            headerReader.ReadFullStartElement(XD.WsrmFeb2005Dictionary.SequenceFault, wsrmNs);
            headerReader.Skip();
            headerReader.ReadFullStartElement(XD.Message12Dictionary.FaultDetail, wsrmNs);

            if ((headerReader.NodeType != XmlNodeType.Element)
                || (headerReader.NamespaceURI != detailNamespace)
                || (headerReader.LocalName != detailName))
            {
                headerReader.Close();
                return null;
            }

            return headerReader;
        }

        public static XmlDictionaryReader GetReaderAtDetailContentsFeb2005(string detailName, string detailNamespace,
            XmlDictionaryReader headerReader)
        {
            try
            {
                WsrmFeb2005Dictionary wsrmFeb2005Dictionary = XD.WsrmFeb2005Dictionary;
                XmlDictionaryString wsrmNs = wsrmFeb2005Dictionary.Namespace;
                XmlBuffer buffer = null;
                int index = 0;
                int depth = headerReader.Depth;
                headerReader.ReadFullStartElement(wsrmFeb2005Dictionary.SequenceFault, wsrmNs);

                while (headerReader.Depth > depth)
                {
                    if ((headerReader.NodeType == XmlNodeType.Element)
                        && (headerReader.NamespaceURI == detailNamespace)
                        && (headerReader.LocalName == detailName))
                    {
                        if (buffer != null)
                        {
                            return null;
                        }

                        buffer = new XmlBuffer(int.MaxValue);

                        try
                        {
                            index = buffer.SectionCount;
                            XmlDictionaryWriter writer = buffer.OpenSection(headerReader.Quotas);
                            // WriteNode moves the reader to the next sibling.
                            writer.WriteNode(headerReader, false);
                        }
                        finally
                        {
                            buffer.CloseSection();
                        }
                    }
                    else
                    {
                        if (headerReader.Depth == depth)
                            break;

                        headerReader.Read();
                    }
                }

                // Ensure at least one detail is found;
                if (buffer == null)
                {
                    return null;
                }

                // Close causes a state change.  It moves the buffer from Created to Reading.
                buffer.Close();
                XmlDictionaryReader detailReader = buffer.GetReader(index);
                return detailReader;
            }
            finally
            {
                headerReader.Close();
            }
        }

        public static string GetSubcode(XmlDictionaryReader headerReader,
            ReliableMessagingVersion reliableMessagingVersion)
        {
            string subCode = null;

            try
            {
                WsrmFeb2005Dictionary wsrmFeb2005Dictionary = XD.WsrmFeb2005Dictionary;
                XmlDictionaryString wsrmNs = WsrmIndex.GetNamespace(reliableMessagingVersion);
                string ns;

                headerReader.ReadStartElement(wsrmFeb2005Dictionary.SequenceFault, wsrmNs);
                headerReader.ReadStartElement(wsrmFeb2005Dictionary.FaultCode, wsrmNs);
                XmlUtil.ReadContentAsQName(headerReader, out subCode, out ns);

                if (ns != WsrmIndex.GetNamespaceString(reliableMessagingVersion))
                    subCode = null;

                headerReader.ReadEndElement();

                while (headerReader.IsStartElement())
                    headerReader.Skip();

                headerReader.ReadEndElement();
            }
            finally
            {
                headerReader.Close();
            }

            return subCode;
        }

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            writer.WriteStartElement(WsrmFeb2005Strings.Prefix, WsrmFeb2005Strings.FaultCode, Namespace);
            writer.WriteXmlnsAttribute(null, Namespace);
            writer.WriteQualifiedName(Subcode, Namespace);
            writer.WriteEndElement();

            bool wsrm11 = ReliableMessagingVersion == ReliableMessagingVersion.WSReliableMessaging11;

            if (wsrm11)
            {
                writer.WriteStartElement(WsrmFeb2005Strings.Prefix, XD.Message12Dictionary.FaultDetail, DictionaryNamespace);
            }

            Fault.WriteDetail(writer);

            if (wsrm11)
            {
                writer.WriteEndElement();
            }
        }
    }
}
