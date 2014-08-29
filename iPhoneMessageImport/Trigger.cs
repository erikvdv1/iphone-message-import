#region License
// <copyright file="Trigger.cs" company="Infiks">
// 
// iPhone Message Import, imports messages into a backup of your iPhone.
// Copyright (c) 2013 Infiks
// 
// This file is part of iPhone Message Import
// 
// iPhone Message Import is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iPhone Message Import is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iPhone Message Import.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <author>Erik van der Veen</author>
// <date>2013-05-21 21:56</date>
#endregion
using System;

namespace Infiks.IPhone
{
    /// <summary>
    /// This class represents a SQL trigger.
    /// </summary>
    public class Trigger
    {
        /// <summary>
        /// The name of the trigger.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The SQL query that creates this trigger.
        /// </summary>
        public string CreateStatement { get; private set; }

        /// <summary>
        /// The SQL query that deletes this trigger.
        /// </summary>
        public string DeleteStatement { get; private set; }

        /// <summary>
        /// Creates a new Trigger.
        /// </summary>
        /// <param name="name">The name of the trigger.</param>
        /// <param name="sql">The SQL query that creates this trigger.</param>
        public Trigger(string name, string sql)
        {
            Name = name;
            CreateStatement = sql;
            DeleteStatement = String.Format("DROP TRIGGER {0}", Name);
        }
    }
}
