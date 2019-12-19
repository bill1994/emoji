using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kyub.Reflection;
using System.Reflection;
using UnityEngine.UI.Collections;

namespace Kyub.Performance
{
    public class CanvasRebuildRegistry : CanvasUpdateRegistry
    {
        #region Events

        public static event System.Action OnWillPerformRebuild;

        #endregion

        #region Static Instance

        public new static CanvasRebuildRegistry instance
        {
            get
            {
                if (!(CanvasUpdateRegistry.instance is CanvasRebuildRegistry))
                {
                    CanvasUpdateRegistry v_invalidInstance = CanvasUpdateRegistry.instance;
                    var v_instanceField = typeof(CanvasUpdateRegistry).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
                    if (v_instanceField != null)
                    {
                        v_instanceField.SetValue(null, new CanvasRebuildRegistry(v_invalidInstance));
                    }
                }
                return CanvasUpdateRegistry.instance as CanvasRebuildRegistry;
            }
        }

        #endregion

        #region Fields

        List<ICanvasRebuildListener> m_listenerQueue = new List<ICanvasRebuildListener>();
        protected HashSet<ICanvasRebuildListener> _listenersToRefreshHash = new HashSet<ICanvasRebuildListener>();

        static FieldInfo s_layoutRebuildQueueInfo = null;
        IList<ICanvasElement> m_layoutRebuildQueue = null;

        static FieldInfo s_graphicRebuildQueueInfo = null;
        IList<ICanvasElement> m_graphicRebuildQueue = null;

        #endregion

        #region Internal Reflection Properties

        public IList<ICanvasElement> LayoutRebuildQueue
        {
            get
            {
                if (s_layoutRebuildQueueInfo == null)
                {
                    s_layoutRebuildQueueInfo = typeof(CanvasUpdateRegistry).GetField("m_LayoutRebuildQueue", BindingFlags.Instance | BindingFlags.NonPublic);

                }
                if (m_layoutRebuildQueue == null)
                    m_layoutRebuildQueue = s_layoutRebuildQueueInfo.GetValue(this) as IList<ICanvasElement>;

                return m_layoutRebuildQueue;
            }
        }

        public IList<ICanvasElement> GraphicRebuildQueue
        {
            get
            {
                if (s_graphicRebuildQueueInfo == null)
                    s_graphicRebuildQueueInfo = typeof(CanvasUpdateRegistry).GetField("m_GraphicRebuildQueue", BindingFlags.Instance | BindingFlags.NonPublic);
                if (m_graphicRebuildQueue == null)
                    m_graphicRebuildQueue = s_graphicRebuildQueueInfo.GetValue(this) as IList<ICanvasElement>;

                return m_graphicRebuildQueue;
            }
        }

        public IList<ICanvasRebuildListener> RegisteredListenerQueue
        {
            get
            {
                return m_listenerQueue;
            }
        }

        #endregion

        #region Constructors

        protected CanvasRebuildRegistry(CanvasUpdateRegistry p_baseInstanceTemplate) : base()
        {
            Canvas.WillRenderCanvases v_baseDelegate = null;

            //Unregister base PerformUpdate
            if (p_baseInstanceTemplate != null)
            {
                //Unregister events from base type
                var v_eventOnWillRenderCanvas = GetOnWillRenderCanvasEvent();
                if (v_eventOnWillRenderCanvas != null)
                {
                    var v_invocationList = v_eventOnWillRenderCanvas.GetInvocationList();
                    foreach (var v_delegate in v_invocationList)
                    {
                        if (v_delegate != null &&
                            v_delegate.Target is CanvasUpdateRegistry &&
                            (v_delegate.Method.Name == "PerformUpdate"))
                        {
                            Canvas.willRenderCanvases -= (Canvas.WillRenderCanvases)v_delegate;

                            //Save base delegate to force invoke after PerformRefresh
                            if (v_delegate.Target == this)
                            {
                                v_baseDelegate = (Canvas.WillRenderCanvases)v_delegate;
                            }
                        }
                    }
                }

                //Add invalid graphic elements
                if (GraphicRebuildQueue != null)
                {
                    var v_oldGraphics = s_graphicRebuildQueueInfo.GetValue(p_baseInstanceTemplate) as IList<ICanvasElement>;
                    if (v_oldGraphics != null)
                    {
                        for (int i = 0; i < v_oldGraphics.Count; i++)
                        {
                            var v_graphic = v_oldGraphics[i];
                            if (!GraphicRebuildQueue.Contains(v_graphic))
                                GraphicRebuildQueue.Add(v_graphic);
                        }
                    }
                }
                //Add invalid layout elements
                if (LayoutRebuildQueue != null)
                {
                    var v_oldLayouts = s_layoutRebuildQueueInfo.GetValue(p_baseInstanceTemplate) as IList<ICanvasElement>;
                    if (v_oldLayouts != null)
                    {
                        for (int i = 0; i < v_oldLayouts.Count; i++)
                        {
                            var v_layout = v_oldLayouts[i];
                            if (!LayoutRebuildQueue.Contains(v_layout))
                                LayoutRebuildQueue.Add(v_layout);
                        }
                    }
                }
            }

            //Register new PerformUpdate
            Canvas.willRenderCanvases += this.OnBeforePerformUpdate;
            if (v_baseDelegate != null)
                Canvas.willRenderCanvases += v_baseDelegate;
        }

        #endregion

        #region Public Functions (Static)

        public static void RegisterRebuildListener(ICanvasRebuildListener p_listener)
        {
            if (p_listener != null && !p_listener.IsDestroyed() && p_listener.transform != null &&
                !CanvasRebuildRegistry.instance.m_listenerQueue.Contains(p_listener))
            {
                CanvasRebuildRegistry.instance.m_listenerQueue.Add(p_listener);
            }
        }

        public static void UnregisterRebuildListener(ICanvasRebuildListener p_listener)
        {
            if (p_listener != null && !p_listener.IsDestroyed() && p_listener.transform != null)
            {
                var v_index = CanvasRebuildRegistry.instance.m_listenerQueue.IndexOf(p_listener);
                if(v_index >= 0)
                    CanvasRebuildRegistry.instance.m_listenerQueue.RemoveAt(v_index);
            }
        }

        #endregion

        #region Internal Helper Functions

        protected void OnBeforePerformUpdate()
        {
            var v_willRefresh = (GraphicRebuildQueue != null && GraphicRebuildQueue.Count > 0) || (LayoutRebuildQueue != null && LayoutRebuildQueue.Count > 0);

            //Call Invalidate
            if (v_willRefresh)
            {
                //Slow method that track invalidate on specific canvas
                if (m_listenerQueue.Count > 0)
                {
                    List<IList<ICanvasElement>> v_rebuildQueues = new List<IList<ICanvasElement>>() { GraphicRebuildQueue, LayoutRebuildQueue };

                    foreach (var v_queue in v_rebuildQueues)
                    {
                        //We founded every Listener so we can break
                        if (_listenersToRefreshHash.Count == m_listenerQueue.Count)
                            break;
                        if (v_queue != null)
                        {
                            for (int i = 0; i < v_queue.Count; i++)
                            {
                                //Skip if we found all listeners
                                if (_listenersToRefreshHash.Count == m_listenerQueue.Count)
                                    break;
                                var v_canvasElement = v_queue[i];
                                if (v_canvasElement != null && !v_canvasElement.IsDestroyed())
                                {
                                    var v_listener = v_canvasElement.transform.GetComponentInParent<ICanvasRebuildListener>();
                                    if (v_listener != null && !v_listener.IsDestroyed() && !_listenersToRefreshHash.Contains(v_listener))
                                        _listenersToRefreshHash.Add(v_listener);
                                }
                            }
                        }
                    }

                    //Call Rebuild in all listeners
                    foreach (var v_listener in _listenersToRefreshHash)
                    {
                        if (v_listener != null && !v_listener.IsDestroyed())
                            v_listener.OnCanvasRebuild();
                    }
                    _listenersToRefreshHash.Clear();
                }

                //Call fast invalidate
                if (OnWillPerformRebuild != null)
                    OnWillPerformRebuild.Invoke();
            }
        }

        #endregion

        #region Internal Reflection CanvasEvent Functions

        static Canvas.WillRenderCanvases GetOnWillRenderCanvasEvent()
        {
            System.Type classType = typeof(Canvas);
            FieldInfo eventField = classType.GetField("willRenderCanvases", BindingFlags.GetField
                                                               | BindingFlags.Public
                                                               | BindingFlags.NonPublic
                                                               | BindingFlags.Static);

            Canvas.WillRenderCanvases eventValue = (Canvas.WillRenderCanvases)eventField.GetValue(null);

            // eventDelegate will be null if no listeners are attached to the event
            if (eventValue == null)
            {
                return null;
            }

            return eventValue;
        }

        #endregion
    }

    #region Helper Interfaces

    public interface ICanvasRebuildListener
    {
        Transform transform { get; }

        void OnCanvasRebuild();
        bool IsDestroyed();
    }

    #endregion
}
