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
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(OnSaveUpdateInfo))]
    public class OnSaveUpdateCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (OnSaveUpdateInfo)conceptInfo;
            codeBuilder.InsertCode(GetSnippet(info), WritableOrmDataStructureCodeGenerator.OnSaveTag1, info.SaveMethod.Entity);
        }

        private string GetSnippet(OnSaveUpdateInfo info)
        {
            return string.Format(
@"                {{ // {2}
                    {3}
                }}
                for (int i=0; i<inserted.Length; i++) inserted[i] = _executionContext.NHibernateSession.Load<{0}.{1}>(inserted[i].ID);
                for (int i=0; i<updated.Length; i++) updated[i] = _executionContext.NHibernateSession.Load<{0}.{1}>(updated[i].ID);

",
                    info.SaveMethod.Entity.Module.Name,
                    info.SaveMethod.Entity.Name,
                    info.RuleName,
                    info.CsCodeSnippet.Trim());
        }
    }
}
