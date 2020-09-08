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
                var requireRepaint = base.RequiresConstantRepaint;
                if(!requireRepaint)
                    requireRepaint = GetComponent<VideoPlayer>().isPlaying;

                return requireRepaint;
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
            var videoPlayer = GetComponent<VideoPlayer>();
            if(videoPlayer != null)
            {
                videoPlayer.started += HandleOnVideoStarted;
                videoPlayer.loopPointReached += HandleOnVideoEnd;
            }
        }

        protected virtual void UnregisterEvents()
        {
            var videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.started -= HandleOnVideoStarted;
                videoPlayer.loopPointReached -= HandleOnVideoEnd;
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
