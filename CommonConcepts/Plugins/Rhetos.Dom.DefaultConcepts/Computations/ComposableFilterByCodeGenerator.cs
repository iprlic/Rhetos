﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ComposableFilterByInfo))]
    public class ComposableFilterByCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersTypeTag = "AdditionalParametersType";
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersArgumentTag = "AdditionalParametersArgument";
        public static readonly CsTag<ComposableFilterByInfo> BeforeFilterTag = "BeforeFilter";

        private static string FilterExpressionPropertyName(ComposableFilterByInfo info)
        {
            return "_composableFilterExpression_" + CsUtility.TextToIdentifier(info.Parameter);
        }

        private static string FilterImplementationSnippet(ComposableFilterByInfo info)
        {
            return string.Format(
@"        private static readonly Func<IQueryable<{0}>, Common.DomRepository, {1}{4}, IQueryable<{0}>> {2} =
            {3};

        public IQueryable<{0}> Filter(IQueryable<{0}> source, {1} parameter)
        {{
            {6}
            return {2}(source, _domRepository, parameter{5});
        }}

",
            info.Source.GetKeyProperties(),
            info.Parameter,
            FilterExpressionPropertyName(info),
            info.Expression,
            AdditionalParametersTypeTag.Evaluate(info),
            AdditionalParametersArgumentTag.Evaluate(info),
            BeforeFilterTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComposableFilterByInfo)conceptInfo;
            codeBuilder.InsertCode(FilterImplementationSnippet(info), RepositoryHelper.RepositoryMembers, info.Source);
        }
    }
}
