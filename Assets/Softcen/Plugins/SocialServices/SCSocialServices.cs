#if SCSOCIAL
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Softcen.Plugins
{
    public class SCSocialServices : MonoBehaviour
    {
        protected bool m_isInitialized;

        public delegate void SocialStateChanged();
        public static event SocialStateChanged SocialStateChangedEvent;

        /// <summary>
        /// Initialises the component.
        /// </summary>
        ///	<remarks> 
        /// \note You need to call this method, before using any features. 
        /// </remarks>
        public virtual void Initialise()
        {
            m_isInitialized = true;
        }

        public bool IsInitialized
        {
            get { return m_isInitialized; }
        }

        public virtual bool IsInitializedAndAuthenticated()
        {
            return false;
        }

        public virtual bool isAuthenticated()
        {
            return false;
        }

        public virtual void SignIn()
        { }

        public virtual void SignOut()
        { }

        public virtual void ReportAchievementProgress(string id, double progress)
        { }

        public virtual void ReportLeaderboardScore(string id, long score)
        { }

        public virtual void ShowAchievementsUI()
        { }

        public virtual void ShowLeaderboardUI()
        { }

        public virtual void ShowLeaderboardUI(string id)
        { }

        protected void SocialStateChangedReport()
        {
            if (SocialStateChangedEvent != null)
            {
                SocialStateChangedEvent();
            }
        }
    }
}
#endif
