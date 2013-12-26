using System;
using RtmpSharp.IO;
using System.Collections.Generic;
using RtmpSharp.IO.AMF3;
using System.Text;

namespace LegendaryClient.Logic.Riot.Platform
{
    [Serializable]
    [SerializedName("com.riotgames.platform.systemstate.ClientSystemStatesNotification")]
    public class ClientSystemStatesNotification : IExternalizable
    {
        [SerializedName("championTradeThroughLCDS")]
        public Boolean ChampionTradeThroughLCDS { get; set; }

        [SerializedName("practiceGameEnabled")]
        public Boolean PracticeGameEnabled { get; set; }

        [SerializedName("advancedTutorialEnabled")]
        public Boolean AdvancedTutorialEnabled { get; set; }

        [SerializedName("minNumPlayersForPracticeGame")]
        public Int32 MinNumPlayersForPracticeGame { get; set; }

        [SerializedName("practiceGameTypeConfigIdList")]
        public Int32[] PracticeGameTypeConfigIdList { get; set; }

        [SerializedName("freeToPlayChampionIdList")]
        public Int32[] FreeToPlayChampionIdList { get; set; }

        [SerializedName("inactiveChampionIdList")]
        public object[] InactiveChampionIdList { get; set; }

        [SerializedName("inactiveSpellIdList")]
        public Int32[] InactiveSpellIdList { get; set; }

        [SerializedName("inactiveTutorialSpellIdList")]
        public Int32[] InactiveTutorialSpellIdList { get; set; }

        [SerializedName("inactiveClassicSpellIdList")]
        public Int32[] InactiveClassicSpellIdList { get; set; }

        [SerializedName("inactiveOdinSpellIdList")]
        public Int32[] InactiveOdinSpellIdList { get; set; }

        [SerializedName("inactiveAramSpellIdList")]
        public Int32[] InactiveAramSpellIdList { get; set; }

        [SerializedName("enabledQueueIdsList")]
        public Int32[] EnabledQueueIdsList { get; set; }

        [SerializedName("unobtainableChampionSkinIDList")]
        public Int32[] UnobtainableChampionSkinIDList { get; set; }

        [SerializedName("archivedStatsEnabled")]
        public Boolean ArchivedStatsEnabled { get; set; }

        [SerializedName("queueThrottleDTO")]
        public Dictionary<String, Object> QueueThrottleDTO { get; set; }

        [SerializedName("gameMapEnabledDTOList")]
        public List<Dictionary<String, Object>> GameMapEnabledDTOList { get; set; }

        [SerializedName("storeCustomerEnabled")]
        public Boolean StoreCustomerEnabled { get; set; }

        [SerializedName("socialIntegrationEnabled")]
        public Boolean SocialIntegrationEnabled { get; set; }

        [SerializedName("runeUniquePerSpellBook")]
        public Boolean RuneUniquePerSpellBook { get; set; }

        [SerializedName("tribunalEnabled")]
        public Boolean TribunalEnabled { get; set; }

        [SerializedName("observerModeEnabled")]
        public Boolean ObserverModeEnabled { get; set; }

        [SerializedName("spectatorSlotLimit")]
        public Int32 SpectatorSlotLimit { get; set; }

        [SerializedName("clientHeartBeatRateSeconds")]
        public Int32 ClientHeartBeatRateSeconds { get; set; }

        [SerializedName("observableGameModes")]
        public String[] ObservableGameModes { get; set; }

        [SerializedName("observableCustomGameModes")]
        public String ObservableCustomGameModes { get; set; }

        [SerializedName("teamServiceEnabled")]
        public Boolean TeamServiceEnabled { get; set; }

        [SerializedName("leagueServiceEnabled")]
        public Boolean LeagueServiceEnabled { get; set; }

        [SerializedName("modularGameModeEnabled")]
        public Boolean ModularGameModeEnabled { get; set; }

        [SerializedName("riotDataServiceDataSendProbability")]
        public int RiotDataServiceDataSendProbability { get; set; }

        [SerializedName("displayPromoGamesPlayedEnabled")]
        public Boolean DisplayPromoGamesPlayedEnabled { get; set; }

        [SerializedName("masteryPageOnServer")]
        public Boolean MasteryPageOnServer { get; set; }

        [SerializedName("maxMasteryPagesOnServer")]
        public Int32 MaxMasteryPagesOnServer { get; set; }

        [SerializedName("tournamentSendStatsEnabled")]
        public Boolean TournamentSendStatsEnabled { get; set; }

        [SerializedName("replayServiceAddress")]
        public String ReplayServiceAddress { get; set; }

        [SerializedName("kudosEnabled")]
        public Boolean KudosEnabled { get; set; }

        [SerializedName("buddyNotesEnabled")]
        public Boolean BuddyNotesEnabled { get; set; }

        [SerializedName("localeSpecificChatRoomsEnabled")]
        public Boolean LocaleSpecificChatRoomsEnabled { get; set; }

        [SerializedName("replaySystemStates")]
        public Dictionary<String, Object> ReplaySystemStates { get; set; }

        [SerializedName("sendFeedbackEventsEnabled")]
        public Boolean SendFeedbackEventsEnabled { get; set; }

        [SerializedName("knownGeographicGameServerRegions")]
        public String[] KnownGeographicGameServerRegions { get; set; }

        [SerializedName("leaguesDecayMessagingEnabled")]
        public Boolean LeaguesDecayMessagingEnabled { get; set; }

        public string Json { get; set; }

        public void ReadExternal(IDataInput input)
        {
            Json = input.ReadUtf((int)input.ReadUInt32());
        }

        public void WriteExternal(IDataOutput output)
        {
            var bytes = Encoding.UTF8.GetBytes(Json);

            output.WriteInt32(bytes.Length);
            output.WriteBytes(bytes);
        }
    }
}
