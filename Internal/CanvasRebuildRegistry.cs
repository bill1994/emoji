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
                    CanvasUpdateRegistry invalidInstance = CanvasUpdateRegistry.instance;
                    var instanceField = typeof(CanvasUpdateRegistry).GetField("s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
                    if (instanceField != null)
                    {
                        instanceField.SetValue(null, new CanvasRebuildRegistry(invalidInstance));
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

        Canvas.WillRenderCanvases OnBasePerformUpdate = null;
        protected CanvasRebuildRegistry(CanvasUpdateRegistry baseInstanceTemplate) : base()
        {
            //Unregister base PerformUpdate
            if (baseInstanceTemplate != null)
            {
                //Unregister events from base type
                var eventOnWillRenderCanvas = GetOnWillRenderCanvasEvent();
                if (eventOnWillRenderCanvas != null)
                {
                    var invocationList = eventOnWillRenderCanvas.GetInvocationList();
                    foreach (var actionDelegate in invocationList)
                    {
                        if (actionDelegate != null &&
                            actionDelegate.Target is CanvasUpdateRegistry &&
                            (actionDelegate.Method.Name == "PerformUpdate"))
                        {
                            Canvas.willRenderCanvases -= (Canvas.WillRenderCanvases)actionDelegate;

                            //Save base delegate to force invoke after PerformRefresh
                            if (actionDelegate.Target == this)
                            {
                                OnBasePerformUpdate = (Canvas.WillRenderCanvases)actionDelegate;
                            }
                        }
                    }
                }

                //Add invalid graphic elements
                if (GraphicRebuildQueue != null)
                {
                    var oldGraphics = s_graphicRebuildQueueInfo.GetValue(baseInstanceTemplate) as IList<ICanvasElement>;
                    if (oldGraphics != null)
                    {
                        for (int i = 0; i < oldGraphics.Count; i++)
                        {
                            var graphic = oldGraphics[i];
                            if (!GraphicRebuildQueue.Contains(graphic))
                                GraphicRebuildQueue.Add(graphic);
                        }
                    }
                }
                //Add invalid layout elements
                if (LayoutRebuildQueue != null)
                {
                    var oldLayouts = s_layoutRebuildQueueInfo.GetValue(baseInstanceTemplate) as IList<ICanvasElement>;
                    if (oldLayouts != null)
                    {
                        for (int i = 0; i < oldLayouts.Count; i++)
                        {
                            var layout = oldLayouts[i];
                            if (!LayoutRebuildQueue.Contains(layout))
                                LayoutRebuildQueue.Add(layout);
                        }
                    }
                }
            }

            //Register new PerformUpdate
            Canvas.willRenderCanvases -= this.OnBeforePerformUpdate;
            Canvas.willRenderCanvases += this.OnBeforePerformUpdate;
        }

        #endregion

        #region Public Functions (Static)

        public static void RegisterRebuildListener(ICanvasRebuildListener listener)
        {
            if (listener != null && !listener.IsDestroyed() && listener.transform != null &&
                !CanvasRebuildRegistry.instance.m_listenerQueue.Contains(listener))
            {
                CanvasRebuildRegistry.instance.m_listenerQueue.Add(listener);
            }
        }

        public static void UnregisterRebuildListener(ICanvasRebuildListener listener)
        {
            if (listener != null && !listener.IsDestroyed() && listener.transform != null)
            {
                var index = CanvasRebuildRegistry.instance.m_listenerQueue.IndexOf(listener);
                if(index >= 0)
                    CanvasRebuildRegistry.instance.m_listenerQueue.RemoveAt(index);
            }
        }

        #endregion

        #region Internal Helper Functions

        protected void OnBeforePerformUpdate()
        {
            var willRefresh = (GraphicRebuildQueue != null && GraphicRebuildQueue.Count > 0) || (LayoutRebuildQueue != null && LayoutRebuildQueue.Count > 0);

            //Call Invalidate
            if (willRefresh)
            {
                //Slow method that track invalidate on specific canvas
                if (m_listenerQueue.Count > 0)
                {
                    List<IList<ICanvasElement>> rebuildQueues = new List<IList<ICanvasElement>>() { GraphicRebuildQueue, LayoutRebuildQueue };

                    foreach (var queue in rebuildQueues)
                    {
                        //We founded every Listener so we can break
                        if (_listenersToRefreshHash.Count == m_listenerQueue.Count)
                            break;
                        if (queue != null)
                        {
                            for (int i = 0; i < queue.Count; i++)
                            {
                                //Skip if we found all listeners
                                if (_listenersToRefreshHash.Count == m_listenerQueue.Count)
                                    break;
                                var canvasElement = queue[i];
                                if (canvasElement != null && !canvasElement.IsDestroyed())
                                {
                                    var listener = canvasElement.transform.GetComponentInParent<ICanvasRebuildListener>();
                                    if (listener != null && !listener.IsDestroyed() && !_listenersToRefreshHash.Contains(listener))
                                        _listenersToRefreshHash.Add(listener);
                                }
                            }
                        }
                    }

                    //Call Rebuild in all listeners
                    foreach (var listener in _listenersToRefreshHash)
                    {
                        if (listener != null && !listener.IsDestroyed())
                            listener.OnCanvasRebuild();
                    }
                    _listenersToRefreshHash.Clear();
                }

                //Call fast invalidate
                if (OnWillPerformRebuild != null)
                    OnWillPerformRebuild.Invoke();
            }

            if (OnBasePerformUpdate != null)
                OnBasePerformUpdate.Invoke();
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
