#if SCSOCIAL && UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;

namespace Softcen.Plugins
{
    public class SCSocialServicesAndroid : SCSocialServices
    {
        public override void Initialise()
        {
#if SOFTCEN_DEBUG
            Debug.Log(SCPlugins.TAG + "SCSocialServicesAndroid Initialise");
#endif
            base.Initialise();
            PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
#if SCCLOUD
                // enables saving game progress.
                .EnableSavedGames()
#endif
                // registers a callback to handle game invitations received while the game is not running.
                //.WithInvitationDelegate(< callback method >)
                // registers a callback for turn based match notifications received while the
                // game is not running.
                //.WithMatchDelegate(< callback method >)
                // require access to a player's Google+ social graph (usually not needed)
                //.RequireGooglePlus()
                .Build();

            PlayGamesPlatform.InitializeInstance(config);
            // recommended for debugging:
#if SOFTCEN_DEBUG
            PlayGamesPlatform.DebugLogEnabled = true;
#endif
            // Activate the Google Play Games platform
            PlayGamesPlatform.Activate();
        }

        public override bool IsInitializedAndAuthenticated()
        {
            if (!m_isInitialized)
            {
                Initialise();
            }
            return PlayGamesPlatform.Instance.IsAuthenticated();
        }

        public override bool isAuthenticated()
        {
            return PlayGamesPlatform.Instance.IsAuthenticated();
            //return Social.localUser.authenticated;
        }

        public override void SignIn()
        {
            // authenticate user:
            if (!IsInitializedAndAuthenticated())
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "Start SignIn");
#endif
                //Social.localUser.Authenticate((bool success) =>
                PlayGamesPlatform.Instance.Authenticate((bool success) =>
                {
#if SOFTCEN_DEBUG
                    Debug.Log(SCPlugins.TAG + "SignIn success: " + success.ToString());
#endif
                    if (success)
                    {
                        SocialStateChangedReport();
#if SCCLOUD
                        SCPlugins.CloudServices.Initialise();
#endif
                    }
                    else
                    {
                    }
                });
            }
        }

        public override void SignOut()
        {
            //if (Social.localUser.authenticated)
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "SignOut");
#endif
                PlayGamesPlatform.Instance.SignOut();
            }
        }


        public override void ReportAchievementProgress(string id, double progress)
        {
            if (IsInitializedAndAuthenticated())
            {
                //Social.ReportProgress(id, progress, (bool success) =>
                PlayGamesPlatform.Instance.ReportProgress(id, progress, (bool success) =>
                {
#if SOFTCEN_DEBUG
                    Debug.Log(SCPlugins.TAG + "ReportAchievementProgress success: " + success.ToString() + ", id: " + id + ", progress: " + progress);
#endif
                    if (success)
                    {
                    }
                    else
                    {
                    }
                });
            }
        }

        public override void ReportLeaderboardScore(string id, long score)
        {
            if (IsInitializedAndAuthenticated())
            {
                //Social.ReportScore(score, id, (bool success) =>
                PlayGamesPlatform.Instance.ReportScore(score, id, (bool success) =>
                {
#if SOFTCEN_DEBUG
                    Debug.Log(SCPlugins.TAG + "ReportLeaderboardScore success: " + success.ToString() + ", id: " + id + ", score: " + score);
#endif
                    if (success)
                    {
                    }
                    else
                    {
                    }
                });
            }
        }

        public override void ShowAchievementsUI()
        {
            if (IsInitializedAndAuthenticated())
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "ShowAchievementsUI");
#endif
                PlayGamesPlatform.Instance.ShowAchievementsUI();
                //Social.ShowAchievementsUI();
            }
        }

        public override void ShowLeaderboardUI()
        {
            if (IsInitializedAndAuthenticated())
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "ShowLeaderboardUI");
#endif
                PlayGamesPlatform.Instance.ShowLeaderboardUI();
                //Social.ShowLeaderboardUI();
            }
        }

        public override void ShowLeaderboardUI(string id)
        {
            if (IsInitializedAndAuthenticated())
            {
#if SOFTCEN_DEBUG
                Debug.Log(SCPlugins.TAG + "ShowLeaderboardUI id: " + id);
#endif
                PlayGamesPlatform.Instance.ShowLeaderboardUI(id);
            }
        }

    }
}
#endif
