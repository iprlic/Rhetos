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
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureExtendsInfo))]
    public class DataStructureExtendsDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public static readonly SqlTag<DataStructureExtendsInfo> ForeignKeyConstraintOptions = "FK options";

        public static string GetConstraintName(DataStructureExtendsInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("DataStructureExtendsDatabaseDefinition_ConstraintName",
                info.Extension.Name,
                info.Base.Name));
        }

        private static bool ShouldCreateConstraint(DataStructureExtendsInfo info)
        {
            return info.Extension is EntityInfo
                && ForeignKeyUtility.GetSchemaTableForForeignKey(info.Base) != null;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (DataStructureExtendsInfo)conceptInfo;
            if (ShouldCreateConstraint(info))
            {
                return Sql.Format("DataStructureExtendsDatabaseDefinition_Create",
                    SqlUtility.Identifier(info.Extension.Module.Name) + "." + SqlUtility.Identifier(info.Extension.Name),
                    GetConstraintName(info),
                    ForeignKeyUtility.GetSchemaTableForForeignKey(info.Base),
                    ForeignKeyConstraintOptions.Evaluate(info));
            }
            // TODO: else - Generate a Filter+InvalidData validation in the server application that checks for invalid items.
            return "";
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (DataStructureExtendsInfo) conceptInfo;

            var dependencies = new List<Tuple<IConceptInfo, IConceptInfo>>();
            dependencies.Add(Tuple.Create<IConceptInfo, IConceptInfo>(info.Base, info.Extension));

            if (ShouldCreateConstraint(info))
                dependencies.AddRange(ForeignKeyUtility.GetAdditionalForeignKeyDependencies(info.Base)
                    .Select(dep => Tuple.Create<IConceptInfo, IConceptInfo>(dep, info))
                    .ToList());

            createdDependencies = dependencies;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (DataStructureExtendsInfo)conceptInfo;
            if (ShouldCreateConstraint(info))
            {
                return Sql.Format("DataStructureExtendsDatabaseDefinition_Remove",
                    SqlUtility.Identifier(info.Extension.Module.Name) + "." + SqlUtility.Identifier(info.Extension.Name),
                    GetConstraintName(info));
            }
            return "";
        }
    }
}
