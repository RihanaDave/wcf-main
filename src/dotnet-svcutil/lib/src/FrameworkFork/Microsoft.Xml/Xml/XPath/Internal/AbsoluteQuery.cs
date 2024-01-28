// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MS.Internal.Xml.XPath
{
    using System;
    using Microsoft.Xml;
    using Microsoft.Xml.XPath;
    using System.Diagnostics;
    using System.Globalization;

    internal sealed class AbsoluteQuery : ContextQuery
    {
        public AbsoluteQuery() : base() { }
        private AbsoluteQuery(AbsoluteQuery other) : base(other) { }

        public override object Evaluate(XPathNodeIterator context)
        {
            base.contextNode = context.Current.Clone();
            base.contextNode.MoveToRoot();
            count = 0;
            return this;
        }

        public override XPathNavigator MatchNode(XPathNavigator context)
        {
            if (context != null && context.NodeType == XPathNodeType.Root)
            {
                return context;
            }
            return null;
        }

        public override XPathNodeIterator Clone() { return new AbsoluteQuery(this); }
    }
}
