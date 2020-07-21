using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Steamworks;

namespace BratusSteamLibraries
{
    /// <summary>
    /// This class is used to store the achievement as whole
    /// </summary>
    public class Achievements
    {
        //The Achievement ID (on the steam web page)
        public string achievementID;

        //The achievement displayed name
        public string achievementName;

        //Achievement description shown
        public string achievementDesc;

        /*---------------------------------------------------------------------------------------------
        this is what is stored for information, E.g: total wins. It is on an int format, but you can
        Change it based on your prefereces, but remember to update the stats on the steam page too.
        Remember to also update the type of GetSteamStat() method
        ---------------------------------------------------------------------------------------------*/
        public int achievementStat;

        //If the achievement was unlocked
        public bool Achieved;

        public Achievements(string _achievementID, string _AchievementName,
            string _AchievementDesc, bool _Achieved, int _achievementStat)
        {
            achievementID = _achievementID;
            achievementName = _AchievementName;
            achievementDesc = _AchievementDesc;
            achievementStat = _achievementStat;
            Achieved = _Achieved;
        }
    }

    /// <summary>
    /// This class is used to call achievement related methods
    /// </summary>
    public class SteamAchievements : MonoBehaviour
    {
        //For the achievements to be started, you need the total quantity of achievements of the game (from the steam page)
        public int quantityOfAchievements;

        //Flag used on update to update data (used normally after the update of the data on steam)
        private static bool m_storeStats;

        //SteamID of the user
        private static CSteamID m_steamID;


        /*------------------------------------------------------------------------------------
        The list of the achievements are composed of Achievements Type Objects
        It is automatically created at the OnEnable function, inside the "for".
        The achievements are created on a null state and will be filled on the OnUserStatsReceived.

        The achievement ID will need the int of "QuantityOfAchievements" to serve as base of how much
        achievements will be needed on the list. The Achievement IDS begin at 00.

        The achievements must be created at the steam achievement page as "ACHIEVEMENT_00", "ACHIEVEMENT_01"
        "ACHIEVEMENT_03", if you don't fill it this way, the system will not work
        ------------------------------------------------------------------------------------*/
        public static List<Achievements> listOfAchievements = new List<Achievements>();

        //CallResult that checks whenever the stats were received
        private CallResult<UserStatsReceived_t> m_userStatsReceived = new CallResult<UserStatsReceived_t>();
        //CallResult that checks whenever the stats is stored
        private CallResult<UserStatsStored_t> m_userStatsStored = new CallResult<UserStatsStored_t>();
        //CallResult that checks if the achievement was stored
        private CallResult<UserAchievementStored_t> m_achievementStored = new CallResult<UserAchievementStored_t>();

        private void Awake()
        {
            // DontDestroyOnLoad(gameObject);

            listOfAchievements.Clear();
            //This creates all the achievements on the list. The achievements are
            for (int i = 0; i <= quantityOfAchievements; i++)
            {
                if (i < 10)
                    listOfAchievements.Insert(i, new Achievements("ACHIEVEMENT_0" + i, "", "", false, 0));
                if (i >= 10)
                    listOfAchievements.Insert(i, new Achievements("ACHIEVEMENT_" + i, "", "", false, 0));
            }

        }

        private void OnEnable()
        {
            if (!SteamManager.Initialized)
                return;

            m_steamID = SteamUser.GetSteamID();

            if (m_steamID == null)
                Debug.Log("No SteamID found");


        }
        private void Start()
        {
            DownloadUserStats();
        }

        /// <summary>
        /// This method gets the icon of the achievement as of the actual state (if needed to use on a menu 
        /// option for instance)
        /// </summary>
        /// <param name="_achievementID"></param>
        /// <returns></returns>
        public static Sprite GetAchievementIcon(string _achievementID)
        {
            int _iconInt;
            uint _width, _height;

            Texture2D _downloadedAvatar;

            Rect _rect = new Rect(0, 0, 184, 184);

            Vector2 _pivot = new Vector2(0.5f, 0.5f);

            _iconInt = SteamUserStats.GetAchievementIcon(_achievementID);

            //If the icon isn't downloaded yet
            if (_iconInt == -1)
            {
                //this is a delay necessary if the image is not loaded yet, may freeze a bit of the code
                for (int i = 0; i <= 2000; i++)
                {
                    Debug.Log("Icon not found");
                }
            }

            if (_iconInt > 0)
            {
                SteamUtils.GetImageSize(_iconInt, out _width, out _height);

                if (_width > 0 && _height > 0)
                {
                    byte[] _avatarStream = new byte[4 * (int)_width * (int)_height];

                    SteamUtils.GetImageRGBA(_iconInt, _avatarStream, 4 * (int)_width * (int)_height);

                    _downloadedAvatar = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false);
                    _downloadedAvatar.LoadRawTextureData(_avatarStream);
                    _downloadedAvatar.Apply();

                    return (Sprite.Create(_downloadedAvatar, _rect, _pivot));
                }
            }

            return null;
        }

        /// <summary>
        /// Use this method to initialize the class, it'll download the user stats of the player
        /// and fill the Achievement list
        /// </summary>
        private void DownloadUserStats()
        {
            //SteamAPI handle
            SteamAPICall_t _steamAPICall = SteamUserStats.RequestUserStats(m_steamID);
            //Callback
            m_userStatsReceived.Set(_steamAPICall, OnUserStatsReceived);
        }


        /// <summary>
        /// Update a given stat by using an achievementID and the quantity to be updated.
        /// REMEMBER THAT THE STATS MUST BE NAMED AS FOLLOW ON THE STEAM PAGE:
        /// <para/>E.g: If the Stat belongs to the ACHIEVEMENT_00, name the stat on the steam "ACHIEVEMENT_00_STAT"
        /// otherwise it won't work
        /// </summary>
        /// <param name="_achievementID"></param>
        /// <param name="_statQuantity"></param>
        public static void UpdateSteamStat(string _achievementID, int _statQuantity)
        {
            Achievements _achievements = listOfAchievements.Find((_achievement) => _achievement.achievementID == _achievementID);
            try
            {
                if (_achievements.achievementID.Equals(_achievementID))
                {
                    SteamUserStats.SetStat(_achievementID + "_STAT", _statQuantity);
                    Debug.Log("Steam Stat Successfully updated!");
                }

            }
            catch (NullReferenceException e)
            {
                Debug.Log("Update Stat failed!" + "Error: " + e);
            }
            m_storeStats = true;
        }

        /// <summary>
        /// Returns a stat of type int
        /// </summary>
        /// <param name="_achievementID"></param>
        /// <returns></returns>
        public static int GetSteamStat(string _achievementID)
        {
            int _stat;

            Achievements _achievements = listOfAchievements.Find((_achievement) => _achievement.achievementID == _achievementID);
            try
            {
                if (_achievements.achievementID.Equals(_achievementID))
                {
                    SteamUserStats.GetStat(_achievementID + "_STAT", out _stat);
                    Debug.Log("Stat successfuled got!");
                    return _stat;
                }
                else
                    return 0;
            }
            catch (NullReferenceException e)
            {
                Debug.Log("Not found!");
                return 0;
            }
        }

        /// <summary>
        /// Unlock an steam achievement by supplying an ID
        /// </summary>
        /// <param name="_achievementID"></param>
        public static void UnlockSteamAchievement(string _achievementID)
        {
            Achievements _achievements = listOfAchievements.Find((_achievement) => _achievement.achievementID == _achievementID);

            try
            {
                if (_achievements.achievementID == _achievementID)
                {
                    SteamUserStats.SetAchievement(_achievementID);
                    m_storeStats = true;

                }
            }
            catch (NullReferenceException e)
            {
                Debug.Log("The achievement name was not found, verify if the name follows the standard");
            }

        }

        /// <summary>
        /// Clear an achievement, if you want to give the option for the player to clear an
        /// specific achievement.
        /// </summary>
        /// <param name="_achievementID"></param>
        public static void ClearSteamAchievement(string _achievementID)
        {
            Achievements _achievements = listOfAchievements.Find((_achievement) => _achievement.achievementID == _achievementID);

            try
            {
                if (_achievements.achievementID == _achievementID)
                {
                    SteamUserStats.ClearAchievement(_achievementID);
                    m_storeStats = true;
                }
            }
            catch (NullReferenceException)
            {
                Debug.Log("The achievement name was not found, verify if the name follows the standard");
            }
        }

        /// <summary>
        /// Clear all achievements. You can use it as an option for the player or for testing
        /// </summary>
        public static void ClearAllSteamAchievements()
        {
            foreach (Achievements _achiev in listOfAchievements)
            {
                SteamUserStats.ClearAchievement(_achiev.achievementID);
                _achiev.Achieved = false;
                SteamUserStats.SetStat(_achiev.achievementID + "_STAT", 0);
            }
            m_storeStats = true;
        }

        /// <summary>
        /// This function shall be used for testing, or if you want to get the full details achievements
        /// </summary>
        /// <param name="_achievementID"></param>
        public static void CheckAchievementAndStats(string _achievementID)
        {
            Achievements _achievements = listOfAchievements.Find((_achievement) => _achievement.achievementID == _achievementID);

            try
            {
                if (_achievements.achievementID.Equals(_achievementID))
                {
                    //Get the achievement status (locked/unlocked)
                    SteamUserStats.GetAchievement(_achievementID, out _achievements.Achieved);

                    //Get the achievement name
                    _achievements.achievementName = SteamUserStats.GetAchievementDisplayAttribute(
                       _achievements.achievementID, "name");

                    //Get the achievement description
                    _achievements.achievementDesc = SteamUserStats.GetAchievementDisplayAttribute(
                        _achievements.achievementID, "desc");

                    //Get the stat related to the Achievement
                    SteamUserStats.GetStat(_achievements.achievementID + "_STAT", out _achievements.achievementStat);

                    Debug.Log(
                   "Achievement ID - " + _achievements.achievementID + "\n" +
                   " Achievement Name - " + _achievements.achievementName + "\n" +
                   " Achievement Descri - " + _achievements.achievementDesc + "\n" +
                   " Achievement Stat - " + _achievements.achievementStat.ToString() + "\n" +
                   " Achievement Achieved - " + _achievements.Achieved + "\n"
                   );
                }
            }
            catch (NullReferenceException)
            {
                Debug.Log("The achievement name was not found, verify if the name follows the standard");
            }
        }

        /// <summary>
        /// UserStats received, normally used on the enable of this script to check the state of all achievements.
        /// this must be initialized via "DownloadUserStats()" method otherwise the achievements will not work.
        /// <para/> Also it can be used to reconcile the data if you use achievements for unlockables.
        /// </summary>
        /// <param name="_callback"></param>
        /// <param name="IOFailure"></param>
        private void OnUserStatsReceived(UserStatsReceived_t _callback, bool IOFailure)
        {
            Debug.Log("Failure - " + IOFailure + " User - " +
                _callback.m_steamIDUser + "GameID -" + _callback.m_nGameID);

            foreach (Achievements _achiev in listOfAchievements)
            {
                bool _ret = SteamUserStats.GetAchievement(_achiev.achievementID, out _achiev.Achieved);
                if (_ret)
                {
                    _achiev.achievementName = SteamUserStats.GetAchievementDisplayAttribute(
                                            _achiev.achievementID, "name");

                    _achiev.achievementDesc = SteamUserStats.GetAchievementDisplayAttribute(
                        _achiev.achievementID, "desc");

                    SteamUserStats.GetStat(_achiev.achievementID + "_STAT", out _achiev.achievementStat);

                    Debug.Log(
                        "Achievement ID - " + _achiev.achievementID + "\n" +
                        " Achievement Name - " + _achiev.achievementName + "\n" +
                        " Achievement Descri - " + _achiev.achievementDesc + "\n" +
                        " Achievement Stat - " + _achiev.achievementStat.ToString() + "\n" +
                        " Achievement Achieved - " + _achiev.Achieved + "\n"
                        );
                }
                else
                    Debug.Log("SteamUserStats.GetAchievement failed for Achievement " + _achiev.achievementID + "\nIs it registered in the Steam Partner site?");
            }
        }

        //CallRedult method to check if the StoreStats was successfull or not
        private static void OnUserStatsStored(UserStatsStored_t _callback, bool IOFailure)
        {
            if (EResult.k_EResultOK == _callback.m_eResult)
                Debug.Log("StoreStats was successful");
            else
                Debug.Log("StoreStats failed");
        }

        //CallResult method to check if the Achievement was stored correctly
        private static void OnUserAchievementStored(UserAchievementStored_t _callback, bool IOFailure)
        {
            if (0 == _callback.m_nMaxProgress)
                Debug.Log("Achievement " + _callback.m_rgchAchievementName + " Unlocked!");
            else
                Debug.Log("Achievement " + _callback.m_rgchAchievementName + "Progress - "
                + _callback.m_nCurProgress + " - Max - " + _callback.m_nMaxProgress);
        }

        public void Update()
        {
            if (!SteamManager.Initialized)
                return;

            //if the m_storeStats == true
            if (m_storeStats)
            {
                //the result will change so it will be called again or not
                bool _storeSuccess = SteamUserStats.StoreStats();
                m_storeStats = !_storeSuccess;
                DownloadUserStats();
            }
            SteamAPI.RunCallbacks();
        }
    }

}