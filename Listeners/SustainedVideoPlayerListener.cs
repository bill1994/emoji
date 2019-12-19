using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;

namespace Kyub.Performance
{
    [DisallowMultipleComponent]
    public class SustainedVideoPlayerListener : SustainedTransformListener
    {
        #region Public Properties

        public override bool RequiresConstantRepaint
        {
            get
            {
                var v_requireRepaint = base.RequiresConstantRepaint;
                if(!v_requireRepaint)
                    v_requireRepaint = GetComponent<VideoPlayer>().isPlaying;

                return v_requireRepaint;
            }

            set
            {
                base.RequiresConstantRepaint = value;
            }
        }

        #endregion

        #region Constructors

        public SustainedVideoPlayerListener()
        {
            m_intervalFramerate = 0;
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            RegisterEvents();
            base.OnEnable();
            MarkDynamicElementDirty();
        }

        protected override void OnDisable()
        {
            UnregisterEvents();
            base.OnDisable();
        }

        #endregion

        #region Helper Functions

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            var v_videoPlayer = GetComponent<VideoPlayer>();
            if(v_videoPlayer != null)
            {
                v_videoPlayer.started += HandleOnVideoStarted;
                v_videoPlayer.loopPointReached += HandleOnVideoEnd;
            }
        }

        protected virtual void UnregisterEvents()
        {
            var v_videoPlayer = GetComponent<VideoPlayer>();
            if (v_videoPlayer != null)
            {
                v_videoPlayer.started -= HandleOnVideoStarted;
                v_videoPlayer.loopPointReached -= HandleOnVideoEnd;
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void HandleOnVideoStarted(VideoPlayer source)
        {
            MarkDynamicElementDirty();
        }

        protected virtual void HandleOnVideoEnd(VideoPlayer source)
        {
            if(!source.isLooping)
                MarkDynamicElementDirty();
        }

        #endregion
    }
}
