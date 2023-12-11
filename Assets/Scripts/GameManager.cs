using System;
using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace TempCompany.MissileGame
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static Character LocalPlayerInstance = null;
        public static Character LocalEnemyInstance = null;
        public static GameManager Instance = null;

        public Transform hudMain;
        public Text finishLabel;
        public Text counterLabel;
        public int totalLeaveTime = 3;
        public int remainLeaveTime;
        public GameObject playerPrefab;
        private Stopwatch _leavetimer = new();

        public bool isPlayStart = false;

        public bool isSendFinishGame = false;
        public bool isReceiveFinishGame = false;
        public bool isLeaveStart = false;
        public bool isLeaveEnd = false;

        private void Awake()
        {
            Instance = this;
            finishLabel.gameObject.SetActive(false);
            counterLabel.gameObject.SetActive(false);
        }

        private void Update()
        {
            CheckStart();
            CheckFinish();
            UpdateCounter();
            ProcessLeave();
        }

        private void CheckStart()
        {
            if (isPlayStart)
                return;
            isPlayStart = null != LocalPlayerInstance && null != LocalEnemyInstance;
        }
        
        private void CheckFinish()
        {
            if (false == PhotonNetwork.IsMasterClient)
                return;
            if (false == isPlayStart)
                return;
            if (true == isSendFinishGame)
                return;
            if (true == isReceiveFinishGame)
                return;
            if (false == LocalPlayerInstance.IsLive() || false == LocalEnemyInstance.IsLive())
            {
                Debug.Log($"isFinished! IsMasterClient({PhotonNetwork.IsMasterClient}) ({photonView.IsMine})");
                isSendFinishGame = true;
                SendFinishGame();
            }
        }

        private void UpdateCounter()
        {
            if (false == isReceiveFinishGame)
                return;
            remainLeaveTime = totalLeaveTime - _leavetimer.Elapsed.Seconds;
            if (0 > remainLeaveTime)
            {
                LeaveRoom();
                _leavetimer.Stop();
                return;
            }

            string remainSecStr = remainLeaveTime.ToString();
            if (counterLabel.text.Equals(remainSecStr))
                return;
            counterLabel.text = remainSecStr;
        }

        private void ProcessLeave()
        {
            //Debug.Log($"LeaveRoom State : {PhotonNetwork.NetworkClientState}");
            if (PhotonNetwork.NetworkClientState == ClientState.Leaving)
                return;
            if (false == isLeaveStart)
                return;
            if (isLeaveEnd)
                return;
            PhotonNetwork.LeaveRoom();
            isLeaveEnd = true;
        }

        #region Photon Callbacks

        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(0);
            Debug.Log($"leave room");
        }

        public override void OnPlayerEnteredRoom(Player other)
        {
            if (false == PhotonNetwork.IsMasterClient)
                return;
            
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                Debug.Log($"OnPlayerEnteredRoom Room Full! {PhotonNetwork.CurrentRoom}"); // called before OnPlayerLeftRoom
                foreach (var actorId in PhotonNetwork.CurrentRoom.Players.Keys)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    Debug.Log($"actorid({actorId}) Player({PhotonNetwork.CurrentRoom.Players[actorId]}) sendSpawnMessage({PhotonNetwork.CurrentRoom.Players[actorId].NickName})({photonView.Controller.NickName})");
                    photonView.RPC(nameof(RpcSpawnMessage), RpcTarget.All, PhotonNetwork.CurrentRoom.Players[actorId]);
                }
            }
        }

        public override void OnPlayerLeftRoom(Player other)
        {
            if (false == PhotonNetwork.IsMasterClient)
                return;
            LoadArena();
        }

        [PunRPC]
        void RpcSpawnMessage(Player player)
        {
            if (false == PhotonNetwork.NickName.Equals(player.NickName))
                return;
            if (playerPrefab == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
                return;
            }
            PhotonNetwork.Instantiate(this.playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
        }

        [PunRPC]
        void RpcFinishGame(string nickName, int winnerViewId)
        {
            Debug.Log($"RpcFinishGame sender : {nickName}");
            string endStr = LocalPlayerInstance.photonView.ViewID == winnerViewId ? "Win!" : "Lose!";
            finishLabel.gameObject.SetActive(true);
            finishLabel.text = endStr;
            counterLabel.gameObject.SetActive(true);
            counterLabel.text = totalLeaveTime.ToString();
            Debug.Log($"finish : {endStr}");
            _leavetimer.Reset();
            _leavetimer.Start();
            isReceiveFinishGame = true;
        }

        #endregion

        #region Public Methods

        public void LeaveRoom()
        {
            isLeaveStart = true;
        }

        private void SendFinishGame()
        {
            int winnerViewId = LocalPlayerInstance.CurHealth > 0 ? LocalPlayerInstance.photonView.ViewID : LocalEnemyInstance.photonView.ViewID;
            photonView.RPC(nameof(RpcFinishGame), RpcTarget.All, photonView.Controller.NickName, winnerViewId);
        }

        #endregion

        #region Private Methods

        void LoadArena()
        {
            PhotonNetwork.LoadLevel("PlayRoom");
        }

        #endregion
    }
}