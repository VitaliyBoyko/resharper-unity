﻿using JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Tree.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.Cg.Parsing.TokenNodeTypes
{
    internal class CgIdentifierTokenNodeType : CgTokenNodeTypeBase
    {
        public CgIdentifierTokenNodeType(int index)
            : base("IDENTIFIER", index)
        {
        }

        public override LeafElementBase Create(string token)
        {
            return new CgIdentifier(token);
        }

        public override LeafElementBase Create(IBuffer buffer, TreeOffset startOffset, TreeOffset endOffset)
        {
            return new CgIdentifier(buffer.GetText(new TextRange(startOffset.Offset, endOffset.Offset)));
        }

        public override string TokenRepresentation => "identifier";
    }
}