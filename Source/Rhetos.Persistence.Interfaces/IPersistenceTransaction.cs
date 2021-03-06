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
using NHibernate;

namespace Rhetos.Persistence
{
    public interface IPersistenceTransaction : IDisposable
    {
        ISession NHibernateSession { get; }

        /// <summary>
        /// DiscardChanges marks the transaction as invalid. The changes will be descarded (rollback executed) on Dispose.
        /// </summary>
        void DiscardChanges();

        /// <summary>
        /// Use for cleanup code, such as deleting temporary data that may be used until the transaction is closed.
        /// This event will not be invoked if the transaction rollback was executed (see <see cref="DiscardChanges()"/>).
        /// </summary>
        event Action BeforeClose;
    }
}
