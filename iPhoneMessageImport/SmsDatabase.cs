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
using System.Linq;

namespace Infiks.IPhone
{
    /// <summary>
    /// Represents the iPhone SMS database.
    /// </summary>
    public class SmsDatabase : IDisposable
    {
        /// <summary>
        /// The message service used in the database.
        /// </summary>
        private const string MessageService = "SMS";

        /// <summary>
        /// The name of the table in which the messages reside.
        /// </summary>
        private const string MessageTable = "message";

        /// <summary>
        /// The name of the cross table for chats and messages.
        /// </summary>
        private const string ChatMessageJoinTable = "chat_message_join";

        /// <summary>
        /// The name of the table in which the chats reside.
        /// </summary>
        private const string ChatTable = "chat";

        /// <summary>
        /// The name of the cross table for chats and handles.
        /// </summary>
        private const string ChatHandleJoinTable = "chat_handle_join";

        /// <summary>
        /// The name of the table in which the handles reside.
        /// </summary>
        private const string HandleTable = "handle";

        /// <summary>
        /// The table in which SQLite stores some properties.
        /// </summary>
        private const string PropertiesTable = "_SqliteDatabaseProperties";

        /// <summary>
        /// Indicates whether the database is opened or not.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// The location of the database.
        /// </summary>
        public string Location { get; private set; }

        /// <summary>
        /// The triggers defined in this database.
        /// </summary>
        public IEnumerable<Trigger> Triggers { get; private set; }

        /// <summary>
        /// The SQLite connection of the database.
        /// </summary>
        private SQLiteConnection _connection;

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
            using (SQLiteTransaction transaction = _connection.BeginTransaction())
            {
                // Get or create handle and chat
                long handleId = GetOrCreateHandle(group);
                long chatId = GetOrCreateChat(group);

                // Insert messages using a prepared statement
                using (var command = new SQLiteCommand(_connection))
                {
                    // Build query
                    command.CommandText = String.Format("INSERT INTO {0} " +
                        "(guid, text, handle_id, country, version, service, date, date_read, date_delivered, is_finished, is_from_me, is_prepared, is_read, is_sent) " +
                        "VALUES (?, ?, {1}, '{2}', {3}, '{4}', ?, ?, ?, {5}, ?, {6}, ?, ?)",
                        MessageTable,
                        handleId,
                        group.CountryCode.ToString().ToLower(),
                        1,
                        MessageService,
                        1,
                        1);

                    // Create and setup statement parameters
                    var guid = new SQLiteParameter();
                    var text = new SQLiteParameter();
                    var date = new SQLiteParameter();
                    var dateRead = new SQLiteParameter();
                    var dateDelivered = new SQLiteParameter();
                    var isFromMe = new SQLiteParameter();
                    var isRead = new SQLiteParameter();
                    var isSent = new SQLiteParameter();
                    command.Parameters.Add(guid);
                    command.Parameters.Add(text);
                    command.Parameters.Add(date);
                    command.Parameters.Add(dateRead);
                    command.Parameters.Add(dateDelivered);
                    command.Parameters.Add(isFromMe);
                    command.Parameters.Add(isRead);
                    command.Parameters.Add(isSent);

                    // Execute the statement for each message
                    foreach (var message in group)
                    {
                        // Fill prepared statement
                        guid.Value = Guid.NewGuid().ToString().ToUpper();
                        text.Value = message.Text;
                        date.Value = message.AppleTimestamp;
                        dateRead.Value = message.AppleTimestamp;
                        dateDelivered.Value = message.AppleTimestamp;
                        isFromMe.Value = (message.Type == MessageType.Outgoing) ? 1 : 0;
                        isRead.Value = (message.Type == MessageType.Incoming) ? 1 : 0;
                        isSent.Value = (message.Type == MessageType.Outgoing) ? 1 : 0;
                        command.ExecuteNonQuery();

                        // Insert join
                        long messageId = LastInsertId;
                        string insertJoin = String.Format("INSERT INTO {0} (chat_id, message_id) VALUES ({1}, {2})", ChatMessageJoinTable, chatId, messageId);
                        Query(insertJoin);
                    }
                }

                // Commit changes
                transaction.Commit();
            } 

        }

        /// <summary>
        /// Finds the handle corresponding to the message group. If no such handle exists it will be created.
        /// </summary>
        /// <param name="group">The message group.</param>
        /// <returns>The ID of the handle.</returns>
        private long GetOrCreateHandle(MessageGroup group)
        {
            // Check for existing handle
            string checkHandle = String.Format("SELECT ROWID FROM {0} WHERE id = '{1}' AND service = '{2}'", HandleTable, group.Address, MessageService);
            object existingHandleId = Scalar(checkHandle);
            if (existingHandleId != null)
                return (long)existingHandleId;

            // Insert new handle
            string insertHandle = String.Format("INSERT INTO {0} (id, country, service, uncanonicalized_id) VALUES ('{1}', '{2}', '{3}', '{1}')", HandleTable, group.Address, group.CountryCode, MessageService);
            Query(insertHandle);
            return LastInsertId;
        }

        /// <summary>
        /// Finds the chat corresponding to the message group. If no such chat exists it will be created. The corresponding handle will also be created.
        /// </summary>
        /// <param name="group">The message group.</param>
        /// <returns>The ID of the chat.</returns>
        private long GetOrCreateChat(MessageGroup group)
        {
            // Check for existing chat
            string checkChat = String.Format("SELECT ROWID FROM {0} WHERE chat_identifier = '{1}' AND service_name = '{2}'", ChatTable, group.Address, MessageService);
            object existingChatId = Scalar(checkChat);
            if (existingChatId != null)
                return  (long)existingChatId;

            // Insert new chat
            string insertChat = String.Format("INSERT INTO {0} (guid, style, state, chat_identifier, service_name) VALUES ('{1}', {2}, {3}, '{4}', '{5}')", ChatTable, string.Format("{0};-;{1}", MessageService, group.Address), 45, 2, group.Address, MessageService);
            Query(insertChat);
            long chatId = LastInsertId;

            // Get or create handle
            long handleId = GetOrCreateHandle(group);

            // Insert join
            string insertJoin = String.Format("INSERT INTO {0} (chat_id, handle_id) VALUES ({1}, {2})", ChatHandleJoinTable, chatId, handleId);
            Query(insertJoin);

            return chatId;
        }

        /// <summary>
        /// Gets all triggers associated with this database.
        /// </summary>
        /// <returns>The SQLite triggers.</returns>
        private IEnumerable<Trigger> GetTriggers()
        {
            string sql = String.Format("SELECT name, sql FROM {0} WHERE type = '{1}'", "SQLITE_MASTER", "trigger");
            DataTable table = GetDataTable(sql);
            return from DataRow row in table.Rows select new Trigger(row["name"] as string, row["sql"] as string);
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
            foreach (Trigger trigger in Triggers)
            {
                Query(trigger.DeleteStatement);
            }
        }

        /// <summary>
        /// Restores all triggers to the database.
        /// </summary>
        public void CreateTriggers()
        {
            foreach (Trigger trigger in Triggers)
            {
                Query(trigger.CreateStatement);
            }
        }

        /// <summary>
        /// Creates a new transaction if one isn't already active on the connection.
        /// </summary>
        /// <returns>The new transaction object.</returns>
        public Transaction BeginTransaction()
        {
            return new Transaction(_connection.BeginTransaction());
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
                var mycommand = new SQLiteCommand(_connection) { CommandText = sql };
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
                var mycommand = new SQLiteCommand(_connection) { CommandText = sql };
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
                var mycommand = new SQLiteCommand(_connection) { CommandText = sql };
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
                _connection = new SQLiteConnection(connectionString);
                _connection.Open();
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
                _connection.Close();
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
