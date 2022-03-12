using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class SteamLobbyManager : SteamManager
{
    #region SINGLETON
    public static SteamLobbyManager Singleton { get; protected set; }
    public bool Online { get {return lobby_id!=null;} }

    public CSteamID? lobby_id {get; private set;}= null;

    public void Start()
    {
        if (Singleton == null)
            Singleton = this;
        else
        {
            Debug.LogError("Duplicate SteamLobbyManager");
            Application.Quit();
        }
    }
    #endregion SINGLETON

    public string GetSteamHostId(){
        if (this.lobby_id != null){
            CSteamID steamIDHost = SteamMatchmaking.GetLobbyOwner( (CSteamID)this.lobby_id );
            string friend_name = SteamFriends.GetFriendPersonaName(steamIDHost);
            return steamIDHost.m_SteamID.ToString();
        }
        return null;
    }

    internal void InviteSteamUser(string steam_id)
    {
        ulong steam_id_ulong;
        if (ulong.TryParse(steam_id, out steam_id_ulong)){
            CSteamID user_steam_id = new CSteamID(steam_id_ulong);
            SteamFriends.ActivateGameOverlayToUser("steamid", user_steam_id);
        }
    }

    public void InviteFriends()
    {
        if (this.lobby_id != null)
            SteamFriends.ActivateGameOverlayInviteDialog((CSteamID)this.lobby_id);
    }

    internal string GetLobbyInfo()
    {
        string pchValue = "";
        if (this.lobby_id != null){
            //SteamMatchmaking.GetLobbyData((CSteamID)this.lobby_id);
            int nDataCount = SteamMatchmaking.GetLobbyDataCount((CSteamID)this.lobby_id);
            if (nDataCount > 10) nDataCount = 10;
            string pchKey;
            for (int i = 0; i < nDataCount; ++i)
            {
                bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(
                    (CSteamID)this.lobby_id, i, 
                    out pchKey,
                     Constants.k_nMaxLobbyKeyLength, 
                    out pchValue,
                     Constants.k_cubChatMetadataMax);
                if (!lobbyDataRet)
                {
                    Debug.LogError("SteamMatchmaking.GetLobbyDataByIndex returned false.");
                    continue;
                }
                Debug.Log(pchKey);
                Debug.Log(pchValue);

            }
        }
        return pchValue;
        
    }

    public List<string> GetSteamUserIds(){
        List<string> steam_ids = new List<string>();
        if (this.lobby_id != null){
            for (int member_idx = 0; member_idx < SteamMatchmaking.GetLobbyMemberLimit((CSteamID)this.lobby_id); member_idx++){
                ulong steam_id = SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)this.lobby_id, member_idx).m_SteamID;
                if (steam_id > 0)
                    steam_ids.Add(steam_id.ToString());
            }
        }
        return steam_ids;
    }
    public void OpenLobbyAsync(){
        this.OpenLobbyAsync(null);
    }
    public void OpenLobbyAsync(Action<LobbyCreated_t, bool> onLobbyCreated_callback=null)
    {
		CallResult<LobbyCreated_t> m_LobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(StoreNewLobbyData);
        if (onLobbyCreated_callback!=null)
            this.event_lobby_created+=onLobbyCreated_callback;
        m_LobbyCreatedCallResult.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 8));
        Debug.Log(this.GetType()+": Opening lobby...");
    }
    private void StoreNewLobbyData(LobbyCreated_t pCallback, bool bIOFailure){
        this.lobby_id = (CSteamID)pCallback.m_ulSteamIDLobby;
        Debug.Log(this.GetType()+": Lobby opened");
        this.event_lobby_created?.Invoke(pCallback, bIOFailure);
    }
    private Action<LobbyCreated_t, bool> event_lobby_created;
}
