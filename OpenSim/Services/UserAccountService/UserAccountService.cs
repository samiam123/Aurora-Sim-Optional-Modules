/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Reflection;
using Nini.Config;
using OpenSim.Data;
using OpenSim.Framework.Console;
using OpenSim.Services.Interfaces;
using System.Collections.Generic;
using OpenMetaverse;

namespace OpenSim.Services.UserAccountService
{
    public class UserAccountService : UserAccountServiceBase, IUserAccountService
    {
        public UserAccountService(IConfigSource config)
            : base(config)
        {
            MainConsole.Instance.Commands.AddCommand("UserService", false,
                    "create user",
                    "create user [<first> [<last> [<pass> [<x> <y> [<email>]]]]]",
                    "Create a new user", HandleCreateUser);
        }

        #region IUserAccountService

        public UserAccount GetUserAccount(UUID scopeID, string firstName,
                string lastName)
        {
            UserAccountData[] d;

            if (scopeID != UUID.Zero)
            {
                d = m_Database.Get(
                        new string[] { "ScopeID", "FirstName", "LastName" },
                        new string[] { scopeID.ToString(), firstName, lastName });
            }
            else
            {
                d = m_Database.Get(
                        new string[] { "FirstName", "LastName" },
                        new string[] { firstName, lastName });
            }

            if (d.Length < 1)
                return null;

            return MakeUserAccount(d[0]);
        }

        private UserAccount MakeUserAccount(UserAccountData d)
        {
            UserAccount u = new UserAccount();
            u.FirstName = d.FirstName;
            u.LastName = d.LastName;
            u.PrincipalID = d.PrincipalID;
            u.ScopeID = d.ScopeID;
            u.Email = d.Data["Email"].ToString();
            u.Created = Convert.ToInt32(d.Data["Created"].ToString());

            string[] URLs = d.Data["ServiceURLs"].ToString().Split(new char[] { ' ' });
            u.ServiceURLs = new Dictionary<string, object>();

            foreach (string url in URLs)
            {
                string[] parts = url.Split(new char[] { '=' });

                if (parts.Length != 2)
                    continue;

                string name = System.Web.HttpUtility.UrlDecode(parts[0]);
                string val = System.Web.HttpUtility.UrlDecode(parts[1]);

                u.ServiceURLs[name] = val;
            }

            return u;
        }

        public UserAccount GetUserAccount(UUID scopeID, string email)
        {
            UserAccountData[] d;

            if (scopeID != UUID.Zero)
            {
                d = m_Database.Get(
                        new string[] { "ScopeID", "Email" },
                        new string[] { scopeID.ToString(), email });
            }
            else
            {
                d = m_Database.Get(
                        new string[] { "Email" },
                        new string[] { email });
            }

            if (d.Length < 1)
                return null;

            return MakeUserAccount(d[0]);
        }

        public UserAccount GetUserAccount(UUID scopeID, UUID principalID)
        {
            UserAccountData[] d;

            if (scopeID != UUID.Zero)
            {
                d = m_Database.Get(
                        new string[] { "ScopeID", "PrincipalID" },
                        new string[] { scopeID.ToString(), principalID.ToString() });
            }
            else
            {
                d = m_Database.Get(
                        new string[] { "PrincipalID" },
                        new string[] { principalID.ToString() });
            }

            if (d.Length < 1)
                return null;

            return MakeUserAccount(d[0]);
        }

        public bool StoreUserAccount(UserAccount data)
        {
            UserAccountData d = new UserAccountData();

            d.FirstName = data.FirstName;
            d.LastName = data.LastName;
            d.PrincipalID = data.PrincipalID;
            d.ScopeID = data.ScopeID;
            d.Data = new Dictionary<string, string>();
            d.Data["Email"] = data.Email;
            d.Data["Created"] = data.Created.ToString();

            List<string> parts = new List<string>();

            foreach (KeyValuePair<string, object> kvp in data.ServiceURLs)
            {
                string key = System.Web.HttpUtility.UrlEncode(kvp.Key);
                string val = System.Web.HttpUtility.UrlEncode(kvp.Value.ToString());
                parts.Add(key + "=" + val);
            }

            d.Data["ServiceURLs"] = string.Join(" ", parts.ToArray());

            return m_Database.Store(d);
        }

        public List<UserAccount> GetUserAccounts(UUID scopeID, string query)
        {
            UserAccountData[] d = m_Database.GetUsers(scopeID, query);

            if (d == null)
                return new List<UserAccount>();

            List<UserAccount> ret = new List<UserAccount>();

            foreach (UserAccountData data in d)
                ret.Add(MakeUserAccount(data));

            return ret;
        }

        #endregion

        #region Console commands
        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="cmdparams">string array with parameters: firstname, lastname, password, locationX, locationY, email</param>
        protected void HandleCreateUser(string module, string[] cmdparams)
        {
            string firstName;
            string lastName;
            string password;
            string email;
            uint regX = 1000;
            uint regY = 1000;

            //    IConfig standalone;
            //    if ((standalone = m_config.Source.Configs["StandAlone"]) != null)
            //    {
            //        regX = (uint)standalone.GetInt("default_location_x", (int)regX);
            //        regY = (uint)standalone.GetInt("default_location_y", (int)regY);
            //    }


            //    if (cmdparams.Length < 3)
            //        firstName = MainConsole.Instance.CmdPrompt("First name", "Default");
            //    else firstName = cmdparams[2];

            //    if (cmdparams.Length < 4)
            //        lastName = MainConsole.Instance.CmdPrompt("Last name", "User");
            //    else lastName = cmdparams[3];

            //    if (cmdparams.Length < 5)
            //        password = MainConsole.Instance.PasswdPrompt("Password");
            //    else password = cmdparams[4];

            //    if (cmdparams.Length < 6)
            //        regX = Convert.ToUInt32(MainConsole.Instance.CmdPrompt("Start Region X", regX.ToString()));
            //    else regX = Convert.ToUInt32(cmdparams[5]);

            //    if (cmdparams.Length < 7)
            //        regY = Convert.ToUInt32(MainConsole.Instance.CmdPrompt("Start Region Y", regY.ToString()));
            //    else regY = Convert.ToUInt32(cmdparams[6]);

            //    if (cmdparams.Length < 8)
            //        email = MainConsole.Instance.CmdPrompt("Email", "");
            //    else email = cmdparams[7];

            //    if (null == m_commsManager.UserProfileCacheService.GetUserDetails(firstName, lastName))
            //    {
            //        m_commsManager.UserAdminService.AddUser(firstName, lastName, password, email, regX, regY);
            //    }
            //    else
            //    {
            //        m_log.ErrorFormat("[CONSOLE]: A user with the name {0} {1} already exists!", firstName, lastName);
            //    }
            //}

        }
        #endregion
    }
}
