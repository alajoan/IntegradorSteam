using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using UnityEngine.UI;
using System.Threading;

namespace BratusSteamLibraries
{
    public class LeaderboardCell
    {
        public Sprite playerImage;
        public string playerName;
        public int playerScore;

        public LeaderboardCell(Sprite _playerImage, string _playerName, int _playerScore)
        {
            playerImage = _playerImage;
            playerName = _playerName;
            playerScore = _playerScore;
        }
    }

    public class SteamLeaderboard : MonoBehaviour
    {
        //Leaderboard name, can be put directly into the "SteamUserStats.FindLeaderBoard"
        [SerializeField]
        private static string m_leaderboardName;

        //Quantity of entries found on a given leaderboard
        private static int m_leaderboardCount;

        //Steam leaderboard that'll be setted by the method "OnLeaderBoardFindResult" and is the result of the LeaderBoardFindResult callback result
        private static SteamLeaderboard_t m_steamLeaderBoard;

        //Entries from the leaderboard that'll be setted by the method "OnLeaderBoardScoresDownloaded" and is the result of the LeaderboardScoresDownloaded callback result
        private static SteamLeaderboardEntries_t m_leaderboardEntries;

        //This list is made by LeaderboardCells classes that'll host all the information of the players got on the leaderboard entries
        private static List<LeaderboardCell> leaderboardPlayersList = new List<LeaderboardCell>();

        //This list is the gameObject part of the leaderboard cells
        private static List<GameObject> leaderboardCellList = new List<GameObject>();

        //This range will set how many entries will be downloaded
        [SerializeField]
        private static int m_leaderboardEntriesRangeInit, m_leaderboardEntriesRangeEnd;

        //This transform will be the parent of the leaderboard individual cells
        [SerializeField]
        public static Transform m_Leaderboardtarget;

        /*-------------------------------------------------------------------------------
         * This is the prefab of the cells that MUST CONTAIN:
         * An image Object
         * A text object named "name"
         * A text object named "score"        
        ---------------------------------------------------------------------------------*/
        private static GameObject m_leaderboardPrefab;

        //Distance between each cell, must be adjusted accordingly
        private static float m_distanceBetweenCells;


        //Used by the method OnLeaderBoardFindResult() to check if the leaderboard was found or not.
        private static bool m_leaderboardInitiated, m_usingentriesFinished;

        /*-----------------------------------------------------------------------------------------------------------------------
         * Flag that controls the upload score method (this is setted to keep best)
         * k_ELeaderboardUploadScoreMethodKeepBest = update using the best score as base
         * k_ELeaderboardUploadScoreMethodForceUpdate = update the leaderboard with whatever result you put on score
         -----------------------------------------------------------------------------------------------------------------------*/
        private static ELeaderboardUploadScoreMethod m_uploadScoreMethod = ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest;

        /*------------------------------------------------------------------------------------------------------------------------
         * Flag that controls how the request will be handled
         * k_ELeaderboardDataRequestGlobal = Requests the data from all results
         * k_ELeaderboardDataRequestFriends = Requests the data from all your steam friends
         * k_ELeaderboardDataRequestGlobalAroundUser = Request from global data, but the range will be around the user. Eg: if the range is -3,3 it'll return the 3 results before and after the player
         * k_ELeaderboardDataRequestUsers = Can not be used, specified on Steam APi.
        --------------------------------------------------------------------------------------------------------------------------*/
        private static ELeaderboardDataRequest m_requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestUsers;


        //Call result that will be set by FindLeaderboard() method depending if the OnLeaderBoardUpload result returns true or false.
        private static CallResult<LeaderboardFindResult_t> m_leaderboardFindResult = new CallResult<LeaderboardFindResult_t>();

        //Call result that will be set by UpdateScore() method depending if the OnLeaderboardUploadResult resul returns true or false.
        private static CallResult<LeaderboardScoreUploaded_t> m_uploadResult = new CallResult<LeaderboardScoreUploaded_t>();

        //Call result that will be set by DownloadLeaderboardEntries() method depending if the OnLeaderboardScoresDownloaded resul returns true or false.
        private static CallResult<LeaderboardScoresDownloaded_t> m_scoresDownloadedResult = new CallResult<LeaderboardScoresDownloaded_t>();

        #region public variables for editor use

        [Tooltip("teste")]
        public string leaderboardName;
        public int leaderboardEntriesRangeinit, leaderboardEntriesRangeEnd;
        public Transform leaderboardTarget;
        public GameObject leaderboardPrefab;
        public float distanceBetweenCells;
        public ELeaderboardUploadScoreMethod uploadScoreMethod;
        public ELeaderboardDataRequest requestType;
        #endregion

        private void Awake()
        {
            m_leaderboardName = leaderboardName;
            m_leaderboardEntriesRangeInit = leaderboardEntriesRangeinit;
            m_leaderboardEntriesRangeEnd = leaderboardEntriesRangeEnd;
            m_leaderboardPrefab = leaderboardPrefab;
            m_distanceBetweenCells = distanceBetweenCells;
            m_uploadScoreMethod = uploadScoreMethod;
            m_requestType = requestType;
            m_usingentriesFinished = false;
        }

        private void OnEnable()
        {
            if (!SteamManager.Initialized)
                return;
        }

        private void Start()
        {

            //m_Leaderboardtarget.transform.parent.gameObject.SetActive(false);
            //Find leaderboard for later usage

            FindLeaderboard();
        }

        /// <summary>
        /// Creates the leaderboard on the m_leaderboardTarget gameObject. 
        /// "this" is the normal parameter, it'll get the instance of the object monobehaviour to play the coroutine.
        /// </summary>
        /// <param name="_coroutineStart"></param>
        public static void InstantiateLeaderboard(MonoBehaviour _coroutineStart)
        {
            _coroutineStart.StartCoroutine(LeaderboardCreation());
        }

        //This method will download all leaderboard entries and use it to instantiate a new leaderboard.
        /// <summary>
        /// The leaderboard needed some time to download and use the data. DON'T USE THIS METHOD DIRECTLY
        /// </summary>
        /// <returns>nothing</returns>
        private static IEnumerator LeaderboardCreation()
        {
            if (!m_Leaderboardtarget)
                yield return null;
            else
            {
                DownloadLeaderboardEntries();
                yield return new WaitForSeconds(1f);
                UseDownloadedEntries();
                InstantiateNewLeaderboardCell();
            }

        }

        /// <summary>
        /// this method is used to search the leaderboard by name with a SteamAPICall
        /// </summary>
        public static void FindLeaderboard()
        {
            //this call finds the leaderboard by name (in this case the nam is already on a variable, but you could use a string normally)
            SteamAPICall_t _steamAPICall = SteamUserStats.FindLeaderboard(m_leaderboardName);


            //---------------------------------------------------------------------------------------------------------------------------
            //The variable "m_leaderboardFindResult" is a call result that needs to be used in conjuction with an "OnLeaderBoardFindResult" Method
            //if the result of the call is true, it'll change the m_steamLeaderboard
            //---------------------------------------------------------------------------------------------------------------------------
            m_leaderboardFindResult.Set(_steamAPICall, OnLeaderBoardFindResult);

        }

        /// <summary>
        /// This method download the leaderboard entries, with the range being m_leaderboardEntriesRangeInit to m_leaderboardEntriesRangeInit
        /// </summary>
        public static void DownloadLeaderboardEntries()
        {
            if (!m_leaderboardInitiated)
                Debug.Log("The leaderboard was not found!");
            else
            {
                SteamAPICall_t _steamAPICall = SteamUserStats.DownloadLeaderboardEntries(m_steamLeaderBoard, m_requestType, m_leaderboardEntriesRangeInit, m_leaderboardEntriesRangeEnd);
                m_scoresDownloadedResult.Set(_steamAPICall, OnLeaderBoardScoresDownloaded);
            }

        }

        /// <summary>
        /// This method makes use of the entries that the method "DownloadLeaderboardEntries" got
        /// </summary>
        public static void UseDownloadedEntries()
        {
            leaderboardPlayersList.Clear();
            for (int i = 0; i < m_leaderboardCount; i++)
            {
                LeaderboardEntry_t _LeaderboardEntry;
                Sprite _playerImage;

                //Returns the entry from "m_leaderboardEntries" using the method GetDownloadedLeaderboardEntry, modifying the variable _leaderboardEntry using the modifier out
                bool ret = SteamUserStats.GetDownloadedLeaderboardEntry(m_leaderboardEntries, i, out _LeaderboardEntry, null, 0);
                Debug.Log("Score: " + _LeaderboardEntry.m_nScore + " User ID: " + SteamFriends.GetFriendPersonaName(_LeaderboardEntry.m_steamIDUser));

                //The sprite image is generated on the FetchAvatar method, we pass the user that is on the current leaderboard entry
                _playerImage = FetchAvatar(_LeaderboardEntry.m_steamIDUser);

                if (!_playerImage)
                    _playerImage = FetchAvatar(_LeaderboardEntry.m_steamIDUser);

                /*---------------------------------------------------------------------------------------
                 * We insert into the LeaderboardPlayers list a new leaderboardCell that is composed by:
                 * public Sprite playerImage;
                 * public string playerName
                 * public int playerScore
                 * This list will be used in conjuction with the list of The cells prefab list, using this list as a reference 
                 ---------------------------------------------------------------------------------------*/
                leaderboardPlayersList.Insert(i, new LeaderboardCell(_playerImage, SteamFriends.GetFriendPersonaName(_LeaderboardEntry.m_steamIDUser),
                    _LeaderboardEntry.m_nScore));
            }
        }

        /*------------------------------------------------------------------------------------------------------------
         * For this method to work we need that those variables are not null
         * m_leaderboardPrefab
         * m_distanceBetweenCells
         * m_leaderboardTarget
         * leaderbordCellList must not be empty
         -------------------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// This method instantiates the cells of the leaderboard. Normally you'd use the InstantiateLeadeboard() method to create
        /// the leaderboard
        /// </summary>
        public static void InstantiateNewLeaderboardCell()
        {
            for (int i = 0; i < leaderboardPlayersList.Count; i++)
            {
                //Position where the cell will be
                Vector2 _cellPosition;

                //The new instace of the m_leaderboardPrefab
                GameObject _instantiatedCell;

                if (m_leaderboardPrefab)
                    _instantiatedCell = Instantiate(m_leaderboardPrefab);
                else
                {
                    _instantiatedCell = null;
                }

                //The parent of the newly instantiated cell is the m_leaderboardTarget
                _instantiatedCell.transform.parent = m_Leaderboardtarget.transform;

                //Checking if it is the first cell, if it is the first the position will be 0,0; If not, it'll be the last y - m_distancebetweencells
                //If it is ascending instead of descending, change the "-" for a "+"
                if (i == 0)
                    _cellPosition = new Vector2(0, 0);
                else
                    _cellPosition = new Vector2(leaderboardCellList[i - 1].transform.localPosition.x,
                                           leaderboardCellList[i - 1].transform.localPosition.y - m_distanceBetweenCells);

                _instantiatedCell.transform.localPosition = _cellPosition;

                //insert first so we can use it to change the variables
                leaderboardCellList.Insert(i, _instantiatedCell);

                Image _playerImage = leaderboardCellList[i].transform.GetComponentInChildren<Image>();
                Text[] _playerTexts = leaderboardCellList[i].transform.GetComponentsInChildren<Text>();

                _playerImage.sprite = leaderboardPlayersList[i].playerImage;
                _playerTexts[0].text = leaderboardPlayersList[i].playerName;
                _playerTexts[1].text = leaderboardPlayersList[i].playerScore.ToString();
            }
        }

        /// <summary>
        /// UpdateScore into the Steam leaderboards.
        /// The method is bool so you can use it as a condition. 
        /// <para/> True = Successfully updated; False = Wasn't updated
        /// <para/> E.g: if(SteamLeaderboard.UpdateScore(score) == true) { SteamLeaderboard.InstantiateLeaderboard(this) }
        /// </summary>
        /// <param name="_score"></param>
        public static bool UpdateScore(int _score)
        {
            //If the leaderboard wasn't initiated, it'll show an error. (this is set by the findLeaderboard method) 
            if (!m_leaderboardInitiated)
            {
                Debug.Log("!!!!!!!! The Leaderboard was not found! !!!!!!!");
                return false;
            }

            else
            {
                SteamAPICall_t _steamAPICall = SteamUserStats.UploadLeaderboardScore(m_steamLeaderBoard, m_uploadScoreMethod, _score, null, 0);
                m_uploadResult.Set(_steamAPICall, OnLeaderBoardUploadResult);
                return true;
            }

        }

        /// <summary>
        /// This method fetches an avatar from a certain user (CSteamID)
        /// </summary>
        /// <param name="_steamID"></param>
        /// <returns></returns>
        public static Sprite FetchAvatar(CSteamID _steamID)
        {
            int _avatarInt;
            uint _width, _height;

            Texture2D _downloadedAvatar;

            Rect _rect = new Rect(0, 0, 184, 184);

            Vector2 _pivot = new Vector2(0.5f, 0.5f);

            _avatarInt = SteamFriends.GetLargeFriendAvatar(_steamID);

            if (_avatarInt == -1)
            {
                for (int i = 0; i <= 2000; i++)
                {
                    Debug.Log("avatar not found");
                }
            }

            if (_avatarInt > 0)
            {
                SteamUtils.GetImageSize(_avatarInt, out _width, out _height);

                if (_width > 0 && _height > 0)
                {
                    byte[] _avatarStream = new byte[4 * (int)_width * (int)_height];

                    SteamUtils.GetImageRGBA(_avatarInt, _avatarStream, 4 * (int)_width * (int)_height);

                    _downloadedAvatar = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false);
                    _downloadedAvatar.LoadRawTextureData(_avatarStream);
                    _downloadedAvatar.Apply();

                    return (Sprite.Create(_downloadedAvatar, _rect, _pivot));
                }
            }

            return null;
        }


        #region callback methods
        static private void OnLeaderBoardFindResult(LeaderboardFindResult_t _callback, bool _IOFailure)
        {
            Debug.Log("STEAM LEADERBOARDS: Found - " + _callback.m_bLeaderboardFound + " leaderboardID - " + _callback.m_hSteamLeaderboard.m_SteamLeaderboard);
            m_steamLeaderBoard = _callback.m_hSteamLeaderboard;

            m_leaderboardInitiated = true;
        }

        //This method is just to check if the upload was successful and permits to debug some things like Failure, if the score was set, etc etc
        static private void OnLeaderBoardUploadResult(LeaderboardScoreUploaded_t _callback, bool _IOFailure)
        {
            Debug.Log("STEAM LEADERBOARDS: failure - " + _IOFailure + " Completed - " + _callback.m_bSuccess + " NewScore: " + _callback.m_nGlobalRankNew + " Score: " + _callback.m_nScore + " HasChanged - " + _callback.m_bScoreChanged);
        }

        //This checks if the leaderboard was successfully downloaded or not and updates the variables that'll be used in conjuction with the SteamAPI Handler
        static private void OnLeaderBoardScoresDownloaded(LeaderboardScoresDownloaded_t _callback, bool _IOFailure)
        {
            m_leaderboardEntries = _callback.m_hSteamLeaderboardEntries;
            m_leaderboardCount = _callback.m_cEntryCount;

            Debug.Log("Leaderboard: " + _callback.m_hSteamLeaderboard + " Entries: " + _callback.m_hSteamLeaderboardEntries + "Count: " + _callback.m_cEntryCount);
        }
        #endregion

        private void Update()
        {
            SteamAPI.RunCallbacks();
        }

    }
}

