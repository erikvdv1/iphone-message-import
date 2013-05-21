#region License
// <copyright file="SmsDatabase.cs" company="Infiks">
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
// <date>2013-05-21 21:55</date>
#endregion
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace Infiks.IPhone
{
    /// <summary>
    /// Represents the iPhone SMS database.
    /// </summary>
    public class SmsDatabase : IDisposable
    {
        /// <summary>
        /// The table in which the messages reside.
        /// </summary>
        private const string MessageTable = "message";

        /// <summary>
        /// The table in which the messages groups reside.
        /// </summary>
        private const string MessageGroupTable = "msg_group";

        /// <summary>
        /// The table in which the group members reside.
        /// </summary>
        private const string GroupMemberTable = "group_member";

        /// <summary>
        /// The table in which SQLite stores its properties.
        /// </summary>
        private const string PropertiesTable = "_SqliteDatabaseProperties";

        /// <summary>
        /// Indicates whether the database is opened.
        /// </summary>
        public bool IsOpen { private set; get; }

        /// <summary>
        /// The location of the database.
        /// </summary>
        public string Location { private set; get; }

        /// <summary>
        /// The SQLite connection of the database.
        /// </summary>
        public SQLiteConnection Connection { get; private set; }

        /// <summary>
        /// The triggers defined in this database.
        /// </summary>
        public IEnumerable<SQLiteTrigger> Triggers { get; private set; }

        /// <summary>
        /// The auto-generated ID of the last inserted record.
        /// </summary>
        public int LastInsertId
        {
            get { return GetLastInsertId(); }
        }

        /// <summary>
        /// Creates a new database wrapper.
        /// </summary>
        /// <param name="location">The path to the SQLite database that contains the messages.</param>
        public SmsDatabase(string location)
        {
            Location = location;
            Open();
            Triggers = GetTriggers();
        }

        /// <summary>
        /// Writes a group of messages to the database.
        /// </summary>
        /// <param name="group">The group of messages.</param>
        public void SaveMessageGroup(MessageGroup group)
        {
            using (SQLiteTransaction transaction = Connection.BeginTransaction())
            {
                long groupId = 0;
                long messageId = 0;

                // Check for existing group member
                string checkMember = String.Format("SELECT group_id FROM {0} WHERE address = '{1}'", GroupMemberTable, group.Address);
                object existingGroupId = Scalar(checkMember);

                if (existingGroupId == null)
                {
                    // Insert new group
                    string insertGroup =
                        String.Format("INSERT INTO {0} (type, newest_message, unread_count) VALUES ({1}, {2}, {3})",
                                      MessageGroupTable, 0, 0, 0);
                    Query(insertGroup);
                    groupId = GetLastInsertId();

                    // Insert group member
                    string insertMember = String.Format("INSERT INTO {0} (group_id, address, country) VALUES ({1}, '{2}', '{3}')", GroupMemberTable, groupId, group.Address, group.CountryCode);
                    Query(insertMember);
                }
                else
                {
                    groupId = (long)existingGroupId;
                }

                // Insert messages using a prepared statement
                using (var command = new SQLiteCommand(Connection))
                {
                    /* A row consists of the following fields
                     * - rowid:    A unique identifier for each message.
                     * - address:  The telephone number of the sender / recipient.
                     * - date:     The Timestamp of when the message was sent / received (1-1-2001 + <Timezone> + <Timestamp>).
                     * - text:     The message content.
                     * - flags:    A 3 indicates that the message was sent, a 2 indicates that it was received.
                     * - group_id: Indicates the conversation the message belongs to (messages are linked together via groups and the group_member table).
                     */

                    // Create and setup statement parameters
                    var address = new SQLiteParameter();
                    var date = new SQLiteParameter();
                    var text = new SQLiteParameter();
                    var type = new SQLiteParameter();
                    var association = new SQLiteParameter();

                    command.CommandText = String.Format("INSERT INTO {0} " +
                        "(address, date, text, flags, replace, group_id, association_id, height, UIFlags, version, country, read, madrid_version, madrid_type, madrid_error, is_madrid, madrid_date_read, madrid_date_delivered) " +
                        "VALUES (?, ?, ?, ?, 0, {1}, ?, 0, 4, 0, '{2}', 1, 0, 0, 0, 0, 0, 0)",
                        MessageTable,
                        groupId,
                        group.CountryCode);
                    command.Parameters.Add(address);
                    command.Parameters.Add(date);
                    command.Parameters.Add(text);
                    command.Parameters.Add(type);
                    command.Parameters.Add(association);

                    // Execute the statement for each message
                    foreach (var message in group)
                    {
                        address.Value = message.Address;
                        date.Value = message.Date;
                        text.Value = message.Text;
                        type.Value = message.Type;
                        association.Value = (message.Type == MessageType.Incoming) ? 0 : message.Date;
                        command.ExecuteNonQuery();

                        messageId = GetLastInsertId();
                    }
                }

                // Update group
                string updateGroup = String.Format("UPDATE {0} SET newest_message = {1} WHERE ROWID = {2} AND newest_message < {1}", MessageGroupTable, messageId, groupId);
                Query(updateGroup);

                // Commit changes
                transaction.Commit();
            } 

        }

        /// <summary>
        /// Gets all triggers associated with this database.
        /// </summary>
        /// <returns>The SQLite triggers.</returns>
        private IEnumerable<SQLiteTrigger> GetTriggers()
        {
            string sql = String.Format("SELECT name, sql FROM {0} WHERE type = '{1}' AND tbl_name = '{2}'", "SQLITE_MASTER", "trigger", MessageTable);
            DataTable table = GetDataTable(sql);
            return from DataRow row in table.Rows select new SQLiteTrigger(row["name"] as string, row["sql"] as string);
        }

        /// <summary>
        /// Returns the ID of the last inserted record.
        /// </summary>
        /// <returns>The ID of the last inserted record.</returns>
        public int GetLastInsertId()
        {
            int id;
            var result = Scalar("SELECT last_insert_rowid()");
            var success = Int32.TryParse(result.ToString(), out id);
            return success ? id : -1;
        }

        /// <summary>
        /// Updates the message counters in the properties table.
        /// </summary>
        /// <param name="outgoingCount">The number of outgoing messages.</param>
        /// <param name="incomingCount">The number of incoming messages.</param>
        public void UpdateCounters(int outgoingCount, int incomingCount)
        {
            string updateCounter = String.Format("UPDATE {0} SET value = value + {1} WHERE key = '{2}'", PropertiesTable, outgoingCount, "counter_out_lifetime");
            Query(updateCounter);
            updateCounter = String.Format("UPDATE {0} SET value = value + {1} WHERE key = '{2}'", PropertiesTable, incomingCount, "counter_in_lifetime");
            Query(updateCounter);
        }

        /// <summary>
        /// Removes all triggers from the database.
        /// This allows inserting new records, without triggering unsupported functions.
        /// </summary>
        public void DropTriggers()
        {
            foreach (SQLiteTrigger trigger in Triggers)
            {
                Query(trigger.DeleteStatement);
            }
        }

        /// <summary>
        /// Restores all triggers to the database.
        /// </summary>
        public void CreateTriggers()
        {
            foreach (SQLiteTrigger trigger in Triggers)
            {
                Query(trigger.CreateStatement);
            }
        }

        /// <summary>
        /// Executes a SQL query and stores the result in a DataTable.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The results of the query.</returns>
        private DataTable GetDataTable(string sql)
        {
            var dt = new DataTable();

            try
            {
                var mycommand = new SQLiteCommand(Connection) { CommandText = sql };
                SQLiteDataReader reader = mycommand.ExecuteReader();

                dt.Load(reader);
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return dt;
        }

        /// <summary>
        /// Executes a SQL query and returns the number of rows affected.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The number of rows affected.</returns>
        public int Query(string sql)
        {
            int result = -1;
            try
            {
                var mycommand = new SQLiteCommand(Connection) { CommandText = sql };
                result = mycommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        /// <summary>
        /// Executes a SQL query and returns the resulting scalar (one value).
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <returns>The result of the query.</returns>
        public object Scalar(string sql)
        {
            object result = null;
            try
            {
                var mycommand = new SQLiteCommand(Connection) { CommandText = sql };
                result = mycommand.ExecuteScalar();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        /// <summary>
        /// Opens the database for interacting.
        /// </summary>
        public void Open()
        {
            if (IsOpen) return;

            try
            {
                string connectionString = string.Format("Data Source={0};New=False;", Location);
                Connection = new SQLiteConnection(connectionString);
                Connection.Open();
                IsOpen = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Closes the database.
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;

            try
            {
                Connection.Close();
                IsOpen = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Closes the database.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
