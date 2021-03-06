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
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RegExMatch")]
    public class RegExMatchInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string RegularExpression { get; set; }

        public string ErrorMessage { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var itemFilterRegExMatchProperty = new ItemFilterInfo
            {
                Expression = String.Format(@"item => !String.IsNullOrEmpty(item.{0}) && !(new System.Text.RegularExpressions.Regex({1})).IsMatch(item.{0})",
                    Property.Name,
                    CsUtility.QuotedString("^" + RegularExpression + "$")),
                FilterName = Property.Name + "_RegExMatchFilter",
                Source = Property.DataStructure
            };
            var invalidDataRegExMatchProperty = new InvalidDataMarkPropertyInfo
            {
                DependedProperty = Property,
                FilterType = itemFilterRegExMatchProperty.FilterName,
                ErrorMessage = ErrorMessage,
                Source = Property.DataStructure
            };
            return new IConceptInfo[] { itemFilterRegExMatchProperty, invalidDataRegExMatchProperty };
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> existingConcepts)
        {
            try
            {
                new Regex(RegularExpression);
            }
            catch (Exception ex)
            {
                var msg = "Invalid format of the regular expression.";
                throw new DslSyntaxException(this, msg, ex);
            }
        }
    }
}
