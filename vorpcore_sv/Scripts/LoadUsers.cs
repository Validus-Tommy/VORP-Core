﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using CitizenFX.Core;
using vorpcore_sv.Class;
using vorpcore_sv.Utils;

namespace vorpcore_sv.Scripts
{
    public class LoadUsers:BaseScript
    {
        public static Dictionary<string, User> _users;
        public static List<string> _whitelist;
        public static bool _usingWhitelist;
        public LoadUsers()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            _users = new Dictionary<string, User>();
            _whitelist = new List<string>();
        }

        private async Task<bool> LoadUser([FromSource]Player source)
        {
            Debug.WriteLine(source.Identifiers["steam"]);
            string identifier = "steam:" + source.Identifiers["steam"];
            List<object> resultList = await Exports["ghmattimysql"].executeSync("SELECT * FROM users WHERE identifier = ?", new[] {identifier});
            if (resultList.Count > 0)
            {
                IDictionary<string, object> user = (dynamic)resultList[0];
                if ((int)user["banned"] == 1)
                {
                    return true;
                }
                User newUser = new User(identifier, user["group"].ToString(),(int)user["warnings"]);
                if (_users.ContainsKey(identifier))
                {
                    _users[identifier] = newUser;
                }
                else
                {
                    _users.Add(identifier,newUser);
                }
                
                return false;
            }
            else
            {
                //Usuario nuevo que entra por primera vez y no puede estar baneado xd
                await Exports["ghmattimysql"].executeSync("INSERT INTO users VALUES(?,'user',0,0)", new[] {identifier});
                User newUser = new User(identifier, "user", 0);
                if (_users.ContainsKey(identifier))
                {
                    _users[identifier] = newUser;
                }
                else
                {
                    _users.Add(identifier,newUser);
                }
                return false;
            }
        }
        
        private async void OnPlayerConnecting([FromSource]Player source, string playerName, dynamic setKickReason, dynamic deferrals)
        {
            deferrals.defer();
            bool _userEntering = false;
            bool banned = false;

            await Delay(0);

            if (!LoadConfig.isConfigLoaded)
            {
                deferrals.done("Servers is loading, Please wait a minute.");
                setKickReason("Servers is loading, Please wait a minute.");
                return;
            }

            var steamIdentifier = source.Identifiers["steam"];
            deferrals.update(LoadConfig.Langs["CheckingIdentifier"]);
            if (steamIdentifier == null)
            {
                deferrals.done(LoadConfig.Langs["NoSteam"]);
                setKickReason(LoadConfig.Langs["NoSteam"]);
            }
            if (_usingWhitelist)
            {
                if (_whitelist.Contains(steamIdentifier))
                {
                    //deferrals.done();
                    _userEntering = true;
                }
                else
                {
                    deferrals.done(LoadConfig.Langs["NoInWhitelist"]);
                    setKickReason(LoadConfig.Langs["NoInWhitelist"]);
                }
            }
            else
            {
                _userEntering = true;
            }

            if (_userEntering)
            {
                deferrals.update(LoadConfig.Langs["LoadingUser"]);
                banned =  await LoadUser(source);
                if (banned)
                {
                    deferrals.done(LoadConfig.Langs["BannedUser"]);
                    setKickReason(LoadConfig.Langs["BannedUser"]);
                }
                deferrals.done();
            }
        }
    }
}