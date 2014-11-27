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
using System.Linq;
using System.Text;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(RowPermissionsAllowReadInfo))]
    public class RowPermissionsAllowReadCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (RowPermissionsAllowReadInfo)conceptInfo;

            codeBuilder.InsertCode(
                GetSnippetFilterExpression(
                    info.RowPermissionsFilter.Source,
                    info.Name,
                    info.GroupSelector,
                    info.PermissionPredicate,
                    info.Condition,
                    allow: true),
                RowPermissionsPluginableFilterInfo.FilterExpressionsTag,
                info.RowPermissionsFilter);
        }

        public static string GetSnippetFilterExpression(
            DataStructureInfo source,
            string name,
            string groupSelector,
            string permissionPredicate,
            string condition,
            bool allow)
        {
            string checkRuleCondition;
            if (!string.IsNullOrEmpty(condition))
                checkRuleCondition = string.Format(
                @"Func<bool> ruleCondition = () => {0};
				if (ruleCondition.Invoke())
					",
                    condition);
            else
                checkRuleCondition = "";

            return string.Format(
            @"{{
				var {2}Function = DomUtility.Function(() =>
                    {3});
				var {2} = {2}Function.Invoke();
				{5}filterExpression.{6}({4});
			}}
            ",
                source.Module.Name,
                source.Name,
                name,
                groupSelector,
                permissionPredicate,
                checkRuleCondition,
                allow ? "Include" : "Exclude");
        }
    }
}