/*******************************************************************************
*                                                                              *
* Author    :  Angus Johnson                                                   *
* Version   :  6.4.2                                                           *
* Date      :  27 February 2017                                                *
* Website   :  http://www.angusj.com                                           *
* Copyright :  Angus Johnson 2010-2017                                         *
*                                                                              *
* License:                                                                     *
* Use, modification & distribution is subject to Boost Software License Ver 1. *
* http://www.boost.org/LICENSE_1_0.txt                                         *
*                                                                              *
* Attributions:                                                                *
* The code in this library is an extension of Bala Vatti's clipping algorithm: *
* "A generic solution to polygon clipping"                                     *
* Communications of the ACM, Vol 35, Issue 7 (July 1992) pp 56-63.             *
* http://portal.acm.org/citation.cfm?id=129906                                 *
*                                                                              *
* Computer graphics and geometric modeling: implementation and algorithms      *
* By Max K. Agoston                                                            *
* Springer; 1 edition (January 4, 2005)                                        *
* http://books.google.com/books?q=vatti+clipping+agoston                       *
*                                                                              *
* See also:                                                                    *
* "Polygon Offsetting by Computing Winding Numbers"                            *
* Paper no. DETC2005-85513 pp. 565-575                                         *
* ASME 2005 International Design Engineering Technical Conferences             *
* and Computers and Information in Engineering Conference (IDETC/CIE2005)      *
* September 24-28, 2005 , Long Beach, California, USA                          *
* http://www.me.berkeley.edu/~mcmains/pubs/DAC05OffsetPolygon.pdf              *
*                                                                              *
*******************************************************************************/

/*******************************************************************************
*                                                                              *
* This is a translation of the Delphi Clipper library and the naming style     *
* used has retained a Delphi flavour.                                          *
*                                                                              *
*******************************************************************************/

//use_lines: Enables open path clipping. Adds a very minor cost to performance.
#define use_lines

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Kyub.ExternLibs.ClipperLib
{
  //------------------------------------------------------------------------------
  // PolyTree & PolyNode classes
  //------------------------------------------------------------------------------

  public class PolyTree : PolyNode
  {
      internal List<PolyNode> m_AllPolys = new List<PolyNode>();

      //The GC probably handles this cleanup more efficiently ...
      //~PolyTree(){Clear();}
        
      public void Clear() 
      {
          for (int i = 0; i < m_AllPolys.Count; i++)
              m_AllPolys[i] = null;
          m_AllPolys.Clear(); 
          m_Childs.Clear(); 
      }
        
      public PolyNode GetFirst()
      {
          if (m_Childs.Count > 0)
              return m_Childs[0];
          else
              return null;
      }

      public int Total
      {
          get 
          { 
            int result = m_AllPolys.Count;
            //with negative offsets, ignore the hidden outer polygon ...
            if (result > 0 && m_Childs[0] != m_AllPolys[0]) result--;
            return result;
          }
      }

  }
        
  public class PolyNode 
  {
      internal PolyNode m_Parent;
      internal List<Vector2> m_polygon = new List<Vector2>();
      internal int m_Index;
      internal JoinType m_jointype;
      internal EndType m_endtype;
      internal List<PolyNode> m_Childs = new List<PolyNode>();

      private bool IsHoleNode()
      {
          bool result = true;
          PolyNode node = m_Parent;
          while (node != null)
          {
              result = !result;
              node = node.m_Parent;
          }
          return result;
      }

      public int ChildCount
      {
          get { return m_Childs.Count; }
      }

      public List<Vector2> Contour
      {
          get { return m_polygon; }
      }

      internal void AddChild(PolyNode Child)
      {
          int cnt = m_Childs.Count;
          m_Childs.Add(Child);
          Child.m_Parent = this;
          Child.m_Index = cnt;
      }

      public PolyNode GetNext()
      {
          if (m_Childs.Count > 0) 
              return m_Childs[0]; 
          else
              return GetNextSiblingUp();        
      }
  
      internal PolyNode GetNextSiblingUp()
      {
          if (m_Parent == null)
              return null;
          else if (m_Index == m_Parent.m_Childs.Count - 1)
              return m_Parent.GetNextSiblingUp();
          else
              return m_Parent.m_Childs[m_Index + 1];
      }

      public List<PolyNode> Childs
      {
          get { return m_Childs; }
      }

      public PolyNode Parent
      {
          get { return m_Parent; }
      }

      public bool IsHole
      {
          get { return IsHoleNode(); }
      }

      public bool IsOpen { get; set; }
  }

  //------------------------------------------------------------------------------
  //------------------------------------------------------------------------------

  public enum ClipType { ctIntersection, ctUnion, ctDifference, ctXor };
  public enum PolyType { ptSubject, ptClip };
  
  //By far the most widely used winding rules for polygon filling are
  //EvenOdd & NonZero (GDI, GDI+, XLib, OpenGL, Cairo, AGG, Quartz, SVG, Gr32)
  //Others rules include Positive, Negative and ABS_GTR_EQ_TWO (only in OpenGL)
  //see http://glprogramming.com/red/chapter11.html
  public enum PolyFillType { pftEvenOdd, pftNonZero, pftPositive, pftNegative };
  
  public enum JoinType { jtSquare, jtRound, jtMiter };
  public enum EndType { etClosedPolygon, etClosedLine, etOpenButt, etOpenSquare, etOpenRound };

  internal enum EdgeSide {esLeft, esRight};
  internal enum Direction {dRightToLeft, dLeftToRight};
    
  internal class TEdge {
    internal Vector2 Bot;
    internal Vector2 Curr; //current (updated for every new scanbeam)
    internal Vector2 Top;
    internal Vector2 Delta;
    internal double Dx;
    internal PolyType PolyTyp;
    internal EdgeSide Side; //side only refers to current side of solution poly
    internal int WindDelta; //1 or -1 depending on winding direction
    internal int WindCnt;
    internal int WindCnt2; //winding count of the opposite polytype
    internal int OutIdx;
    internal TEdge Next;
    internal TEdge Prev;
    internal TEdge NextInLML;
    internal TEdge NextInAEL;
    internal TEdge PrevInAEL;
    internal TEdge NextInSEL;
    internal TEdge PrevInSEL;
  };

  public class IntersectNode
  {
      internal TEdge Edge1;
      internal TEdge Edge2;
      internal Vector2 Pt;
  };

  public class MyIntersectNodeSort : IComparer<IntersectNode>
  {
    public int Compare(IntersectNode node1, IntersectNode node2)
    {
      float i = node2.Pt.y - node1.Pt.y;
      if (i > 0) return 1;
      else if (i < 0) return -1;
      else return 0;
    }
  }

  internal class LocalMinima
  {
    internal float y;
    internal TEdge LeftBound;
    internal TEdge RightBound;
    internal LocalMinima Next;
  };

  internal class Scanbeam
  {
      internal float y;
      internal Scanbeam Next;
  };

  internal class Maxima
  {
      internal float x;
      internal Maxima Next;
      internal Maxima Prev;
  };

  //OutRec: contains a path in the clipping solution. Edges in the AEL will
  //carry a pointer to an OutRec when they are part of the clipping solution.
  internal class OutRec
  {
    internal int Idx;
    internal bool IsHole;
    internal bool IsOpen;
    internal OutRec FirstLeft; //see comments in clipper.pas
    internal OutPt Pts;
    internal OutPt BottomPt;
    internal PolyNode PolyNode;
  };

  internal class OutPt
  {
    internal int Idx;
    internal Vector2 Pt;
    internal OutPt Next;
    internal OutPt Prev;
  };

  internal class Join
  {
    internal OutPt OutPt1;
    internal OutPt OutPt2;
    internal Vector2 OffPt;
  };

  public class ClipperBase
  {    
    internal const double horizontal = -3.4E+38;
    internal const int Skip = -2;
    internal const int Unassigned = -1;
    internal const double tolerance = 1.0E-20;
    internal static bool near_zero(double val){return (val > -tolerance) && (val < tolerance);}

    public const float loRange = 0x3FFFFFFF;
    public const float hiRange = 0x3FFFFFFFFFFFFFFFL; 

    internal LocalMinima m_MinimaList;
    internal LocalMinima m_CurrentLM;
    internal List<List<TEdge>> m_edges = new List<List<TEdge>>();
    internal Scanbeam m_Scanbeam;
    internal List<OutRec> m_PolyOuts;
    internal TEdge m_ActiveEdges;
    internal bool m_UseFullRange;
    internal bool m_HasOpenPaths;

    //------------------------------------------------------------------------------

    public bool PreserveCollinear
    {
      get;
      set;
    }
    //------------------------------------------------------------------------------

    public void Swap(ref float val1, ref float val2)
    {
      float tmp = val1;
      val1 = val2;
      val2 = tmp;
    }
    //------------------------------------------------------------------------------

    internal static bool IsHorizontal(TEdge e)
    {
      return e.Delta.y == 0;
    }
    //------------------------------------------------------------------------------

    internal bool PointIsVertex(Vector2 pt, OutPt pp)
    {
      OutPt pp2 = pp;
      do
      {
        if (pp2.Pt == pt) return true;
        pp2 = pp2.Next;
      }
      while (pp2 != pp);
      return false;
    }
    //------------------------------------------------------------------------------

    internal bool PointOnLineSegment(Vector2 pt, 
        Vector2 linePt1, Vector2 linePt2, bool UseFullRange)
    {
        return ((pt.x == linePt1.x) && (pt.y == linePt1.y)) ||
          ((pt.x == linePt2.x) && (pt.y == linePt2.y)) ||
          (((pt.x > linePt1.x) == (pt.x < linePt2.x)) &&
          ((pt.y > linePt1.y) == (pt.y < linePt2.y)) &&
          ((pt.x - linePt1.x) * (linePt2.y - linePt1.y) ==
            (linePt2.x - linePt1.x) * (pt.y - linePt1.y)));
    }
    //------------------------------------------------------------------------------

    internal bool PointOnPolygon(Vector2 pt, OutPt pp, bool UseFullRange)
    {
      OutPt pp2 = pp;
      while (true)
      {
        if (PointOnLineSegment(pt, pp2.Pt, pp2.Next.Pt, UseFullRange))
          return true;
        pp2 = pp2.Next;
        if (pp2 == pp) break;
      }
      return false;
    }
    //------------------------------------------------------------------------------

    internal static bool SlopesEqual(TEdge e1, TEdge e2, bool UseFullRange)
    {
        return (float)(e1.Delta.y) * (e2.Delta.x) ==
          (float)(e1.Delta.x) * (e2.Delta.y);
    }
    //------------------------------------------------------------------------------

    internal static bool SlopesEqual(Vector2 pt1, Vector2 pt2,
        Vector2 pt3, bool UseFullRange)
    {
        return
          (float)(pt1.y - pt2.y) * (pt2.x - pt3.x) - (float)(pt1.x - pt2.x) * (pt2.y - pt3.y) == 0;
    }
    //------------------------------------------------------------------------------

    internal static bool SlopesEqual(Vector2 pt1, Vector2 pt2,
        Vector2 pt3, Vector2 pt4, bool UseFullRange)
    {
        return
          (float)(pt1.y - pt2.y) * (pt3.x - pt4.x) - (float)(pt1.x - pt2.x) * (pt3.y - pt4.y) == 0;
    }
    //------------------------------------------------------------------------------

    internal ClipperBase() //constructor (nb: no external instantiation)
    {
        m_MinimaList = null;
        m_CurrentLM = null;
        m_UseFullRange = false;
        m_HasOpenPaths = false;
    }
    //------------------------------------------------------------------------------

    public virtual void Clear()
    {
        DisposeLocalMinimaList();
        for (int i = 0; i < m_edges.Count; ++i)
        {
            for (int j = 0; j < m_edges[i].Count; ++j) m_edges[i][j] = null;
            m_edges[i].Clear();
        }
        m_edges.Clear();
        m_UseFullRange = false;
        m_HasOpenPaths = false;
    }
    //------------------------------------------------------------------------------

    private void DisposeLocalMinimaList()
    {
        while( m_MinimaList != null )
        {
            LocalMinima tmpLm = m_MinimaList.Next;
            m_MinimaList = null;
            m_MinimaList = tmpLm;
        }
        m_CurrentLM = null;
    }
    //------------------------------------------------------------------------------

    void RangeTest(Vector2 Pt, ref bool useFullRange)
    {
      if (useFullRange)
      {
        if (Pt.x > hiRange || Pt.y > hiRange || -Pt.x > hiRange || -Pt.y > hiRange) 
          throw new ClipperException("Coordinate outside allowed range");
      }
      else if (Pt.x > loRange || Pt.y > loRange || -Pt.x > loRange || -Pt.y > loRange) 
      {
        useFullRange = true;
        RangeTest(Pt, ref useFullRange);
      }
    }
    //------------------------------------------------------------------------------

    private void InitEdge(TEdge e, TEdge eNext,
      TEdge ePrev, Vector2 pt)
    {
      e.Next = eNext;
      e.Prev = ePrev;
      e.Curr = pt;
      e.OutIdx = Unassigned;
    }
    //------------------------------------------------------------------------------

    private void InitEdge2(TEdge e, PolyType polyType)
    {
      if (e.Curr.y >= e.Next.Curr.y)
      {
        e.Bot = e.Curr;
        e.Top = e.Next.Curr;
      }
      else
      {
        e.Top = e.Curr;
        e.Bot = e.Next.Curr;
      }
      SetDx(e);
      e.PolyTyp = polyType;
    }
    //------------------------------------------------------------------------------

    private TEdge FindNextLocMin(TEdge E)
    {
      TEdge E2;
      for (;;)
      {
        while (E.Bot != E.Prev.Bot || E.Curr == E.Top) E = E.Next;
        if (E.Dx != horizontal && E.Prev.Dx != horizontal) break;
        while (E.Prev.Dx == horizontal) E = E.Prev;
        E2 = E;
        while (E.Dx == horizontal) E = E.Next;
        if (E.Top.y == E.Prev.Bot.y) continue; //ie just an intermediate horz.
        if (E2.Prev.Bot.x < E.Bot.x) E = E2;
        break;
      }
      return E;
    }
    //------------------------------------------------------------------------------

    private TEdge ProcessBound(TEdge E, bool LeftBoundIsForward)
    {
      TEdge EStart, Result = E;
      TEdge Horz;

      if (Result.OutIdx == Skip)
      {
        //check if there are edges beyond the skip edge in the bound and if so
        //create another LocMin and calling ProcessBound once more ...
        E = Result;
        if (LeftBoundIsForward)
        {
          while (E.Top.y == E.Next.Bot.y) E = E.Next;
          while (E != Result && E.Dx == horizontal) E = E.Prev;
        }
        else
        {
          while (E.Top.y == E.Prev.Bot.y) E = E.Prev;
          while (E != Result && E.Dx == horizontal) E = E.Next;
        }
        if (E == Result)
        {
          if (LeftBoundIsForward) Result = E.Next;
          else Result = E.Prev;
        }
        else
        {
          //there are more edges in the bound beyond result starting with E
          if (LeftBoundIsForward)
            E = Result.Next;
          else
            E = Result.Prev;
          LocalMinima locMin = new LocalMinima();
          locMin.Next = null;
          locMin.y = E.Bot.y;
          locMin.LeftBound = null;
          locMin.RightBound = E;
          E.WindDelta = 0;
          Result = ProcessBound(E, LeftBoundIsForward);
          InsertLocalMinima(locMin);
        }
        return Result;
      }

      if (E.Dx == horizontal)
      {
        //We need to be careful with open paths because this may not be a
        //true local minima (ie E may be following a skip edge).
        //Also, consecutive horz. edges may start heading left before going right.
        if (LeftBoundIsForward) EStart = E.Prev;
        else EStart = E.Next;
        if (EStart.Dx == horizontal) //ie an adjoining horizontal skip edge
        {
        if (EStart.Bot.x != E.Bot.x && EStart.Top.x != E.Bot.x)
            ReverseHorizontal(E);
        }
        else if (EStart.Bot.x != E.Bot.x)
        ReverseHorizontal(E);
      }

      EStart = E;
      if (LeftBoundIsForward)
      {
        while (Result.Top.y == Result.Next.Bot.y && Result.Next.OutIdx != Skip)
          Result = Result.Next;
        if (Result.Dx == horizontal && Result.Next.OutIdx != Skip)
        {
          //nb: at the top of a bound, horizontals are added to the bound
          //only when the preceding edge attaches to the horizontal's left vertex
          //unless a Skip edge is encountered when that becomes the top divide
          Horz = Result;
          while (Horz.Prev.Dx == horizontal) Horz = Horz.Prev;
          if (Horz.Prev.Top.x > Result.Next.Top.x) Result = Horz.Prev;
        }
        while (E != Result)
        {
          E.NextInLML = E.Next;
          if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Prev.Top.x) 
            ReverseHorizontal(E);
          E = E.Next;
        }
        if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Prev.Top.x) 
          ReverseHorizontal(E);
        Result = Result.Next; //move to the edge just beyond current bound
      }
      else
      {
        while (Result.Top.y == Result.Prev.Bot.y && Result.Prev.OutIdx != Skip)
          Result = Result.Prev;
        if (Result.Dx == horizontal && Result.Prev.OutIdx != Skip)
        {
          Horz = Result;
          while (Horz.Next.Dx == horizontal) Horz = Horz.Next;
          if (Horz.Next.Top.x == Result.Prev.Top.x || 
              Horz.Next.Top.x > Result.Prev.Top.x) Result = Horz.Next;
        }

        while (E != Result)
        {
          E.NextInLML = E.Prev;
          if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Next.Top.x) 
            ReverseHorizontal(E);
          E = E.Prev;
        }
        if (E.Dx == horizontal && E != EStart && E.Bot.x != E.Next.Top.x) 
          ReverseHorizontal(E);
        Result = Result.Prev; //move to the edge just beyond current bound
      }
      return Result;
    }
    //------------------------------------------------------------------------------


    public bool AddPath(List<Vector2> pg, PolyType polyType, bool Closed)
    {
#if use_lines
      if (!Closed && polyType == PolyType.ptClip)
        throw new ClipperException("AddPath: Open paths must be subject.");
#else
      if (!Closed)
        throw new ClipperException("AddPath: Open paths have been disabled.");
#endif

      int highI = (int)pg.Count - 1;
      if (Closed) while (highI > 0 && (pg[highI] == pg[0])) --highI;
      while (highI > 0 && (pg[highI] == pg[highI - 1])) --highI;
      if ((Closed && highI < 2) || (!Closed && highI < 1)) return false;

      //create a new edge array ...
      List<TEdge> edges = new List<TEdge>(highI+1);
      for (int i = 0; i <= highI; i++) edges.Add(new TEdge());
          
      bool IsFlat = true;

      //1. Basic (first) edge initialization ...
      edges[1].Curr = pg[1];
      RangeTest(pg[0], ref m_UseFullRange);
      RangeTest(pg[highI], ref m_UseFullRange);
      InitEdge(edges[0], edges[1], edges[highI], pg[0]);
      InitEdge(edges[highI], edges[0], edges[highI - 1], pg[highI]);
      for (int i = highI - 1; i >= 1; --i)
      {
        RangeTest(pg[i], ref m_UseFullRange);
        InitEdge(edges[i], edges[i + 1], edges[i - 1], pg[i]);
      }
      TEdge eStart = edges[0];

      //2. Remove duplicate vertices, and (when closed) collinear edges ...
      TEdge E = eStart, eLoopStop = eStart;
      for (;;)
      {
        //nb: allows matching start and end points when not Closed ...
        if (E.Curr == E.Next.Curr && (Closed || E.Next != eStart))
        {
          if (E == E.Next) break;
          if (E == eStart) eStart = E.Next;
          E = RemoveEdge(E);
          eLoopStop = E;
          continue;
        }
        if (E.Prev == E.Next) 
          break; //only two vertices
        else if (Closed &&
          SlopesEqual(E.Prev.Curr, E.Curr, E.Next.Curr, m_UseFullRange) && 
          (!PreserveCollinear ||
          !Pt2IsBetweenPt1AndPt3(E.Prev.Curr, E.Curr, E.Next.Curr))) 
        {
          //Collinear edges are allowed for open paths but in closed paths
          //the default is to merge adjacent collinear edges into a single edge.
          //However, if the PreserveCollinear property is enabled, only overlapping
          //collinear edges (ie spikes) will be removed from closed paths.
          if (E == eStart) eStart = E.Next;
          E = RemoveEdge(E);
          E = E.Prev;
          eLoopStop = E;
          continue;
        }
        E = E.Next;
        if ((E == eLoopStop) || (!Closed && E.Next == eStart)) break;
      }

      if ((!Closed && (E == E.Next)) || (Closed && (E.Prev == E.Next)))
        return false;

      if (!Closed)
      {
        m_HasOpenPaths = true;
        eStart.Prev.OutIdx = Skip;
      }

      //3. Do second stage of edge initialization ...
      E = eStart;
      do
      {
        InitEdge2(E, polyType);
        E = E.Next;
        if (IsFlat && E.Curr.y != eStart.Curr.y) IsFlat = false;
      }
      while (E != eStart);

      //4. Finally, add edge bounds to LocalMinima list ...

      //Totally flat paths must be handled differently when adding them
      //to LocalMinima list to avoid endless loops etc ...
      if (IsFlat) 
      {
        if (Closed) return false;
        E.Prev.OutIdx = Skip;
        LocalMinima locMin = new LocalMinima();
        locMin.Next = null;
        locMin.y = E.Bot.y;
        locMin.LeftBound = null;
        locMin.RightBound = E;
        locMin.RightBound.Side = EdgeSide.esRight;
        locMin.RightBound.WindDelta = 0;
        for ( ; ; )
        {
          if (E.Bot.x != E.Prev.Top.x) ReverseHorizontal(E);
          if (E.Next.OutIdx == Skip) break;
          E.NextInLML = E.Next;
          E = E.Next;
        }
        InsertLocalMinima(locMin);
        m_edges.Add(edges);
        return true;
      }

      m_edges.Add(edges);
      bool leftBoundIsForward;
      TEdge EMin = null;

      //workaround to avoid an endless loop in the while loop below when
      //open paths have matching start and end points ...
      if (E.Prev.Bot == E.Prev.Top) E = E.Next;

      for (;;)
      {
        E = FindNextLocMin(E);
        if (E == EMin) break;
        else if (EMin == null) EMin = E;

        //E and E.Prev now share a local minima (left aligned if horizontal).
        //Compare their slopes to find which starts which bound ...
        LocalMinima locMin = new LocalMinima();
        locMin.Next = null;
        locMin.y = E.Bot.y;
        if (E.Dx < E.Prev.Dx) 
        {
          locMin.LeftBound = E.Prev;
          locMin.RightBound = E;
          leftBoundIsForward = false; //Q.nextInLML = Q.prev
        } else
        {
          locMin.LeftBound = E;
          locMin.RightBound = E.Prev;
          leftBoundIsForward = true; //Q.nextInLML = Q.next
        }
        locMin.LeftBound.Side = EdgeSide.esLeft;
        locMin.RightBound.Side = EdgeSide.esRight;

        if (!Closed) locMin.LeftBound.WindDelta = 0;
        else if (locMin.LeftBound.Next == locMin.RightBound)
          locMin.LeftBound.WindDelta = -1;
        else locMin.LeftBound.WindDelta = 1;
        locMin.RightBound.WindDelta = -locMin.LeftBound.WindDelta;

        E = ProcessBound(locMin.LeftBound, leftBoundIsForward);
        if (E.OutIdx == Skip) E = ProcessBound(E, leftBoundIsForward);

        TEdge E2 = ProcessBound(locMin.RightBound, !leftBoundIsForward);
        if (E2.OutIdx == Skip) E2 = ProcessBound(E2, !leftBoundIsForward);

        if (locMin.LeftBound.OutIdx == Skip)
          locMin.LeftBound = null;
        else if (locMin.RightBound.OutIdx == Skip)
          locMin.RightBound = null;
        InsertLocalMinima(locMin);
        if (!leftBoundIsForward) E = E2;
      }
      return true;

    }
    //------------------------------------------------------------------------------

    public bool AddPaths(List<List<Vector2>> ppg, PolyType polyType, bool closed)
    {
      bool result = false;
      for (int i = 0; i < ppg.Count; ++i)
        if (AddPath(ppg[i], polyType, closed)) result = true;
      return result;
    }
    //------------------------------------------------------------------------------

    internal bool Pt2IsBetweenPt1AndPt3(Vector2 pt1, Vector2 pt2, Vector2 pt3)
    {
      if ((pt1 == pt3) || (pt1 == pt2) || (pt3 == pt2)) return false;
      else if (pt1.x != pt3.x) return (pt2.x > pt1.x) == (pt2.x < pt3.x);
      else return (pt2.y > pt1.y) == (pt2.y < pt3.y);
    }
    //------------------------------------------------------------------------------

    TEdge RemoveEdge(TEdge e)
    {
      //removes e from double_linked_list (but without removing from memory)
      e.Prev.Next = e.Next;
      e.Next.Prev = e.Prev;
      TEdge result = e.Next;
      e.Prev = null; //flag as removed (see ClipperBase.Clear)
      return result;
    }
    //------------------------------------------------------------------------------

    private void SetDx(TEdge e)
    {
      e.Delta.x = (e.Top.x - e.Bot.x);
      e.Delta.y = (e.Top.y - e.Bot.y);
      if (e.Delta.y == 0) e.Dx = horizontal;
      else e.Dx = (double)(e.Delta.x) / (e.Delta.y);
    }
    //---------------------------------------------------------------------------

    private void InsertLocalMinima(LocalMinima newLm)
    {
      if( m_MinimaList == null )
      {
        m_MinimaList = newLm;
      }
      else if( newLm.y >= m_MinimaList.y )
      {
        newLm.Next = m_MinimaList;
        m_MinimaList = newLm;
      } else
      {
        LocalMinima tmpLm = m_MinimaList;
        while( tmpLm.Next != null  && ( newLm.y < tmpLm.Next.y ) )
          tmpLm = tmpLm.Next;
        newLm.Next = tmpLm.Next;
        tmpLm.Next = newLm;
      }
    }
    //------------------------------------------------------------------------------

    internal Boolean PopLocalMinima(float Y, out LocalMinima current)
    {
        current = m_CurrentLM;
        if (m_CurrentLM != null && m_CurrentLM.y == Y)
        {
            m_CurrentLM = m_CurrentLM.Next;
            return true;
        }
        return false;
    }
    //------------------------------------------------------------------------------

    private void ReverseHorizontal(TEdge e)
    {
      //swap horizontal edges' top and bottom x's so they follow the natural
      //progression of the bounds - ie so their xbots will align with the
      //adjoining lower edge. [Helpful in the ProcessHorizontal() method.]
      Swap(ref e.Top.x, ref e.Bot.x);
    }
    //------------------------------------------------------------------------------

    internal virtual void Reset()
    {
      m_CurrentLM = m_MinimaList;
      if (m_CurrentLM == null) return; //ie nothing to process

      //reset all edges ...
      m_Scanbeam = null;
      LocalMinima lm = m_MinimaList;
      while (lm != null)
      {
        InsertScanbeam(lm.y);
        TEdge e = lm.LeftBound;
        if (e != null)
        {
          e.Curr = e.Bot;
          e.OutIdx = Unassigned;
        }
        e = lm.RightBound;
        if (e != null)
        {
          e.Curr = e.Bot;
          e.OutIdx = Unassigned;
        }
        lm = lm.Next;
      }
      m_ActiveEdges = null;
    }
    //------------------------------------------------------------------------------

    public static Rect GetBounds(List<List<Vector2>> paths)
    {
      int i = 0, cnt = paths.Count;
      while (i < cnt && paths[i].Count == 0) i++;
      if (i == cnt) return new Rect(0,0,0,0);
      Rect result = new Rect();
      result.xMin = paths[i][0].x;
      result.xMax = result.xMin;
      result.yMin = paths[i][0].y;
      result.yMax = result.yMin;
      for (; i < cnt; i++)
        for (int j = 0; j < paths[i].Count; j++)
        {
          if (paths[i][j].x < result.xMin) result.xMin = paths[i][j].x;
          else if (paths[i][j].x > result.xMax) result.xMax = paths[i][j].x;
          if (paths[i][j].y < result.yMin) result.yMin = paths[i][j].y;
          else if (paths[i][j].y > result.yMax) result.yMax = paths[i][j].y;
        }
      return result;
    }
    //------------------------------------------------------------------------------

    internal void InsertScanbeam(float Y)
    {
        //single-linked list: sorted descending, ignoring dups.
        if (m_Scanbeam == null)
        {
            m_Scanbeam = new Scanbeam();
            m_Scanbeam.Next = null;
            m_Scanbeam.y = Y;
        }
        else if (Y > m_Scanbeam.y)
        {
            Scanbeam newSb = new Scanbeam();
            newSb.y = Y;
            newSb.Next = m_Scanbeam;
            m_Scanbeam = newSb;
        }
        else
        {
            Scanbeam sb2 = m_Scanbeam;
            while (sb2.Next != null && (Y <= sb2.Next.y)) sb2 = sb2.Next;
            if (Y == sb2.y) return; //ie ignores duplicates
            Scanbeam newSb = new Scanbeam();
            newSb.y = Y;
            newSb.Next = sb2.Next;
            sb2.Next = newSb;
        }
    }
    //------------------------------------------------------------------------------

    internal Boolean PopScanbeam(out float Y)
    {
        if (m_Scanbeam == null)
        {
            Y = 0;
            return false;
        }
        Y = m_Scanbeam.y;
        m_Scanbeam = m_Scanbeam.Next;
        return true;
    }
    //------------------------------------------------------------------------------

    internal Boolean LocalMinimaPending()
    {
        return (m_CurrentLM != null);
    }
    //------------------------------------------------------------------------------

    internal OutRec CreateOutRec()
    {
        OutRec result = new OutRec();
        result.Idx = Unassigned;
        result.IsHole = false;
        result.IsOpen = false;
        result.FirstLeft = null;
        result.Pts = null;
        result.BottomPt = null;
        result.PolyNode = null;
        m_PolyOuts.Add(result);
        result.Idx = m_PolyOuts.Count - 1;
        return result;
    }
    //------------------------------------------------------------------------------

    internal void DisposeOutRec(int index)
    {
        OutRec outRec = m_PolyOuts[index];
        outRec.Pts = null;
        outRec = null;
        m_PolyOuts[index] = null;
    }
    //------------------------------------------------------------------------------

    internal void UpdateEdgeIntoAEL(ref TEdge e)
    {
        if (e.NextInLML == null)
            throw new ClipperException("UpdateEdgeIntoAEL: invalid call");
        TEdge AelPrev = e.PrevInAEL;
        TEdge AelNext = e.NextInAEL;
        e.NextInLML.OutIdx = e.OutIdx;
        if (AelPrev != null)
            AelPrev.NextInAEL = e.NextInLML;
        else m_ActiveEdges = e.NextInLML;
        if (AelNext != null)
            AelNext.PrevInAEL = e.NextInLML;
        e.NextInLML.Side = e.Side;
        e.NextInLML.WindDelta = e.WindDelta;
        e.NextInLML.WindCnt = e.WindCnt;
        e.NextInLML.WindCnt2 = e.WindCnt2;
        e = e.NextInLML;
        e.Curr = e.Bot;
        e.PrevInAEL = AelPrev;
        e.NextInAEL = AelNext;
        if (!IsHorizontal(e)) InsertScanbeam(e.Top.y);
    }
    //------------------------------------------------------------------------------

    internal void SwapPositionsInAEL(TEdge edge1, TEdge edge2)
    {
        //check that one or other edge hasn't already been removed from AEL ...
        if (edge1.NextInAEL == edge1.PrevInAEL ||
          edge2.NextInAEL == edge2.PrevInAEL) return;

        if (edge1.NextInAEL == edge2)
        {
            TEdge next = edge2.NextInAEL;
            if (next != null)
                next.PrevInAEL = edge1;
            TEdge prev = edge1.PrevInAEL;
            if (prev != null)
                prev.NextInAEL = edge2;
            edge2.PrevInAEL = prev;
            edge2.NextInAEL = edge1;
            edge1.PrevInAEL = edge2;
            edge1.NextInAEL = next;
        }
        else if (edge2.NextInAEL == edge1)
        {
            TEdge next = edge1.NextInAEL;
            if (next != null)
                next.PrevInAEL = edge2;
            TEdge prev = edge2.PrevInAEL;
            if (prev != null)
                prev.NextInAEL = edge1;
            edge1.PrevInAEL = prev;
            edge1.NextInAEL = edge2;
            edge2.PrevInAEL = edge1;
            edge2.NextInAEL = next;
        }
        else
        {
            TEdge next = edge1.NextInAEL;
            TEdge prev = edge1.PrevInAEL;
            edge1.NextInAEL = edge2.NextInAEL;
            if (edge1.NextInAEL != null)
                edge1.NextInAEL.PrevInAEL = edge1;
            edge1.PrevInAEL = edge2.PrevInAEL;
            if (edge1.PrevInAEL != null)
                edge1.PrevInAEL.NextInAEL = edge1;
            edge2.NextInAEL = next;
            if (edge2.NextInAEL != null)
                edge2.NextInAEL.PrevInAEL = edge2;
            edge2.PrevInAEL = prev;
            if (edge2.PrevInAEL != null)
                edge2.PrevInAEL.NextInAEL = edge2;
        }

        if (edge1.PrevInAEL == null)
            m_ActiveEdges = edge1;
        else if (edge2.PrevInAEL == null)
            m_ActiveEdges = edge2;
    }
    //------------------------------------------------------------------------------

    internal void DeleteFromAEL(TEdge e)
    {
        TEdge AelPrev = e.PrevInAEL;
        TEdge AelNext = e.NextInAEL;
        if (AelPrev == null && AelNext == null && (e != m_ActiveEdges))
            return; //already deleted
        if (AelPrev != null)
            AelPrev.NextInAEL = AelNext;
        else m_ActiveEdges = AelNext;
        if (AelNext != null)
            AelNext.PrevInAEL = AelPrev;
        e.NextInAEL = null;
        e.PrevInAEL = null;
    }
    //------------------------------------------------------------------------------

  } //end ClipperBase

  public class Clipper : ClipperBase
  {
      //InitOptions that can be passed to the constructor ...
      public const int ioReverseSolution = 1;
      public const int ioStrictlySimple = 2;
      public const int ioPreserveCollinear = 4;

      private ClipType m_ClipType;
      private Maxima m_Maxima;
      private TEdge m_SortedEdges;
      private List<IntersectNode> m_IntersectList;
      IComparer<IntersectNode> m_IntersectNodeComparer;
      private bool m_ExecuteLocked;
      private PolyFillType m_ClipFillType;
      private PolyFillType m_SubjFillType;
      private List<Join> m_Joins;
      private List<Join> m_GhostJoins;
      private bool m_UsingPolyTree;

      public Clipper(int InitOptions = 0): base() //constructor
      {
          m_Scanbeam = null;
          m_Maxima = null;
          m_ActiveEdges = null;
          m_SortedEdges = null;
          m_IntersectList = new List<IntersectNode>();
          m_IntersectNodeComparer = new MyIntersectNodeSort();
          m_ExecuteLocked = false;
          m_UsingPolyTree = false;
          m_PolyOuts = new List<OutRec>();
          m_Joins = new List<Join>();
          m_GhostJoins = new List<Join>();
          ReverseSolution = (ioReverseSolution & InitOptions) != 0;
          StrictlySimple = (ioStrictlySimple & InitOptions) != 0;
          PreserveCollinear = (ioPreserveCollinear & InitOptions) != 0;
      }
      //------------------------------------------------------------------------------

      private void InsertMaxima(float X)
      {
          //double-linked list: sorted ascending, ignoring dups.
          Maxima newMax = new Maxima();
          newMax.x = X;
          if (m_Maxima == null)
          {
              m_Maxima = newMax;
              m_Maxima.Next = null;
              m_Maxima.Prev = null;
          }
          else if (X < m_Maxima.x)
          {
              newMax.Next = m_Maxima;
              newMax.Prev = null;
              m_Maxima = newMax;
          }
          else
          {
              Maxima m = m_Maxima;
              while (m.Next != null && (X >= m.Next.x)) m = m.Next;
              if (X == m.x) return; //ie ignores duplicates (& CG to clean up newMax)
              //insert newMax between m and m.Next ...
              newMax.Next = m.Next;
              newMax.Prev = m;
              if (m.Next != null) m.Next.Prev = newMax;
              m.Next = newMax;
          }
      }
      //------------------------------------------------------------------------------

      public bool ReverseSolution
      {
        get;
        set;
      }
      //------------------------------------------------------------------------------

      public bool StrictlySimple
      {
        get; 
        set;
      }
      //------------------------------------------------------------------------------
       
      public bool Execute(ClipType clipType, List<List<Vector2>> solution, 
          PolyFillType FillType = PolyFillType.pftEvenOdd)
      {
          return Execute(clipType, solution, FillType, FillType);
      }
      //------------------------------------------------------------------------------

      public bool Execute(ClipType clipType, PolyTree polytree,
          PolyFillType FillType = PolyFillType.pftEvenOdd)
      {
          return Execute(clipType, polytree, FillType, FillType);
      }
      //------------------------------------------------------------------------------

      public bool Execute(ClipType clipType, List<List<Vector2>> solution,
          PolyFillType subjFillType, PolyFillType clipFillType)
      {
          if (m_ExecuteLocked) return false;
          if (m_HasOpenPaths) throw 
            new ClipperException("Error: PolyTree struct is needed for open path clipping.");

          m_ExecuteLocked = true;
          solution.Clear();
          m_SubjFillType = subjFillType;
          m_ClipFillType = clipFillType;
          m_ClipType = clipType;
          m_UsingPolyTree = false;
          bool succeeded;
          try
          {
            succeeded = ExecuteInternal();
            //build the return polygons ...
            if (succeeded) BuildResult(solution);
          }
          finally
          {
            DisposeAllPolyPts();
            m_ExecuteLocked = false;
          }
          return succeeded;
      }
      //------------------------------------------------------------------------------

      public bool Execute(ClipType clipType, PolyTree polytree,
          PolyFillType subjFillType, PolyFillType clipFillType)
      {
          if (m_ExecuteLocked) return false;
          m_ExecuteLocked = true;
          m_SubjFillType = subjFillType;
          m_ClipFillType = clipFillType;
          m_ClipType = clipType;
          m_UsingPolyTree = true;
          bool succeeded;
          try
          {
            succeeded = ExecuteInternal();
            //build the return polygons ...
            if (succeeded) BuildResult2(polytree);
          }
          finally
          {
            DisposeAllPolyPts();
            m_ExecuteLocked = false;
          }
          return succeeded;
      }
      //------------------------------------------------------------------------------

      internal void FixHoleLinkage(OutRec outRec)
      {
        //skip if an outermost polygon or
        //already already points to the correct FirstLeft ...
        if (outRec.FirstLeft == null ||
              (outRec.IsHole != outRec.FirstLeft.IsHole &&
              outRec.FirstLeft.Pts != null)) return;

        OutRec orfl = outRec.FirstLeft;
        while (orfl != null && ((orfl.IsHole == outRec.IsHole) || orfl.Pts == null))
          orfl = orfl.FirstLeft;
        outRec.FirstLeft = orfl;
      }
      //------------------------------------------------------------------------------

      private bool ExecuteInternal()
      {
        try
        {
          Reset();
          m_SortedEdges = null;
          m_Maxima = null;

          float botY, topY;
          if (!PopScanbeam(out botY)) return false;
          InsertLocalMinimaIntoAEL(botY);
          while (PopScanbeam(out topY) || LocalMinimaPending())
          {
            ProcessHorizontals();
            m_GhostJoins.Clear();
            if (!ProcessIntersections(topY)) return false;
            ProcessEdgesAtTopOfScanbeam(topY);
            botY = topY;
            InsertLocalMinimaIntoAEL(botY);
          } 

          //fix orientations ...
          foreach (OutRec outRec in m_PolyOuts)
          {
            if (outRec.Pts == null || outRec.IsOpen) continue;
            if ((outRec.IsHole ^ ReverseSolution) == (Area(outRec) > 0))
              ReversePolyPtLinks(outRec.Pts);
          }

          JoinCommonEdges();

          foreach (OutRec outRec in m_PolyOuts)
          {
            if (outRec.Pts == null) 
                continue;
            else if (outRec.IsOpen)
                FixupOutPolyline(outRec);
            else
                FixupOutPolygon(outRec);
          }

          if (StrictlySimple) DoSimplePolygons();
          return true;
        }
        //catch { return false; }
        finally 
        {
          m_Joins.Clear();
          m_GhostJoins.Clear();          
        }
      }
      //------------------------------------------------------------------------------

      private void DisposeAllPolyPts(){
        for (int i = 0; i < m_PolyOuts.Count; ++i) DisposeOutRec(i);
        m_PolyOuts.Clear();
      }
      //------------------------------------------------------------------------------

      private void AddJoin(OutPt Op1, OutPt Op2, Vector2 OffPt)
      {
        Join j = new Join();
        j.OutPt1 = Op1;
        j.OutPt2 = Op2;
        j.OffPt = OffPt;
        m_Joins.Add(j);
      }
      //------------------------------------------------------------------------------

      private void AddGhostJoin(OutPt Op, Vector2 OffPt)
      {
        Join j = new Join();
        j.OutPt1 = Op;
        j.OffPt = OffPt;
        m_GhostJoins.Add(j);
      }
      //------------------------------------------------------------------------------

      private void InsertLocalMinimaIntoAEL(float botY)
      {
        LocalMinima lm;
        while (PopLocalMinima(botY, out lm))
        {
          TEdge lb = lm.LeftBound;
          TEdge rb = lm.RightBound;

          OutPt Op1 = null;
          if (lb == null)
          {
            InsertEdgeIntoAEL(rb, null);
            SetWindingCount(rb);
            if (IsContributing(rb))
              Op1 = AddOutPt(rb, rb.Bot);
          }
          else if (rb == null)
          {
            InsertEdgeIntoAEL(lb, null);
            SetWindingCount(lb);
            if (IsContributing(lb))
              Op1 = AddOutPt(lb, lb.Bot);
            InsertScanbeam(lb.Top.y);
          }
          else
          {
            InsertEdgeIntoAEL(lb, null);
            InsertEdgeIntoAEL(rb, lb);
            SetWindingCount(lb);
            rb.WindCnt = lb.WindCnt;
            rb.WindCnt2 = lb.WindCnt2;
            if (IsContributing(lb))
              Op1 = AddLocalMinPoly(lb, rb, lb.Bot);
            InsertScanbeam(lb.Top.y);
          }

          if (rb != null)
          {
            if (IsHorizontal(rb))
            {
              if (rb.NextInLML != null)
                InsertScanbeam(rb.NextInLML.Top.y);
              AddEdgeToSEL(rb);
            }
            else
              InsertScanbeam(rb.Top.y);
          }

        if (lb == null || rb == null) continue;

          //if output polygons share an Edge with a horizontal rb, they'll need joining later ...
          if (Op1 != null && IsHorizontal(rb) && 
            m_GhostJoins.Count > 0 && rb.WindDelta != 0)
          {
            for (int i = 0; i < m_GhostJoins.Count; i++)
            {
              //if the horizontal Rb and a 'ghost' horizontal overlap, then convert
              //the 'ghost' join to a real join ready for later ...
              Join j = m_GhostJoins[i];
              if (HorzSegmentsOverlap(j.OutPt1.Pt.x, j.OffPt.x, rb.Bot.x, rb.Top.x))
                AddJoin(j.OutPt1, Op1, j.OffPt);
            }
          }

          if (lb.OutIdx >= 0 && lb.PrevInAEL != null &&
            lb.PrevInAEL.Curr.x == lb.Bot.x &&
            lb.PrevInAEL.OutIdx >= 0 &&
            SlopesEqual(lb.PrevInAEL.Curr, lb.PrevInAEL.Top, lb.Curr, lb.Top, m_UseFullRange) &&
            lb.WindDelta != 0 && lb.PrevInAEL.WindDelta != 0)
          {
            OutPt Op2 = AddOutPt(lb.PrevInAEL, lb.Bot);
            AddJoin(Op1, Op2, lb.Top);
          }

          if( lb.NextInAEL != rb )
          {

            if (rb.OutIdx >= 0 && rb.PrevInAEL.OutIdx >= 0 &&
              SlopesEqual(rb.PrevInAEL.Curr, rb.PrevInAEL.Top, rb.Curr, rb.Top, m_UseFullRange) &&
              rb.WindDelta != 0 && rb.PrevInAEL.WindDelta != 0)
            {
              OutPt Op2 = AddOutPt(rb.PrevInAEL, rb.Bot);
              AddJoin(Op1, Op2, rb.Top);
            }

            TEdge e = lb.NextInAEL;
            if (e != null)
              while (e != rb)
              {
                //nb: For calculating winding counts etc, IntersectEdges() assumes
                //that param1 will be to the right of param2 ABOVE the intersection ...
                IntersectEdges(rb, e, lb.Curr); //order important here
                e = e.NextInAEL;
              }
          }
        }
      }
      //------------------------------------------------------------------------------

      private void InsertEdgeIntoAEL(TEdge edge, TEdge startEdge)
      {
        if (m_ActiveEdges == null)
        {
          edge.PrevInAEL = null;
          edge.NextInAEL = null;
          m_ActiveEdges = edge;
        }
        else if (startEdge == null && E2InsertsBeforeE1(m_ActiveEdges, edge))
        {
          edge.PrevInAEL = null;
          edge.NextInAEL = m_ActiveEdges;
          m_ActiveEdges.PrevInAEL = edge;
          m_ActiveEdges = edge;
        }
        else
        {
          if (startEdge == null) startEdge = m_ActiveEdges;
          while (startEdge.NextInAEL != null &&
            !E2InsertsBeforeE1(startEdge.NextInAEL, edge))
            startEdge = startEdge.NextInAEL;
          edge.NextInAEL = startEdge.NextInAEL;
          if (startEdge.NextInAEL != null) startEdge.NextInAEL.PrevInAEL = edge;
          edge.PrevInAEL = startEdge;
          startEdge.NextInAEL = edge;
        }
      }
      //----------------------------------------------------------------------

      private bool E2InsertsBeforeE1(TEdge e1, TEdge e2)
      {
          if (e2.Curr.x == e1.Curr.x)
          {
              if (e2.Top.y > e1.Top.y)
                  return e2.Top.x < TopX(e1, e2.Top.y);
              else return e1.Top.x > TopX(e2, e1.Top.y);
          }
          else return e2.Curr.x < e1.Curr.x;
      }
      //------------------------------------------------------------------------------

      private bool IsEvenOddFillType(TEdge edge) 
      {
        if (edge.PolyTyp == PolyType.ptSubject)
            return m_SubjFillType == PolyFillType.pftEvenOdd; 
        else
            return m_ClipFillType == PolyFillType.pftEvenOdd;
      }
      //------------------------------------------------------------------------------

      private bool IsEvenOddAltFillType(TEdge edge) 
      {
        if (edge.PolyTyp == PolyType.ptSubject)
            return m_ClipFillType == PolyFillType.pftEvenOdd; 
        else
            return m_SubjFillType == PolyFillType.pftEvenOdd;
      }
      //------------------------------------------------------------------------------

      private bool IsContributing(TEdge edge)
      {
          PolyFillType pft, pft2;
          if (edge.PolyTyp == PolyType.ptSubject)
          {
              pft = m_SubjFillType;
              pft2 = m_ClipFillType;
          }
          else
          {
              pft = m_ClipFillType;
              pft2 = m_SubjFillType;
          }

          switch (pft)
          {
              case PolyFillType.pftEvenOdd:
                  //return false if a subj line has been flagged as inside a subj polygon
                  if (edge.WindDelta == 0 && edge.WindCnt != 1) return false;
                  break;
              case PolyFillType.pftNonZero:
                  if (Math.Abs(edge.WindCnt) != 1) return false;
                  break;
              case PolyFillType.pftPositive:
                  if (edge.WindCnt != 1) return false;
                  break;
              default: //PolyFillType.pftNegative
                  if (edge.WindCnt != -1) return false; 
                  break;
          }

          switch (m_ClipType)
          {
            case ClipType.ctIntersection:
                switch (pft2)
                {
                    case PolyFillType.pftEvenOdd:
                    case PolyFillType.pftNonZero:
                        return (edge.WindCnt2 != 0);
                    case PolyFillType.pftPositive:
                        return (edge.WindCnt2 > 0);
                    default:
                        return (edge.WindCnt2 < 0);
                }
            case ClipType.ctUnion:
                switch (pft2)
                {
                    case PolyFillType.pftEvenOdd:
                    case PolyFillType.pftNonZero:
                        return (edge.WindCnt2 == 0);
                    case PolyFillType.pftPositive:
                        return (edge.WindCnt2 <= 0);
                    default:
                        return (edge.WindCnt2 >= 0);
                }
            case ClipType.ctDifference:
                if (edge.PolyTyp == PolyType.ptSubject)
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return (edge.WindCnt2 == 0);
                        case PolyFillType.pftPositive:
                            return (edge.WindCnt2 <= 0);
                        default:
                            return (edge.WindCnt2 >= 0);
                    }
                else
                    switch (pft2)
                    {
                        case PolyFillType.pftEvenOdd:
                        case PolyFillType.pftNonZero:
                            return (edge.WindCnt2 != 0);
                        case PolyFillType.pftPositive:
                            return (edge.WindCnt2 > 0);
                        default:
                            return (edge.WindCnt2 < 0);
                    }
            case ClipType.ctXor:
                if (edge.WindDelta == 0) //XOr always contributing unless open
                  switch (pft2)
                  {
                    case PolyFillType.pftEvenOdd:
                    case PolyFillType.pftNonZero:
                      return (edge.WindCnt2 == 0);
                    case PolyFillType.pftPositive:
                      return (edge.WindCnt2 <= 0);
                    default:
                      return (edge.WindCnt2 >= 0);
                  }
                else
                  return true;
          }
          return true;
      }
      //------------------------------------------------------------------------------

      private void SetWindingCount(TEdge edge)
      {
        TEdge e = edge.PrevInAEL;
        //find the edge of the same polytype that immediately preceeds 'edge' in AEL
        while (e != null && ((e.PolyTyp != edge.PolyTyp) || (e.WindDelta == 0))) e = e.PrevInAEL;
        if (e == null)
        {
          PolyFillType pft;
          pft = (edge.PolyTyp == PolyType.ptSubject ? m_SubjFillType : m_ClipFillType);
          if (edge.WindDelta == 0) edge.WindCnt = (pft == PolyFillType.pftNegative ? -1 : 1);
          else edge.WindCnt = edge.WindDelta;
          edge.WindCnt2 = 0;
          e = m_ActiveEdges; //ie get ready to calc WindCnt2
        }
        else if (edge.WindDelta == 0 && m_ClipType != ClipType.ctUnion)
        {
          edge.WindCnt = 1;
          edge.WindCnt2 = e.WindCnt2;
          e = e.NextInAEL; //ie get ready to calc WindCnt2
        }
        else if (IsEvenOddFillType(edge))
        {
          //EvenOdd filling ...
          if (edge.WindDelta == 0)
          {
            //are we inside a subj polygon ...
            bool Inside = true;
            TEdge e2 = e.PrevInAEL;
            while (e2 != null)
            {
              if (e2.PolyTyp == e.PolyTyp && e2.WindDelta != 0)
                Inside = !Inside;
              e2 = e2.PrevInAEL;
            }
            edge.WindCnt = (Inside ? 0 : 1);
          }
          else
          {
            edge.WindCnt = edge.WindDelta;
          }
          edge.WindCnt2 = e.WindCnt2;
          e = e.NextInAEL; //ie get ready to calc WindCnt2
        }
        else
        {
          //nonZero, Positive or Negative filling ...
          if (e.WindCnt * e.WindDelta < 0)
          {
            //prev edge is 'decreasing' WindCount (WC) toward zero
            //so we're outside the previous polygon ...
            if (Math.Abs(e.WindCnt) > 1)
            {
              //outside prev poly but still inside another.
              //when reversing direction of prev poly use the same WC 
              if (e.WindDelta * edge.WindDelta < 0) edge.WindCnt = e.WindCnt;
              //otherwise continue to 'decrease' WC ...
              else edge.WindCnt = e.WindCnt + edge.WindDelta;
            }
            else
              //now outside all polys of same polytype so set own WC ...
              edge.WindCnt = (edge.WindDelta == 0 ? 1 : edge.WindDelta);
          }
          else
          {
            //prev edge is 'increasing' WindCount (WC) away from zero
            //so we're inside the previous polygon ...
            if (edge.WindDelta == 0)
              edge.WindCnt = (e.WindCnt < 0 ? e.WindCnt - 1 : e.WindCnt + 1);
            //if wind direction is reversing prev then use same WC
            else if (e.WindDelta * edge.WindDelta < 0)
              edge.WindCnt = e.WindCnt;
            //otherwise add to WC ...
            else edge.WindCnt = e.WindCnt + edge.WindDelta;
          }
          edge.WindCnt2 = e.WindCnt2;
          e = e.NextInAEL; //ie get ready to calc WindCnt2
        }

        //update WindCnt2 ...
        if (IsEvenOddAltFillType(edge))
        {
          //EvenOdd filling ...
          while (e != edge)
          {
            if (e.WindDelta != 0)
              edge.WindCnt2 = (edge.WindCnt2 == 0 ? 1 : 0);
            e = e.NextInAEL;
          }
        }
        else
        {
          //nonZero, Positive or Negative filling ...
          while (e != edge)
          {
            edge.WindCnt2 += e.WindDelta;
            e = e.NextInAEL;
          }
        }
      }
      //------------------------------------------------------------------------------

      private void AddEdgeToSEL(TEdge edge)
      {
        //SEL pointers in PEdge are use to build transient lists of horizontal edges.
        //However, since we don't need to worry about processing order, all additions
        //are made to the front of the list ...
        if (m_SortedEdges == null)
        {
            m_SortedEdges = edge;
            edge.PrevInSEL = null;
            edge.NextInSEL = null;
        }
        else
        {
            edge.NextInSEL = m_SortedEdges;
            edge.PrevInSEL = null;
            m_SortedEdges.PrevInSEL = edge;
            m_SortedEdges = edge;
        }
      }
      //------------------------------------------------------------------------------

      internal Boolean PopEdgeFromSEL(out TEdge e)
      {
        //Pop edge from front of SEL (ie SEL is a FILO list)
        e = m_SortedEdges;
        if (e == null) return false;
        TEdge oldE = e;
        m_SortedEdges = e.NextInSEL;
        if (m_SortedEdges != null) m_SortedEdges.PrevInSEL = null;
        oldE.NextInSEL = null;
        oldE.PrevInSEL = null;
        return true;
      }
      //------------------------------------------------------------------------------
     
      private void CopyAELToSEL()
      {
          TEdge e = m_ActiveEdges;
          m_SortedEdges = e;
          while (e != null)
          {
              e.PrevInSEL = e.PrevInAEL;
              e.NextInSEL = e.NextInAEL;
              e = e.NextInAEL;
          }
      }
      //------------------------------------------------------------------------------

      private void SwapPositionsInSEL(TEdge edge1, TEdge edge2)
      {
          if (edge1.NextInSEL == null && edge1.PrevInSEL == null)
              return;
          if (edge2.NextInSEL == null && edge2.PrevInSEL == null)
              return;

          if (edge1.NextInSEL == edge2)
          {
              TEdge next = edge2.NextInSEL;
              if (next != null)
                  next.PrevInSEL = edge1;
              TEdge prev = edge1.PrevInSEL;
              if (prev != null)
                  prev.NextInSEL = edge2;
              edge2.PrevInSEL = prev;
              edge2.NextInSEL = edge1;
              edge1.PrevInSEL = edge2;
              edge1.NextInSEL = next;
          }
          else if (edge2.NextInSEL == edge1)
          {
              TEdge next = edge1.NextInSEL;
              if (next != null)
                  next.PrevInSEL = edge2;
              TEdge prev = edge2.PrevInSEL;
              if (prev != null)
                  prev.NextInSEL = edge1;
              edge1.PrevInSEL = prev;
              edge1.NextInSEL = edge2;
              edge2.PrevInSEL = edge1;
              edge2.NextInSEL = next;
          }
          else
          {
              TEdge next = edge1.NextInSEL;
              TEdge prev = edge1.PrevInSEL;
              edge1.NextInSEL = edge2.NextInSEL;
              if (edge1.NextInSEL != null)
                  edge1.NextInSEL.PrevInSEL = edge1;
              edge1.PrevInSEL = edge2.PrevInSEL;
              if (edge1.PrevInSEL != null)
                  edge1.PrevInSEL.NextInSEL = edge1;
              edge2.NextInSEL = next;
              if (edge2.NextInSEL != null)
                  edge2.NextInSEL.PrevInSEL = edge2;
              edge2.PrevInSEL = prev;
              if (edge2.PrevInSEL != null)
                  edge2.PrevInSEL.NextInSEL = edge2;
          }

          if (edge1.PrevInSEL == null)
              m_SortedEdges = edge1;
          else if (edge2.PrevInSEL == null)
              m_SortedEdges = edge2;
      }
      //------------------------------------------------------------------------------


      private void AddLocalMaxPoly(TEdge e1, TEdge e2, Vector2 pt)
      {
          AddOutPt(e1, pt);
          if (e2.WindDelta == 0) AddOutPt(e2, pt);
          if (e1.OutIdx == e2.OutIdx)
          {
              e1.OutIdx = Unassigned;
              e2.OutIdx = Unassigned;
          }
          else if (e1.OutIdx < e2.OutIdx) 
              AppendPolygon(e1, e2);
          else 
              AppendPolygon(e2, e1);
      }
      //------------------------------------------------------------------------------

      private OutPt AddLocalMinPoly(TEdge e1, TEdge e2, Vector2 pt)
      {
        OutPt result;
        TEdge e, prevE;
        if (IsHorizontal(e2) || (e1.Dx > e2.Dx))
        {
          result = AddOutPt(e1, pt);
          e2.OutIdx = e1.OutIdx;
          e1.Side = EdgeSide.esLeft;
          e2.Side = EdgeSide.esRight;
          e = e1;
          if (e.PrevInAEL == e2)
            prevE = e2.PrevInAEL; 
          else
            prevE = e.PrevInAEL;
        }
        else
        {
          result = AddOutPt(e2, pt);
          e1.OutIdx = e2.OutIdx;
          e1.Side = EdgeSide.esRight;
          e2.Side = EdgeSide.esLeft;
          e = e2;
          if (e.PrevInAEL == e1)
              prevE = e1.PrevInAEL;
          else
              prevE = e.PrevInAEL;
        }

        if (prevE != null && prevE.OutIdx >= 0 && prevE.Top.y < pt.y && e.Top.y < pt.y)
        {
          float xPrev = TopX(prevE, pt.y);
          float xE = TopX(e, pt.y);
          if ((xPrev == xE) && (e.WindDelta != 0) && (prevE.WindDelta != 0) &&
            SlopesEqual(new Vector2(xPrev, pt.y), prevE.Top, new Vector2(xE, pt.y), e.Top, m_UseFullRange))
          {
            OutPt outPt = AddOutPt(prevE, pt);
            AddJoin(result, outPt, e.Top);
          }
        }
        return result;
      }
      //------------------------------------------------------------------------------

      private OutPt AddOutPt(TEdge e, Vector2 pt)
      {
          if (e.OutIdx < 0)
          {
              OutRec outRec = CreateOutRec();
              outRec.IsOpen = (e.WindDelta == 0);
              OutPt newOp = new OutPt();
              outRec.Pts = newOp;
              newOp.Idx = outRec.Idx;
              newOp.Pt = pt;
              newOp.Next = newOp;
              newOp.Prev = newOp;
              if (!outRec.IsOpen)
                  SetHoleState(e, outRec);
              e.OutIdx = outRec.Idx; //nb: do this after SetZ !
              return newOp;
          }
          else
          {
              OutRec outRec = m_PolyOuts[e.OutIdx];
              //OutRec.Pts is the 'Left-most' point & OutRec.Pts.Prev is the 'Right-most'
              OutPt op = outRec.Pts;
              bool ToFront = (e.Side == EdgeSide.esLeft);
              if (ToFront && pt == op.Pt) return op;
              else if (!ToFront && pt == op.Prev.Pt) return op.Prev;

              OutPt newOp = new OutPt();
              newOp.Idx = outRec.Idx;
              newOp.Pt = pt;
              newOp.Next = op;
              newOp.Prev = op.Prev;
              newOp.Prev.Next = newOp;
              op.Prev = newOp;
              if (ToFront) outRec.Pts = newOp;
              return newOp;
          }
      }
      //------------------------------------------------------------------------------

      private OutPt GetLastOutPt(TEdge e)
      {
        OutRec outRec = m_PolyOuts[e.OutIdx];
        if (e.Side == EdgeSide.esLeft) 
            return outRec.Pts;
        else
            return outRec.Pts.Prev;
      }
      //------------------------------------------------------------------------------

      internal void SwapPoints(ref Vector2 pt1, ref Vector2 pt2)
      {
          Vector2 tmp = new Vector2(pt1.x, pt1.y);
          pt1 = pt2;
          pt2 = tmp;
      }
      //------------------------------------------------------------------------------

      private bool HorzSegmentsOverlap(float seg1a, float seg1b, float seg2a, float seg2b)
      {
        if (seg1a > seg1b) Swap(ref seg1a, ref seg1b);
        if (seg2a > seg2b) Swap(ref seg2a, ref seg2b);
        return (seg1a < seg2b) && (seg2a < seg1b);
      }
      //------------------------------------------------------------------------------
  
      private void SetHoleState(TEdge e, OutRec outRec)
      {
        TEdge e2 = e.PrevInAEL;
        TEdge eTmp = null;  
        while (e2 != null)
          {
            if (e2.OutIdx >= 0 && e2.WindDelta != 0) 
            {
              if (eTmp == null)
                eTmp = e2;
              else if (eTmp.OutIdx == e2.OutIdx)
                eTmp = null; //paired               
            }
            e2 = e2.PrevInAEL;
          }

        if (eTmp == null)
        {
          outRec.FirstLeft = null;
          outRec.IsHole = false;
        }
        else
        {
          outRec.FirstLeft = m_PolyOuts[eTmp.OutIdx];
          outRec.IsHole = !outRec.FirstLeft.IsHole;
        }
      }
      //------------------------------------------------------------------------------

      private double GetDx(Vector2 pt1, Vector2 pt2)
      {
          if (pt1.y == pt2.y) return horizontal;
          else return (double)(pt2.x - pt1.x) / (pt2.y - pt1.y);
      }
      //---------------------------------------------------------------------------

      private bool FirstIsBottomPt(OutPt btmPt1, OutPt btmPt2)
      {
        OutPt p = btmPt1.Prev;
        while ((p.Pt == btmPt1.Pt) && (p != btmPt1)) p = p.Prev;
        double dx1p = Math.Abs(GetDx(btmPt1.Pt, p.Pt));
        p = btmPt1.Next;
        while ((p.Pt == btmPt1.Pt) && (p != btmPt1)) p = p.Next;
        double dx1n = Math.Abs(GetDx(btmPt1.Pt, p.Pt));

        p = btmPt2.Prev;
        while ((p.Pt == btmPt2.Pt) && (p != btmPt2)) p = p.Prev;
        double dx2p = Math.Abs(GetDx(btmPt2.Pt, p.Pt));
        p = btmPt2.Next;
        while ((p.Pt == btmPt2.Pt) && (p != btmPt2)) p = p.Next;
        double dx2n = Math.Abs(GetDx(btmPt2.Pt, p.Pt));

        if (Math.Max(dx1p, dx1n) == Math.Max(dx2p, dx2n) &&
          Math.Min(dx1p, dx1n) == Math.Min(dx2p, dx2n))
          return Area(btmPt1) > 0; //if otherwise identical use orientation
        else
          return (dx1p >= dx2p && dx1p >= dx2n) || (dx1n >= dx2p && dx1n >= dx2n);
      }
      //------------------------------------------------------------------------------

      private OutPt GetBottomPt(OutPt pp)
      {
        OutPt dups = null;
        OutPt p = pp.Next;
        while (p != pp)
        {
          if (p.Pt.y > pp.Pt.y)
          {
            pp = p;
            dups = null;
          }
          else if (p.Pt.y == pp.Pt.y && p.Pt.x <= pp.Pt.x)
          {
            if (p.Pt.x < pp.Pt.x)
            {
                dups = null;
                pp = p;
            } else
            {
              if (p.Next != pp && p.Prev != pp) dups = p;
            }
          }
          p = p.Next;
        }
        if (dups != null)
        {
          //there appears to be at least 2 vertices at bottomPt so ...
          while (dups != p)
          {
            if (!FirstIsBottomPt(p, dups)) pp = dups;
            dups = dups.Next;
            while (dups.Pt != pp.Pt) dups = dups.Next;
          }
        }
        return pp;
      }
      //------------------------------------------------------------------------------

      private OutRec GetLowermostRec(OutRec outRec1, OutRec outRec2)
      {
          //work out which polygon fragment has the correct hole state ...
          if (outRec1.BottomPt == null) 
              outRec1.BottomPt = GetBottomPt(outRec1.Pts);
          if (outRec2.BottomPt == null) 
              outRec2.BottomPt = GetBottomPt(outRec2.Pts);
          OutPt bPt1 = outRec1.BottomPt;
          OutPt bPt2 = outRec2.BottomPt;
          if (bPt1.Pt.y > bPt2.Pt.y) return outRec1;
          else if (bPt1.Pt.y < bPt2.Pt.y) return outRec2;
          else if (bPt1.Pt.x < bPt2.Pt.x) return outRec1;
          else if (bPt1.Pt.x > bPt2.Pt.x) return outRec2;
          else if (bPt1.Next == bPt1) return outRec2;
          else if (bPt2.Next == bPt2) return outRec1;
          else if (FirstIsBottomPt(bPt1, bPt2)) return outRec1;
          else return outRec2;
      }
      //------------------------------------------------------------------------------

      bool OutRec1RightOfOutRec2(OutRec outRec1, OutRec outRec2)
      {
          do
          {
              outRec1 = outRec1.FirstLeft;
              if (outRec1 == outRec2) return true;
          } while (outRec1 != null);
          return false;
      }
      //------------------------------------------------------------------------------

      private OutRec GetOutRec(int idx)
      {
        OutRec outrec = m_PolyOuts[idx];
        while (outrec != m_PolyOuts[outrec.Idx])
          outrec = m_PolyOuts[outrec.Idx];
        return outrec;
      }
      //------------------------------------------------------------------------------

      private void AppendPolygon(TEdge e1, TEdge e2)
      {
        OutRec outRec1 = m_PolyOuts[e1.OutIdx];
        OutRec outRec2 = m_PolyOuts[e2.OutIdx];

        OutRec holeStateRec;
        if (OutRec1RightOfOutRec2(outRec1, outRec2)) 
            holeStateRec = outRec2;
        else if (OutRec1RightOfOutRec2(outRec2, outRec1))
            holeStateRec = outRec1;
        else
            holeStateRec = GetLowermostRec(outRec1, outRec2);

        //get the start and ends of both output polygons and
        //join E2 poly onto E1 poly and delete pointers to E2 ...
        OutPt p1_lft = outRec1.Pts;
        OutPt p1_rt = p1_lft.Prev;
        OutPt p2_lft = outRec2.Pts;
        OutPt p2_rt = p2_lft.Prev;

        //join e2 poly onto e1 poly and delete pointers to e2 ...
        if(  e1.Side == EdgeSide.esLeft )
        {
          if (e2.Side == EdgeSide.esLeft)
          {
            //z y x a b c
            ReversePolyPtLinks(p2_lft);
            p2_lft.Next = p1_lft;
            p1_lft.Prev = p2_lft;
            p1_rt.Next = p2_rt;
            p2_rt.Prev = p1_rt;
            outRec1.Pts = p2_rt;
          } else
          {
            //x y z a b c
            p2_rt.Next = p1_lft;
            p1_lft.Prev = p2_rt;
            p2_lft.Prev = p1_rt;
            p1_rt.Next = p2_lft;
            outRec1.Pts = p2_lft;
          }
        } else
        {
          if (e2.Side == EdgeSide.esRight)
          {
            //a b c z y x
            ReversePolyPtLinks( p2_lft );
            p1_rt.Next = p2_rt;
            p2_rt.Prev = p1_rt;
            p2_lft.Next = p1_lft;
            p1_lft.Prev = p2_lft;
          } else
          {
            //a b c x y z
            p1_rt.Next = p2_lft;
            p2_lft.Prev = p1_rt;
            p1_lft.Prev = p2_rt;
            p2_rt.Next = p1_lft;
          }
        }

        outRec1.BottomPt = null; 
        if (holeStateRec == outRec2)
        {
            if (outRec2.FirstLeft != outRec1)
                outRec1.FirstLeft = outRec2.FirstLeft;
            outRec1.IsHole = outRec2.IsHole;
        }
        outRec2.Pts = null;
        outRec2.BottomPt = null;

        outRec2.FirstLeft = outRec1;

        int OKIdx = e1.OutIdx;
        int ObsoleteIdx = e2.OutIdx;

        e1.OutIdx = Unassigned; //nb: safe because we only get here via AddLocalMaxPoly
        e2.OutIdx = Unassigned;

        TEdge e = m_ActiveEdges;
        while( e != null )
        {
          if( e.OutIdx == ObsoleteIdx )
          {
            e.OutIdx = OKIdx;
            e.Side = e1.Side;
            break;
          }
          e = e.NextInAEL;
        }
        outRec2.Idx = outRec1.Idx;
      }
      //------------------------------------------------------------------------------

      private void ReversePolyPtLinks(OutPt pp)
      {
          if (pp == null) return;
          OutPt pp1;
          OutPt pp2;
          pp1 = pp;
          do
          {
              pp2 = pp1.Next;
              pp1.Next = pp1.Prev;
              pp1.Prev = pp2;
              pp1 = pp2;
          } while (pp1 != pp);
      }
      //------------------------------------------------------------------------------

      private static void SwapSides(TEdge edge1, TEdge edge2)
      {
          EdgeSide side = edge1.Side;
          edge1.Side = edge2.Side;
          edge2.Side = side;
      }
      //------------------------------------------------------------------------------

      private static void SwapPolyIndexes(TEdge edge1, TEdge edge2)
      {
          int outIdx = edge1.OutIdx;
          edge1.OutIdx = edge2.OutIdx;
          edge2.OutIdx = outIdx;
      }
      //------------------------------------------------------------------------------

      private void IntersectEdges(TEdge e1, TEdge e2, Vector2 pt)
      {
          //e1 will be to the left of e2 BELOW the intersection. Therefore e1 is before
          //e2 in AEL except when e1 is being inserted at the intersection point ...

        bool e1Contributing = (e1.OutIdx >= 0);
        bool e2Contributing = (e2.OutIdx >= 0);

#if use_lines
          //if either edge is on an OPEN path ...
          if (e1.WindDelta == 0 || e2.WindDelta == 0)
          {
            //ignore subject-subject open path intersections UNLESS they
            //are both open paths, AND they are both 'contributing maximas' ...
            if (e1.WindDelta == 0 && e2.WindDelta == 0) return;
            //if intersecting a subj line with a subj poly ...
            else if (e1.PolyTyp == e2.PolyTyp && 
              e1.WindDelta != e2.WindDelta && m_ClipType == ClipType.ctUnion)
            {
              if (e1.WindDelta == 0)
              {
                if (e2Contributing)
                {
                  AddOutPt(e1, pt);
                  if (e1Contributing) e1.OutIdx = Unassigned;
                }
              }
              else
              {
                if (e1Contributing)
                {
                  AddOutPt(e2, pt);
                  if (e2Contributing) e2.OutIdx = Unassigned;
                }
              }
            }
            else if (e1.PolyTyp != e2.PolyTyp)
            {
              if ((e1.WindDelta == 0) && Math.Abs(e2.WindCnt) == 1 && 
                (m_ClipType != ClipType.ctUnion || e2.WindCnt2 == 0))
              {
                AddOutPt(e1, pt);
                if (e1Contributing) e1.OutIdx = Unassigned;
              }
              else if ((e2.WindDelta == 0) && (Math.Abs(e1.WindCnt) == 1) && 
                (m_ClipType != ClipType.ctUnion || e1.WindCnt2 == 0))
              {
                AddOutPt(e2, pt);
                if (e2Contributing) e2.OutIdx = Unassigned;
              }
            }
            return;
          }
#endif

          //update winding counts...
  //assumes that e1 will be to the Right of e2 ABOVE the intersection
          if (e1.PolyTyp == e2.PolyTyp)
          {
              if (IsEvenOddFillType(e1))
              {
                  int oldE1WindCnt = e1.WindCnt;
                  e1.WindCnt = e2.WindCnt;
                  e2.WindCnt = oldE1WindCnt;
              }
              else
              {
                  if (e1.WindCnt + e2.WindDelta == 0) e1.WindCnt = -e1.WindCnt;
                  else e1.WindCnt += e2.WindDelta;
                  if (e2.WindCnt - e1.WindDelta == 0) e2.WindCnt = -e2.WindCnt;
                  else e2.WindCnt -= e1.WindDelta;
              }
          }
          else
          {
              if (!IsEvenOddFillType(e2)) e1.WindCnt2 += e2.WindDelta;
              else e1.WindCnt2 = (e1.WindCnt2 == 0) ? 1 : 0;
              if (!IsEvenOddFillType(e1)) e2.WindCnt2 -= e1.WindDelta;
              else e2.WindCnt2 = (e2.WindCnt2 == 0) ? 1 : 0;
          }

          PolyFillType e1FillType, e2FillType, e1FillType2, e2FillType2;
          if (e1.PolyTyp == PolyType.ptSubject)
          {
              e1FillType = m_SubjFillType;
              e1FillType2 = m_ClipFillType;
          }
          else
          {
              e1FillType = m_ClipFillType;
              e1FillType2 = m_SubjFillType;
          }
          if (e2.PolyTyp == PolyType.ptSubject)
          {
              e2FillType = m_SubjFillType;
              e2FillType2 = m_ClipFillType;
          }
          else
          {
              e2FillType = m_ClipFillType;
              e2FillType2 = m_SubjFillType;
          }

          int e1Wc, e2Wc;
          switch (e1FillType)
          {
              case PolyFillType.pftPositive: e1Wc = e1.WindCnt; break;
              case PolyFillType.pftNegative: e1Wc = -e1.WindCnt; break;
              default: e1Wc = Math.Abs(e1.WindCnt); break;
          }
          switch (e2FillType)
          {
              case PolyFillType.pftPositive: e2Wc = e2.WindCnt; break;
              case PolyFillType.pftNegative: e2Wc = -e2.WindCnt; break;
              default: e2Wc = Math.Abs(e2.WindCnt); break;
          }

          if (e1Contributing && e2Contributing)
          {
            if ((e1Wc != 0 && e1Wc != 1) || (e2Wc != 0 && e2Wc != 1) ||
              (e1.PolyTyp != e2.PolyTyp && m_ClipType != ClipType.ctXor))
            {
              AddLocalMaxPoly(e1, e2, pt);
            }
            else
            {
              AddOutPt(e1, pt);
              AddOutPt(e2, pt);
              SwapSides(e1, e2);
              SwapPolyIndexes(e1, e2);
            }
          }
          else if (e1Contributing)
          {
              if (e2Wc == 0 || e2Wc == 1)
              {
                AddOutPt(e1, pt);
                SwapSides(e1, e2);
                SwapPolyIndexes(e1, e2);
              }

          }
          else if (e2Contributing)
          {
              if (e1Wc == 0 || e1Wc == 1)
              {
                AddOutPt(e2, pt);
                SwapSides(e1, e2);
                SwapPolyIndexes(e1, e2);
              }
          }
          else if ( (e1Wc == 0 || e1Wc == 1) && (e2Wc == 0 || e2Wc == 1))
          {
              //neither edge is currently contributing ...
              float e1Wc2, e2Wc2;
              switch (e1FillType2)
              {
                  case PolyFillType.pftPositive: e1Wc2 = e1.WindCnt2; break;
                  case PolyFillType.pftNegative: e1Wc2 = -e1.WindCnt2; break;
                  default: e1Wc2 = Math.Abs(e1.WindCnt2); break;
              }
              switch (e2FillType2)
              {
                  case PolyFillType.pftPositive: e2Wc2 = e2.WindCnt2; break;
                  case PolyFillType.pftNegative: e2Wc2 = -e2.WindCnt2; break;
                  default: e2Wc2 = Math.Abs(e2.WindCnt2); break;
              }

              if (e1.PolyTyp != e2.PolyTyp)
              {
                AddLocalMinPoly(e1, e2, pt);
              }
              else if (e1Wc == 1 && e2Wc == 1)
                switch (m_ClipType)
                {
                  case ClipType.ctIntersection:
                    if (e1Wc2 > 0 && e2Wc2 > 0)
                      AddLocalMinPoly(e1, e2, pt);
                    break;
                  case ClipType.ctUnion:
                    if (e1Wc2 <= 0 && e2Wc2 <= 0)
                      AddLocalMinPoly(e1, e2, pt);
                    break;
                  case ClipType.ctDifference:
                    if (((e1.PolyTyp == PolyType.ptClip) && (e1Wc2 > 0) && (e2Wc2 > 0)) ||
                        ((e1.PolyTyp == PolyType.ptSubject) && (e1Wc2 <= 0) && (e2Wc2 <= 0)))
                          AddLocalMinPoly(e1, e2, pt);
                    break;
                  case ClipType.ctXor:
                    AddLocalMinPoly(e1, e2, pt);
                    break;
                }
              else
                SwapSides(e1, e2);
          }
      }
      //------------------------------------------------------------------------------

      private void DeleteFromSEL(TEdge e)
      {
          TEdge SelPrev = e.PrevInSEL;
          TEdge SelNext = e.NextInSEL;
          if (SelPrev == null && SelNext == null && (e != m_SortedEdges))
              return; //already deleted
          if (SelPrev != null)
              SelPrev.NextInSEL = SelNext;
          else m_SortedEdges = SelNext;
          if (SelNext != null)
              SelNext.PrevInSEL = SelPrev;
          e.NextInSEL = null;
          e.PrevInSEL = null;
      }
      //------------------------------------------------------------------------------

      private void ProcessHorizontals()
      {
          TEdge horzEdge; //m_SortedEdges;
          while (PopEdgeFromSEL(out horzEdge))
            ProcessHorizontal(horzEdge);
      }
      //------------------------------------------------------------------------------

      void GetHorzDirection(TEdge HorzEdge, out Direction Dir, out float Left, out float Right)
      {
        if (HorzEdge.Bot.x < HorzEdge.Top.x)
        {
          Left = HorzEdge.Bot.x;
          Right = HorzEdge.Top.x;
          Dir = Direction.dLeftToRight;
        } else
        {
          Left = HorzEdge.Top.x;
          Right = HorzEdge.Bot.x;
          Dir = Direction.dRightToLeft;
        }
      }
      //------------------------------------------------------------------------

      private void ProcessHorizontal(TEdge horzEdge)
      {
        Direction dir;
        float horzLeft, horzRight;
        bool IsOpen = horzEdge.WindDelta == 0;

        GetHorzDirection(horzEdge, out dir, out horzLeft, out horzRight);

        TEdge eLastHorz = horzEdge, eMaxPair = null;
        while (eLastHorz.NextInLML != null && IsHorizontal(eLastHorz.NextInLML)) 
          eLastHorz = eLastHorz.NextInLML;
        if (eLastHorz.NextInLML == null)
          eMaxPair = GetMaximaPair(eLastHorz);

        Maxima currMax = m_Maxima;
        if (currMax != null)
        {
            //get the first maxima in range (X) ...
            if (dir == Direction.dLeftToRight)
            {
              while (currMax != null && currMax.x <= horzEdge.Bot.x)
                  currMax = currMax.Next;
              if (currMax != null && currMax.x >= eLastHorz.Top.x) 
                  currMax = null;
            }
            else
            {
              while (currMax.Next != null && currMax.Next.x < horzEdge.Bot.x) 
                  currMax = currMax.Next;
              if (currMax.x <= eLastHorz.Top.x) currMax = null;
            }
        }

        OutPt op1 = null;
        for (;;) //loop through consec. horizontal edges
        {
          bool IsLastHorz = (horzEdge == eLastHorz);
          TEdge e = GetNextInAEL(horzEdge, dir);
          while(e != null)
          {

              //this code block inserts extra coords into horizontal edges (in output
              //polygons) whereever maxima touch these horizontal edges. This helps
              //'simplifying' polygons (ie if the Simplify property is set).
              if (currMax != null)
              {
                  if (dir == Direction.dLeftToRight)
                  {
                      while (currMax != null && currMax.x < e.Curr.x) 
                      {
                        if (horzEdge.OutIdx >= 0 && !IsOpen) 
                          AddOutPt(horzEdge, new Vector2(currMax.x, horzEdge.Bot.y));
                        currMax = currMax.Next;                  
                      }
                  }
                  else
                  {
                      while (currMax != null && currMax.x > e.Curr.x)
                      {
                          if (horzEdge.OutIdx >= 0 && !IsOpen)
                            AddOutPt(horzEdge, new Vector2(currMax.x, horzEdge.Bot.y));
                        currMax = currMax.Prev;
                      }
                  }
              };

              if ((dir == Direction.dLeftToRight && e.Curr.x > horzRight) ||
                (dir == Direction.dRightToLeft && e.Curr.x < horzLeft)) break;
                                
              //Also break if we've got to the end of an intermediate horizontal edge ...
              //nb: Smaller Dx's are to the right of larger Dx's ABOVE the horizontal.
              if (e.Curr.x == horzEdge.Top.x && horzEdge.NextInLML != null && 
                e.Dx < horzEdge.NextInLML.Dx) break;

              if (horzEdge.OutIdx >= 0 && !IsOpen)  //note: may be done multiple times
              {
                  op1 = AddOutPt(horzEdge, e.Curr);
                  TEdge eNextHorz = m_SortedEdges;
                  while (eNextHorz != null)
                  {
                      if (eNextHorz.OutIdx >= 0 &&
                        HorzSegmentsOverlap(horzEdge.Bot.x,
                        horzEdge.Top.x, eNextHorz.Bot.x, eNextHorz.Top.x))
                      {
                          OutPt op2 = GetLastOutPt(eNextHorz);
                          AddJoin(op2, op1, eNextHorz.Top);
                      }
                      eNextHorz = eNextHorz.NextInSEL;
                  }
                  AddGhostJoin(op1, horzEdge.Bot);
              }
            
              //OK, so far we're still in range of the horizontal Edge  but make sure
              //we're at the last of consec. horizontals when matching with eMaxPair
              if(e == eMaxPair && IsLastHorz)
              {
                if (horzEdge.OutIdx >= 0)
                  AddLocalMaxPoly(horzEdge, eMaxPair, horzEdge.Top);
                DeleteFromAEL(horzEdge);
                DeleteFromAEL(eMaxPair);
                return;
              }
              
              if(dir == Direction.dLeftToRight)
              {
                Vector2 Pt = new Vector2(e.Curr.x, horzEdge.Curr.y);
                IntersectEdges(horzEdge, e, Pt);
              }
              else
              {
                Vector2 Pt = new Vector2(e.Curr.x, horzEdge.Curr.y);
                IntersectEdges(e, horzEdge, Pt);
              }
              TEdge eNext = GetNextInAEL(e, dir);
              SwapPositionsInAEL(horzEdge, e);
              e = eNext;
          } //end while(e != null)

          //Break out of loop if HorzEdge.NextInLML is not also horizontal ...
          if (horzEdge.NextInLML == null || !IsHorizontal(horzEdge.NextInLML)) break;

          UpdateEdgeIntoAEL(ref horzEdge);
          if (horzEdge.OutIdx >= 0) AddOutPt(horzEdge, horzEdge.Bot);
          GetHorzDirection(horzEdge, out dir, out horzLeft, out horzRight);

        } //end for (;;)

        if (horzEdge.OutIdx >= 0 && op1 == null)
        {
            op1 = GetLastOutPt(horzEdge);
            TEdge eNextHorz = m_SortedEdges;
            while (eNextHorz != null)
            {
                if (eNextHorz.OutIdx >= 0 &&
                  HorzSegmentsOverlap(horzEdge.Bot.x,
                  horzEdge.Top.x, eNextHorz.Bot.x, eNextHorz.Top.x))
                {
                    OutPt op2 = GetLastOutPt(eNextHorz);
                    AddJoin(op2, op1, eNextHorz.Top);
                }
                eNextHorz = eNextHorz.NextInSEL;
            }
            AddGhostJoin(op1, horzEdge.Top);
        }

        if (horzEdge.NextInLML != null)
        {
          if(horzEdge.OutIdx >= 0)
          {
            op1 = AddOutPt( horzEdge, horzEdge.Top);

            UpdateEdgeIntoAEL(ref horzEdge);
            if (horzEdge.WindDelta == 0) return;
            //nb: HorzEdge is no longer horizontal here
            TEdge ePrev = horzEdge.PrevInAEL;
            TEdge eNext = horzEdge.NextInAEL;
            if (ePrev != null && ePrev.Curr.x == horzEdge.Bot.x &&
              ePrev.Curr.y == horzEdge.Bot.y && ePrev.WindDelta != 0 &&
              (ePrev.OutIdx >= 0 && ePrev.Curr.y > ePrev.Top.y &&
              SlopesEqual(horzEdge, ePrev, m_UseFullRange)))
            {
              OutPt op2 = AddOutPt(ePrev, horzEdge.Bot);
              AddJoin(op1, op2, horzEdge.Top);
            }
            else if (eNext != null && eNext.Curr.x == horzEdge.Bot.x &&
              eNext.Curr.y == horzEdge.Bot.y && eNext.WindDelta != 0 &&
              eNext.OutIdx >= 0 && eNext.Curr.y > eNext.Top.y &&
              SlopesEqual(horzEdge, eNext, m_UseFullRange))
            {
              OutPt op2 = AddOutPt(eNext, horzEdge.Bot);
              AddJoin(op1, op2, horzEdge.Top);
            }
          }
          else
            UpdateEdgeIntoAEL(ref horzEdge); 
        }
        else
        {
          if (horzEdge.OutIdx >= 0) AddOutPt(horzEdge, horzEdge.Top);
          DeleteFromAEL(horzEdge);
        }
      }
      //------------------------------------------------------------------------------

      private TEdge GetNextInAEL(TEdge e, Direction Direction)
      {
          return Direction == Direction.dLeftToRight ? e.NextInAEL: e.PrevInAEL;
      }
      //------------------------------------------------------------------------------

      private bool IsMinima(TEdge e)
      {
          return e != null && (e.Prev.NextInLML != e) && (e.Next.NextInLML != e);
      }
      //------------------------------------------------------------------------------

      private bool IsMaxima(TEdge e, double Y)
      {
          return (e != null && e.Top.y == Y && e.NextInLML == null);
      }
      //------------------------------------------------------------------------------

      private bool IsIntermediate(TEdge e, double Y)
      {
          return (e.Top.y == Y && e.NextInLML != null);
      }
      //------------------------------------------------------------------------------

      internal TEdge GetMaximaPair(TEdge e)
      {
        if ((e.Next.Top == e.Top) && e.Next.NextInLML == null)
          return e.Next;
        else if ((e.Prev.Top == e.Top) && e.Prev.NextInLML == null)
          return e.Prev;
        else 
					return null;
      }
      //------------------------------------------------------------------------------

      internal TEdge GetMaximaPairEx(TEdge e)
      {
        //as above but returns null if MaxPair isn't in AEL (unless it's horizontal)
        TEdge result = GetMaximaPair(e);
        if (result == null || result.OutIdx == Skip ||
          ((result.NextInAEL == result.PrevInAEL) && !IsHorizontal(result))) return null;
        return result;
      }
      //------------------------------------------------------------------------------

      private bool ProcessIntersections(float topY)
      {
        if( m_ActiveEdges == null ) return true;
        try {
          BuildIntersectList(topY);
          if ( m_IntersectList.Count == 0) return true;
          if (m_IntersectList.Count == 1 || FixupIntersectionOrder()) 
              ProcessIntersectList();
          else 
              return false;
        }
        catch {
          m_SortedEdges = null;
          m_IntersectList.Clear();
          throw new ClipperException("ProcessIntersections error");
        }
        m_SortedEdges = null;
        return true;
      }
      //------------------------------------------------------------------------------

      private void BuildIntersectList(float topY)
      {
        if ( m_ActiveEdges == null ) return;

        //prepare for sorting ...
        TEdge e = m_ActiveEdges;
        m_SortedEdges = e;
        while( e != null )
        {
          e.PrevInSEL = e.PrevInAEL;
          e.NextInSEL = e.NextInAEL;
          e.Curr.x = TopX( e, topY );
          e = e.NextInAEL;
        }

        //bubblesort ...
        bool isModified = true;
        while( isModified && m_SortedEdges != null )
        {
          isModified = false;
          e = m_SortedEdges;
          while( e.NextInSEL != null )
          {
            TEdge eNext = e.NextInSEL;
            Vector2 pt;
            if (e.Curr.x > eNext.Curr.x)
            {
                IntersectPoint(e, eNext, out pt);
                if (pt.y < topY)
                  pt = new Vector2(TopX(e, topY), topY);
                IntersectNode newNode = new IntersectNode();
                newNode.Edge1 = e;
                newNode.Edge2 = eNext;
                newNode.Pt = pt;
                m_IntersectList.Add(newNode);

                SwapPositionsInSEL(e, eNext);
                isModified = true;
            }
            else
              e = eNext;
          }
          if( e.PrevInSEL != null ) e.PrevInSEL.NextInSEL = null;
          else break;
        }
        m_SortedEdges = null;
      }
      //------------------------------------------------------------------------------

      private bool EdgesAdjacent(IntersectNode inode)
      {
        return (inode.Edge1.NextInSEL == inode.Edge2) ||
          (inode.Edge1.PrevInSEL == inode.Edge2);
      }
      //------------------------------------------------------------------------------

      private static int IntersectNodeSort(IntersectNode node1, IntersectNode node2)
      {
        //the following typecast is safe because the differences in Pt.y will
        //be limited to the height of the scanbeam.
        return (int)(node2.Pt.y - node1.Pt.y); 
      }
      //------------------------------------------------------------------------------

      private bool FixupIntersectionOrder()
      {
        //pre-condition: intersections are sorted bottom-most first.
        //Now it's crucial that intersections are made only between adjacent edges,
        //so to ensure this the order of intersections may need adjusting ...
        m_IntersectList.Sort(m_IntersectNodeComparer);

        CopyAELToSEL();
        int cnt = m_IntersectList.Count;
        for (int i = 0; i < cnt; i++)
        {
          if (!EdgesAdjacent(m_IntersectList[i]))
          {
            int j = i + 1;
            while (j < cnt && !EdgesAdjacent(m_IntersectList[j])) j++;
            if (j == cnt) return false;

            IntersectNode tmp = m_IntersectList[i];
            m_IntersectList[i] = m_IntersectList[j];
            m_IntersectList[j] = tmp;

          }
          SwapPositionsInSEL(m_IntersectList[i].Edge1, m_IntersectList[i].Edge2);
        }
          return true;
      }
      //------------------------------------------------------------------------------

      private void ProcessIntersectList()
      {
        for (int i = 0; i < m_IntersectList.Count; i++)
        {
          IntersectNode iNode = m_IntersectList[i];
          {
            IntersectEdges(iNode.Edge1, iNode.Edge2, iNode.Pt);
            SwapPositionsInAEL(iNode.Edge1, iNode.Edge2);
          }
        }
        m_IntersectList.Clear();
      }
      //------------------------------------------------------------------------------

      internal static float Round(double value)
      {
          return value < 0 ? (float)(value - 0.5) : (float)(value + 0.5);
      }
      //------------------------------------------------------------------------------

      private static float TopX(TEdge edge, float currentY)
      {
          if (currentY == edge.Top.y)
              return edge.Top.x;
          return edge.Bot.x + Round(edge.Dx *(currentY - edge.Bot.y));
      }
      //------------------------------------------------------------------------------

      private void IntersectPoint(TEdge edge1, TEdge edge2, out Vector2 ip)
      {
        ip = new Vector2();
        double b1, b2;
        //nb: with very large coordinate values, it's possible for SlopesEqual() to 
        //return false but for the edge.Dx value be equal due to double precision rounding.
        if (edge1.Dx == edge2.Dx)
        {
          ip.y = edge1.Curr.y;
          ip.x = TopX(edge1, ip.y);
          return;
        }

        if (edge1.Delta.x == 0)
        {
            ip.x = edge1.Bot.x;
            if (IsHorizontal(edge2))
            {
                ip.y = edge2.Bot.y;
            }
            else
            {
                b2 = edge2.Bot.y - (edge2.Bot.x / edge2.Dx);
                ip.y = Round(ip.x / edge2.Dx + b2);
            }
        }
        else if (edge2.Delta.x == 0)
        {
            ip.x = edge2.Bot.x;
            if (IsHorizontal(edge1))
            {
                ip.y = edge1.Bot.y;
            }
            else
            {
                b1 = edge1.Bot.y - (edge1.Bot.x / edge1.Dx);
                ip.y = Round(ip.x / edge1.Dx + b1);
            }
        }
        else
        {
            b1 = edge1.Bot.x - edge1.Bot.y * edge1.Dx;
            b2 = edge2.Bot.x - edge2.Bot.y * edge2.Dx;
            double q = (b2 - b1) / (edge1.Dx - edge2.Dx);
            ip.y = Round(q);
            if (Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx))
                ip.x = Round(edge1.Dx * q + b1);
            else
                ip.x = Round(edge2.Dx * q + b2);
        }

        if (ip.y < edge1.Top.y || ip.y < edge2.Top.y)
        {
          if (edge1.Top.y > edge2.Top.y)
            ip.y = edge1.Top.y;
          else
            ip.y = edge2.Top.y;
          if (Math.Abs(edge1.Dx) < Math.Abs(edge2.Dx))
            ip.x = TopX(edge1, ip.y);
          else
            ip.x = TopX(edge2, ip.y);
        }
        //finally, don't allow 'ip' to be BELOW curr.y (ie bottom of scanbeam) ...
        if (ip.y > edge1.Curr.y)
        {
          ip.y = edge1.Curr.y;
          //better to use the more vertical edge to derive X ...
          if (Math.Abs(edge1.Dx) > Math.Abs(edge2.Dx)) 
            ip.x = TopX(edge2, ip.y);
          else 
            ip.x = TopX(edge1, ip.y);
        }
      }
      //------------------------------------------------------------------------------

      private void ProcessEdgesAtTopOfScanbeam(float topY)
      {
        TEdge e = m_ActiveEdges;
        while(e != null)
        {
          //1. process maxima, treating them as if they're 'bent' horizontal edges,
          //   but exclude maxima with horizontal edges. nb: e can't be a horizontal.
          bool IsMaximaEdge = IsMaxima(e, topY);

          if(IsMaximaEdge)
          {
            TEdge eMaxPair = GetMaximaPairEx(e);
            IsMaximaEdge = (eMaxPair == null || !IsHorizontal(eMaxPair));
          }

          if(IsMaximaEdge)
          {
            if (StrictlySimple) InsertMaxima(e.Top.x);
            TEdge ePrev = e.PrevInAEL;
            DoMaxima(e);
            if( ePrev == null) e = m_ActiveEdges;
            else e = ePrev.NextInAEL;
          }
          else
          {
            //2. promote horizontal edges, otherwise update Curr.x and Curr.y ...
            if (IsIntermediate(e, topY) && IsHorizontal(e.NextInLML))
            {
              UpdateEdgeIntoAEL(ref e);
              if (e.OutIdx >= 0)
                AddOutPt(e, e.Bot);
              AddEdgeToSEL(e);
            } 
            else
            {
              e.Curr.x = TopX( e, topY );
              e.Curr.y = topY;
            }
            //When StrictlySimple and 'e' is being touched by another edge, then
            //make sure both edges have a vertex here ...
            if (StrictlySimple)
            {
              TEdge ePrev = e.PrevInAEL;
              if ((e.OutIdx >= 0) && (e.WindDelta != 0) && ePrev != null &&
                (ePrev.OutIdx >= 0) && (ePrev.Curr.x == e.Curr.x) &&
                (ePrev.WindDelta != 0))
              {
                Vector2 ip = new Vector2(e.Curr.x, e.Curr.y);
                OutPt op = AddOutPt(ePrev, ip);
                OutPt op2 = AddOutPt(e, ip);
                AddJoin(op, op2, ip); //StrictlySimple (type-3) join
              }
            }

            e = e.NextInAEL;
          }
        }

        //3. Process horizontals at the Top of the scanbeam ...
        ProcessHorizontals();
        m_Maxima = null;

        //4. Promote intermediate vertices ...
        e = m_ActiveEdges;
        while (e != null)
        {
          if(IsIntermediate(e, topY))
          {
            OutPt op = null;
            if( e.OutIdx >= 0 ) 
              op = AddOutPt(e, e.Top);
            UpdateEdgeIntoAEL(ref e);

            //if output polygons share an edge, they'll need joining later ...
            TEdge ePrev = e.PrevInAEL;
            TEdge eNext = e.NextInAEL;
            if (ePrev != null && ePrev.Curr.x == e.Bot.x &&
              ePrev.Curr.y == e.Bot.y && op != null &&
              ePrev.OutIdx >= 0 && ePrev.Curr.y > ePrev.Top.y &&
              SlopesEqual(e.Curr, e.Top, ePrev.Curr, ePrev.Top, m_UseFullRange) &&
              (e.WindDelta != 0) && (ePrev.WindDelta != 0))
            {
              OutPt op2 = AddOutPt(ePrev, e.Bot);
              AddJoin(op, op2, e.Top);
            }
            else if (eNext != null && eNext.Curr.x == e.Bot.x &&
              eNext.Curr.y == e.Bot.y && op != null &&
              eNext.OutIdx >= 0 && eNext.Curr.y > eNext.Top.y &&
              SlopesEqual(e.Curr, e.Top, eNext.Curr, eNext.Top, m_UseFullRange) &&
              (e.WindDelta != 0) && (eNext.WindDelta != 0))
            {
              OutPt op2 = AddOutPt(eNext, e.Bot);
              AddJoin(op, op2, e.Top);
            }
          }
          e = e.NextInAEL;
        }
      }
      //------------------------------------------------------------------------------

      private void DoMaxima(TEdge e)
      {
        TEdge eMaxPair = GetMaximaPairEx(e);
        if (eMaxPair == null)
        {
          if (e.OutIdx >= 0)
            AddOutPt(e, e.Top);
          DeleteFromAEL(e);
          return;
        }

        TEdge eNext = e.NextInAEL;
        while(eNext != null && eNext != eMaxPair)
        {
          IntersectEdges(e, eNext, e.Top);
          SwapPositionsInAEL(e, eNext);
          eNext = e.NextInAEL;
        }

        if(e.OutIdx == Unassigned && eMaxPair.OutIdx == Unassigned)
        {
          DeleteFromAEL(e);
          DeleteFromAEL(eMaxPair);
        }
        else if( e.OutIdx >= 0 && eMaxPair.OutIdx >= 0 )
        {
          if (e.OutIdx >= 0) AddLocalMaxPoly(e, eMaxPair, e.Top);
          DeleteFromAEL(e);
          DeleteFromAEL(eMaxPair);
        }
#if use_lines
        else if (e.WindDelta == 0)
        {
          if (e.OutIdx >= 0) 
          {
            AddOutPt(e, e.Top);
            e.OutIdx = Unassigned;
          }
          DeleteFromAEL(e);

          if (eMaxPair.OutIdx >= 0)
          {
            AddOutPt(eMaxPair, e.Top);
            eMaxPair.OutIdx = Unassigned;
          }
          DeleteFromAEL(eMaxPair);
        } 
#endif
        else throw new ClipperException("DoMaxima error");
      }
      //------------------------------------------------------------------------------

      public static void ReversePaths(List<List<Vector2>> polys)
      {
        foreach (var poly in polys) { poly.Reverse(); }
      }
      //------------------------------------------------------------------------------

      public static bool Orientation(List<Vector2> poly)
      {
          return Area(poly) >= 0;
      }
      //------------------------------------------------------------------------------

      private int PointCount(OutPt pts)
      {
          if (pts == null) return 0;
          int result = 0;
          OutPt p = pts;
          do
          {
              result++;
              p = p.Next;
          }
          while (p != pts);
          return result;
      }
      //------------------------------------------------------------------------------

      private void BuildResult(List<List<Vector2>> polyg)
      {
          polyg.Clear();
          polyg.Capacity = m_PolyOuts.Count;
          for (int i = 0; i < m_PolyOuts.Count; i++)
          {
              OutRec outRec = m_PolyOuts[i];
              if (outRec.Pts == null) continue;
              OutPt p = outRec.Pts.Prev;
              int cnt = PointCount(p);
              if (cnt < 2) continue;
              List<Vector2> pg = new List<Vector2>(cnt);
              for (int j = 0; j < cnt; j++)
              {
                  pg.Add(p.Pt);
                  p = p.Prev;
              }
              polyg.Add(pg);
          }
      }
      //------------------------------------------------------------------------------

      private void BuildResult2(PolyTree polytree)
      {
          polytree.Clear();

          //add each output polygon/contour to polytree ...
          polytree.m_AllPolys.Capacity = m_PolyOuts.Count;
          for (int i = 0; i < m_PolyOuts.Count; i++)
          {
              OutRec outRec = m_PolyOuts[i];
              int cnt = PointCount(outRec.Pts);
              if ((outRec.IsOpen && cnt < 2) || 
                (!outRec.IsOpen && cnt < 3)) continue;
              FixHoleLinkage(outRec);
              PolyNode pn = new PolyNode();
              polytree.m_AllPolys.Add(pn);
              outRec.PolyNode = pn;
              pn.m_polygon.Capacity = cnt;
              OutPt op = outRec.Pts.Prev;
              for (int j = 0; j < cnt; j++)
              {
                  pn.m_polygon.Add(op.Pt);
                  op = op.Prev;
              }
          }

          //fixup PolyNode links etc ...
          polytree.m_Childs.Capacity = m_PolyOuts.Count;
          for (int i = 0; i < m_PolyOuts.Count; i++)
          {
              OutRec outRec = m_PolyOuts[i];
              if (outRec.PolyNode == null) continue;
              else if (outRec.IsOpen)
              {
                outRec.PolyNode.IsOpen = true;
                polytree.AddChild(outRec.PolyNode);
              }
              else if (outRec.FirstLeft != null && 
                outRec.FirstLeft.PolyNode != null)
                  outRec.FirstLeft.PolyNode.AddChild(outRec.PolyNode);
              else
                polytree.AddChild(outRec.PolyNode);
          }
      }
      //------------------------------------------------------------------------------

      private void FixupOutPolyline(OutRec outrec)
      {
        OutPt pp = outrec.Pts;
        OutPt lastPP = pp.Prev;
        while (pp != lastPP)
        {
            pp = pp.Next;
            if (pp.Pt == pp.Prev.Pt)
            {
                if (pp == lastPP) lastPP = pp.Prev;
                OutPt tmpPP = pp.Prev;
                tmpPP.Next = pp.Next;
                pp.Next.Prev = tmpPP;
                pp = tmpPP;
            }
        }
        if (pp == pp.Prev) outrec.Pts = null;
      }
      //------------------------------------------------------------------------------

      private void FixupOutPolygon(OutRec outRec)
      {
          //FixupOutPolygon() - removes duplicate points and simplifies consecutive
          //parallel edges by removing the middle vertex.
          OutPt lastOK = null;
          outRec.BottomPt = null;
          OutPt pp = outRec.Pts;
          bool preserveCol = PreserveCollinear || StrictlySimple;
          for (;;)
          {
              if (pp.Prev == pp || pp.Prev == pp.Next)
              {
                  outRec.Pts = null;
                  return;
              }
              //test for duplicate points and collinear edges ...
              if ((pp.Pt == pp.Next.Pt) || (pp.Pt == pp.Prev.Pt) ||
                (SlopesEqual(pp.Prev.Pt, pp.Pt, pp.Next.Pt, m_UseFullRange) &&
                (!preserveCol || !Pt2IsBetweenPt1AndPt3(pp.Prev.Pt, pp.Pt, pp.Next.Pt))))
              {
                  lastOK = null;
                  pp.Prev.Next = pp.Next;
                  pp.Next.Prev = pp.Prev;
                  pp = pp.Prev;
              }
              else if (pp == lastOK) break;
              else
              {
                  if (lastOK == null) lastOK = pp;
                  pp = pp.Next;
              }
          }
          outRec.Pts = pp;
      }
      //------------------------------------------------------------------------------

      OutPt DupOutPt(OutPt outPt, bool InsertAfter)
      {
        OutPt result = new OutPt();
        result.Pt = outPt.Pt;
        result.Idx = outPt.Idx;
        if (InsertAfter)
        {
          result.Next = outPt.Next;
          result.Prev = outPt;
          outPt.Next.Prev = result;
          outPt.Next = result;
        } 
        else
        {
          result.Prev = outPt.Prev;
          result.Next = outPt;
          outPt.Prev.Next = result;
          outPt.Prev = result;
        }
        return result;
      }
      //------------------------------------------------------------------------------

      bool GetOverlap(float a1, float a2, float b1, float b2, out float Left, out float Right)
      {
        if (a1 < a2)
        {
          if (b1 < b2) {Left = Math.Max(a1,b1); Right = Math.Min(a2,b2);}
          else {Left = Math.Max(a1,b2); Right = Math.Min(a2,b1);}
        } 
        else
        {
          if (b1 < b2) {Left = Math.Max(a2,b1); Right = Math.Min(a1,b2);}
          else { Left = Math.Max(a2, b2); Right = Math.Min(a1, b1); }
        }
        return Left < Right;
      }
      //------------------------------------------------------------------------------

      bool JoinHorz(OutPt op1, OutPt op1b, OutPt op2, OutPt op2b, 
        Vector2 Pt, bool DiscardLeft)
      {
        Direction Dir1 = (op1.Pt.x > op1b.Pt.x ? 
          Direction.dRightToLeft : Direction.dLeftToRight);
        Direction Dir2 = (op2.Pt.x > op2b.Pt.x ?
          Direction.dRightToLeft : Direction.dLeftToRight);
        if (Dir1 == Dir2) return false;

        //When DiscardLeft, we want Op1b to be on the Left of Op1, otherwise we
        //want Op1b to be on the Right. (And likewise with Op2 and Op2b.)
        //So, to facilitate this while inserting Op1b and Op2b ...
        //when DiscardLeft, make sure we're AT or RIGHT of Pt before adding Op1b,
        //otherwise make sure we're AT or LEFT of Pt. (Likewise with Op2b.)
        if (Dir1 == Direction.dLeftToRight) 
        {
          while (op1.Next.Pt.x <= Pt.x && 
            op1.Next.Pt.x >= op1.Pt.x && op1.Next.Pt.y == Pt.y)  
              op1 = op1.Next;
          if (DiscardLeft && (op1.Pt.x != Pt.x)) op1 = op1.Next;
          op1b = DupOutPt(op1, !DiscardLeft);
          if (op1b.Pt != Pt) 
          {
            op1 = op1b;
            op1.Pt = Pt;
            op1b = DupOutPt(op1, !DiscardLeft);
          }
        } 
        else
        {
          while (op1.Next.Pt.x >= Pt.x && 
            op1.Next.Pt.x <= op1.Pt.x && op1.Next.Pt.y == Pt.y) 
              op1 = op1.Next;
          if (!DiscardLeft && (op1.Pt.x != Pt.x)) op1 = op1.Next;
          op1b = DupOutPt(op1, DiscardLeft);
          if (op1b.Pt != Pt)
          {
            op1 = op1b;
            op1.Pt = Pt;
            op1b = DupOutPt(op1, DiscardLeft);
          }
        }

        if (Dir2 == Direction.dLeftToRight)
        {
          while (op2.Next.Pt.x <= Pt.x && 
            op2.Next.Pt.x >= op2.Pt.x && op2.Next.Pt.y == Pt.y)
              op2 = op2.Next;
          if (DiscardLeft && (op2.Pt.x != Pt.x)) op2 = op2.Next;
          op2b = DupOutPt(op2, !DiscardLeft);
          if (op2b.Pt != Pt)
          {
            op2 = op2b;
            op2.Pt = Pt;
            op2b = DupOutPt(op2, !DiscardLeft);
          };
        } else
        {
          while (op2.Next.Pt.x >= Pt.x && 
            op2.Next.Pt.x <= op2.Pt.x && op2.Next.Pt.y == Pt.y) 
              op2 = op2.Next;
          if (!DiscardLeft && (op2.Pt.x != Pt.x)) op2 = op2.Next;
          op2b = DupOutPt(op2, DiscardLeft);
          if (op2b.Pt != Pt)
          {
            op2 = op2b;
            op2.Pt = Pt;
            op2b = DupOutPt(op2, DiscardLeft);
          };
        };

        if ((Dir1 == Direction.dLeftToRight) == DiscardLeft)
        {
          op1.Prev = op2;
          op2.Next = op1;
          op1b.Next = op2b;
          op2b.Prev = op1b;
        }
        else
        {
          op1.Next = op2;
          op2.Prev = op1;
          op1b.Prev = op2b;
          op2b.Next = op1b;
        }
        return true;
      }
      //------------------------------------------------------------------------------

      private bool JoinPoints(Join j, OutRec outRec1, OutRec outRec2)
      {
        OutPt op1 = j.OutPt1, op1b;
        OutPt op2 = j.OutPt2, op2b;

        //There are 3 kinds of joins for output polygons ...
        //1. Horizontal joins where Join.OutPt1 & Join.OutPt2 are vertices anywhere
        //along (horizontal) collinear edges (& Join.OffPt is on the same horizontal).
        //2. Non-horizontal joins where Join.OutPt1 & Join.OutPt2 are at the same
        //location at the Bottom of the overlapping segment (& Join.OffPt is above).
        //3. StrictlySimple joins where edges touch but are not collinear and where
        //Join.OutPt1, Join.OutPt2 & Join.OffPt all share the same point.
        bool isHorizontal = (j.OutPt1.Pt.y == j.OffPt.y);

        if (isHorizontal && (j.OffPt == j.OutPt1.Pt) && (j.OffPt == j.OutPt2.Pt))
        {          
          //Strictly Simple join ...
          if (outRec1 != outRec2) return false;
          op1b = j.OutPt1.Next;
          while (op1b != op1 && (op1b.Pt == j.OffPt)) 
            op1b = op1b.Next;
          bool reverse1 = (op1b.Pt.y > j.OffPt.y);
          op2b = j.OutPt2.Next;
          while (op2b != op2 && (op2b.Pt == j.OffPt)) 
            op2b = op2b.Next;
          bool reverse2 = (op2b.Pt.y > j.OffPt.y);
          if (reverse1 == reverse2) return false;
          if (reverse1)
          {
            op1b = DupOutPt(op1, false);
            op2b = DupOutPt(op2, true);
            op1.Prev = op2;
            op2.Next = op1;
            op1b.Next = op2b;
            op2b.Prev = op1b;
            j.OutPt1 = op1;
            j.OutPt2 = op1b;
            return true;
          } else
          {
            op1b = DupOutPt(op1, true);
            op2b = DupOutPt(op2, false);
            op1.Next = op2;
            op2.Prev = op1;
            op1b.Prev = op2b;
            op2b.Next = op1b;
            j.OutPt1 = op1;
            j.OutPt2 = op1b;
            return true;
          }
        } 
        else if (isHorizontal)
        {
          //treat horizontal joins differently to non-horizontal joins since with
          //them we're not yet sure where the overlapping is. OutPt1.Pt & OutPt2.Pt
          //may be anywhere along the horizontal edge.
          op1b = op1;
          while (op1.Prev.Pt.y == op1.Pt.y && op1.Prev != op1b && op1.Prev != op2)
            op1 = op1.Prev;
          while (op1b.Next.Pt.y == op1b.Pt.y && op1b.Next != op1 && op1b.Next != op2)
            op1b = op1b.Next;
          if (op1b.Next == op1 || op1b.Next == op2) return false; //a flat 'polygon'

          op2b = op2;
          while (op2.Prev.Pt.y == op2.Pt.y && op2.Prev != op2b && op2.Prev != op1b)
            op2 = op2.Prev;
          while (op2b.Next.Pt.y == op2b.Pt.y && op2b.Next != op2 && op2b.Next != op1)
            op2b = op2b.Next;
          if (op2b.Next == op2 || op2b.Next == op1) return false; //a flat 'polygon'

          float Left, Right;
          //Op1 -. Op1b & Op2 -. Op2b are the extremites of the horizontal edges
          if (!GetOverlap(op1.Pt.x, op1b.Pt.x, op2.Pt.x, op2b.Pt.x, out Left, out Right))
            return false;

          //DiscardLeftSide: when overlapping edges are joined, a spike will created
          //which needs to be cleaned up. However, we don't want Op1 or Op2 caught up
          //on the discard Side as either may still be needed for other joins ...
          Vector2 Pt;
          bool DiscardLeftSide;
          if (op1.Pt.x >= Left && op1.Pt.x <= Right) 
          {
            Pt = op1.Pt; DiscardLeftSide = (op1.Pt.x > op1b.Pt.x);
          } 
          else if (op2.Pt.x >= Left&& op2.Pt.x <= Right) 
          {
            Pt = op2.Pt; DiscardLeftSide = (op2.Pt.x > op2b.Pt.x);
          } 
          else if (op1b.Pt.x >= Left && op1b.Pt.x <= Right)
          {
            Pt = op1b.Pt; DiscardLeftSide = op1b.Pt.x > op1.Pt.x;
          } 
          else
          {
            Pt = op2b.Pt; DiscardLeftSide = (op2b.Pt.x > op2.Pt.x);
          }
          j.OutPt1 = op1;
          j.OutPt2 = op2;
          return JoinHorz(op1, op1b, op2, op2b, Pt, DiscardLeftSide);
        } else
        {
          //nb: For non-horizontal joins ...
          //    1. Jr.OutPt1.Pt.y == Jr.OutPt2.Pt.y
          //    2. Jr.OutPt1.Pt > Jr.OffPt.y

          //make sure the polygons are correctly oriented ...
          op1b = op1.Next;
          while ((op1b.Pt == op1.Pt) && (op1b != op1)) op1b = op1b.Next;
          bool Reverse1 = ((op1b.Pt.y > op1.Pt.y) ||
            !SlopesEqual(op1.Pt, op1b.Pt, j.OffPt, m_UseFullRange));
          if (Reverse1)
          {
            op1b = op1.Prev;
            while ((op1b.Pt == op1.Pt) && (op1b != op1)) op1b = op1b.Prev;
            if ((op1b.Pt.y > op1.Pt.y) ||
              !SlopesEqual(op1.Pt, op1b.Pt, j.OffPt, m_UseFullRange)) return false;
          };
          op2b = op2.Next;
          while ((op2b.Pt == op2.Pt) && (op2b != op2)) op2b = op2b.Next;
          bool Reverse2 = ((op2b.Pt.y > op2.Pt.y) ||
            !SlopesEqual(op2.Pt, op2b.Pt, j.OffPt, m_UseFullRange));
          if (Reverse2)
          {
            op2b = op2.Prev;
            while ((op2b.Pt == op2.Pt) && (op2b != op2)) op2b = op2b.Prev;
            if ((op2b.Pt.y > op2.Pt.y) ||
              !SlopesEqual(op2.Pt, op2b.Pt, j.OffPt, m_UseFullRange)) return false;
          }

          if ((op1b == op1) || (op2b == op2) || (op1b == op2b) ||
            ((outRec1 == outRec2) && (Reverse1 == Reverse2))) return false;

          if (Reverse1)
          {
            op1b = DupOutPt(op1, false);
            op2b = DupOutPt(op2, true);
            op1.Prev = op2;
            op2.Next = op1;
            op1b.Next = op2b;
            op2b.Prev = op1b;
            j.OutPt1 = op1;
            j.OutPt2 = op1b;
            return true;
          } else
          {
            op1b = DupOutPt(op1, true);
            op2b = DupOutPt(op2, false);
            op1.Next = op2;
            op2.Prev = op1;
            op1b.Prev = op2b;
            op2b.Next = op1b;
            j.OutPt1 = op1;
            j.OutPt2 = op1b;
            return true;
          }
        }
      }
      //----------------------------------------------------------------------

      public static int PointInPolygon(Vector2 pt, List<Vector2> path)
      {
        //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
        //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
        //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
        int result = 0, cnt = path.Count;
        if (cnt < 3) return 0;
        Vector2 ip = path[0];
        for (int i = 1; i <= cnt; ++i)
        {
          Vector2 ipNext = (i == cnt ? path[0] : path[i]);
          if (ipNext.y == pt.y)
          {
            if ((ipNext.x == pt.x) || (ip.y == pt.y &&
              ((ipNext.x > pt.x) == (ip.x < pt.x)))) return -1;
          }
          if ((ip.y < pt.y) != (ipNext.y < pt.y))
          {
            if (ip.x >= pt.x)
            {
              if (ipNext.x > pt.x) result = 1 - result;
              else
              {
                double d = (double)(ip.x - pt.x) * (ipNext.y - pt.y) -
                  (double)(ipNext.x - pt.x) * (ip.y - pt.y);
                if (d == 0) return -1;
                else if ((d > 0) == (ipNext.y > ip.y)) result = 1 - result;
              }
            }
            else
            {
              if (ipNext.x > pt.x)
              {
                double d = (double)(ip.x - pt.x) * (ipNext.y - pt.y) -
                  (double)(ipNext.x - pt.x) * (ip.y - pt.y);
                if (d == 0) return -1;
                else if ((d > 0) == (ipNext.y > ip.y)) result = 1 - result;
              }
            }
          }
          ip = ipNext;
        }
        return result;
      }
      //------------------------------------------------------------------------------

      //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
      //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
      private static int PointInPolygon(Vector2 pt, OutPt op)
      {
        //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
        int result = 0;
        OutPt startOp = op;
        float ptx = pt.x, pty = pt.y;
        float poly0x = op.Pt.x, poly0y = op.Pt.y;
        do
        {
          op = op.Next;
          float poly1x = op.Pt.x, poly1y = op.Pt.y;

          if (poly1y == pty)
          {
            if ((poly1x == ptx) || (poly0y == pty &&
              ((poly1x > ptx) == (poly0x < ptx)))) return -1;
          }
          if ((poly0y < pty) != (poly1y < pty))
          {
            if (poly0x >= ptx)
            {
              if (poly1x > ptx) result = 1 - result;
              else
              {
                double d = (double)(poly0x - ptx) * (poly1y - pty) -
                  (double)(poly1x - ptx) * (poly0y - pty);
                if (d == 0) return -1;
                if ((d > 0) == (poly1y > poly0y)) result = 1 - result;
              }
            }
            else
            {
              if (poly1x > ptx)
              {
                double d = (double)(poly0x - ptx) * (poly1y - pty) -
                  (double)(poly1x - ptx) * (poly0y - pty);
                if (d == 0) return -1;
                if ((d > 0) == (poly1y > poly0y)) result = 1 - result;
              }
            }
          }
          poly0x = poly1x; poly0y = poly1y;
        } while (startOp != op);
        return result;
      }
      //------------------------------------------------------------------------------

      private static bool Poly2ContainsPoly1(OutPt outPt1, OutPt outPt2)
      {
        OutPt op = outPt1;
        do
        {
          //nb: PointInPolygon returns 0 if false, +1 if true, -1 if pt on polygon
          int res = PointInPolygon(op.Pt, outPt2);
          if (res >= 0) return res > 0;
          op = op.Next;
        }
        while (op != outPt1);
        return true;
      }
      //----------------------------------------------------------------------

      private void FixupFirstLefts1(OutRec OldOutRec, OutRec NewOutRec)
      { 
        foreach (OutRec outRec in m_PolyOuts)
        {
          OutRec firstLeft = ParseFirstLeft(outRec.FirstLeft);
          if (outRec.Pts != null && firstLeft == OldOutRec)
          {
            if (Poly2ContainsPoly1(outRec.Pts, NewOutRec.Pts))
                outRec.FirstLeft = NewOutRec;
          }
        }
      }
      //----------------------------------------------------------------------

      private void FixupFirstLefts2(OutRec innerOutRec, OutRec outerOutRec)
      {
        //A polygon has split into two such that one is now the inner of the other.
        //It's possible that these polygons now wrap around other polygons, so check
        //every polygon that's also contained by OuterOutRec's FirstLeft container
        //(including nil) to see if they've become inner to the new inner polygon ...
        OutRec orfl = outerOutRec.FirstLeft;
        foreach (OutRec outRec in m_PolyOuts)
        {
          if (outRec.Pts == null || outRec == outerOutRec || outRec == innerOutRec) 
            continue;
          OutRec firstLeft = ParseFirstLeft(outRec.FirstLeft);
          if (firstLeft != orfl && firstLeft != innerOutRec && firstLeft != outerOutRec) 
            continue;
          if (Poly2ContainsPoly1(outRec.Pts, innerOutRec.Pts))
            outRec.FirstLeft = innerOutRec;
          else if (Poly2ContainsPoly1(outRec.Pts, outerOutRec.Pts))
            outRec.FirstLeft = outerOutRec;
          else if (outRec.FirstLeft == innerOutRec || outRec.FirstLeft == outerOutRec) 
            outRec.FirstLeft = orfl;
        }
      }
      //----------------------------------------------------------------------

      private void FixupFirstLefts3(OutRec OldOutRec, OutRec NewOutRec)
      {
        //same as FixupFirstLefts1 but doesn't call Poly2ContainsPoly1()
        foreach (OutRec outRec in m_PolyOuts)
        {
          OutRec firstLeft = ParseFirstLeft(outRec.FirstLeft);
          if (outRec.Pts != null && firstLeft == OldOutRec) 
            outRec.FirstLeft = NewOutRec;
        }
      }
      //----------------------------------------------------------------------

      private static OutRec ParseFirstLeft(OutRec FirstLeft)
      {
        while (FirstLeft != null && FirstLeft.Pts == null) 
          FirstLeft = FirstLeft.FirstLeft;
        return FirstLeft;
      }
      //------------------------------------------------------------------------------

    private void JoinCommonEdges()
      {
        for (int i = 0; i < m_Joins.Count; i++)
        {
          Join join = m_Joins[i];

          OutRec outRec1 = GetOutRec(join.OutPt1.Idx);
          OutRec outRec2 = GetOutRec(join.OutPt2.Idx);

          if (outRec1.Pts == null || outRec2.Pts == null) continue;
          if (outRec1.IsOpen || outRec2.IsOpen) continue;

          //get the polygon fragment with the correct hole state (FirstLeft)
          //before calling JoinPoints() ...
          OutRec holeStateRec;
          if (outRec1 == outRec2) holeStateRec = outRec1;
          else if (OutRec1RightOfOutRec2(outRec1, outRec2)) holeStateRec = outRec2;
          else if (OutRec1RightOfOutRec2(outRec2, outRec1)) holeStateRec = outRec1;
          else holeStateRec = GetLowermostRec(outRec1, outRec2);

          if (!JoinPoints(join, outRec1, outRec2)) continue;

          if (outRec1 == outRec2)
          {
            //instead of joining two polygons, we've just created a new one by
            //splitting one polygon into two.
            outRec1.Pts = join.OutPt1;
            outRec1.BottomPt = null;
            outRec2 = CreateOutRec();
            outRec2.Pts = join.OutPt2;

            //update all OutRec2.Pts Idx's ...
            UpdateOutPtIdxs(outRec2);

            if (Poly2ContainsPoly1(outRec2.Pts, outRec1.Pts))
            {
              //outRec1 contains outRec2 ...
              outRec2.IsHole = !outRec1.IsHole;
              outRec2.FirstLeft = outRec1;

              if (m_UsingPolyTree) FixupFirstLefts2(outRec2, outRec1);

              if ((outRec2.IsHole ^ ReverseSolution) == (Area(outRec2) > 0))
                ReversePolyPtLinks(outRec2.Pts);

            }
            else if (Poly2ContainsPoly1(outRec1.Pts, outRec2.Pts))
            {
              //outRec2 contains outRec1 ...
              outRec2.IsHole = outRec1.IsHole;
              outRec1.IsHole = !outRec2.IsHole;
              outRec2.FirstLeft = outRec1.FirstLeft;
              outRec1.FirstLeft = outRec2;

              if (m_UsingPolyTree) FixupFirstLefts2(outRec1, outRec2);

              if ((outRec1.IsHole ^ ReverseSolution) == (Area(outRec1) > 0))
                ReversePolyPtLinks(outRec1.Pts);
            }
            else
            {
              //the 2 polygons are completely separate ...
              outRec2.IsHole = outRec1.IsHole;
              outRec2.FirstLeft = outRec1.FirstLeft;

              //fixup FirstLeft pointers that may need reassigning to OutRec2
              if (m_UsingPolyTree) FixupFirstLefts1(outRec1, outRec2);
            }
     
          } else
          {
            //joined 2 polygons together ...

            outRec2.Pts = null;
            outRec2.BottomPt = null;
            outRec2.Idx = outRec1.Idx;

            outRec1.IsHole = holeStateRec.IsHole;
            if (holeStateRec == outRec2) 
              outRec1.FirstLeft = outRec2.FirstLeft;
            outRec2.FirstLeft = outRec1;

            //fixup FirstLeft pointers that may need reassigning to OutRec1
            if (m_UsingPolyTree) FixupFirstLefts3(outRec2, outRec1);
          }
        }
      }
      //------------------------------------------------------------------------------

      private void UpdateOutPtIdxs(OutRec outrec)
      {  
        OutPt op = outrec.Pts;
        do
        {
          op.Idx = outrec.Idx;
          op = op.Prev;
        }
        while(op != outrec.Pts);
      }
      //------------------------------------------------------------------------------

      private void DoSimplePolygons()
      {
        int i = 0;
        while (i < m_PolyOuts.Count) 
        {
          OutRec outrec = m_PolyOuts[i++];
          OutPt op = outrec.Pts;
          if (op == null || outrec.IsOpen) continue;
          do //for each Pt in Polygon until duplicate found do ...
          {
            OutPt op2 = op.Next;
            while (op2 != outrec.Pts) 
            {
              if ((op.Pt == op2.Pt) && op2.Next != op && op2.Prev != op) 
              {
                //split the polygon into two ...
                OutPt op3 = op.Prev;
                OutPt op4 = op2.Prev;
                op.Prev = op4;
                op4.Next = op;
                op2.Prev = op3;
                op3.Next = op2;

                outrec.Pts = op;
                OutRec outrec2 = CreateOutRec();
                outrec2.Pts = op2;
                UpdateOutPtIdxs(outrec2);
                if (Poly2ContainsPoly1(outrec2.Pts, outrec.Pts))
                {
                  //OutRec2 is contained by OutRec1 ...
                  outrec2.IsHole = !outrec.IsHole;
                  outrec2.FirstLeft = outrec;
                  if (m_UsingPolyTree) FixupFirstLefts2(outrec2, outrec);
                }
                else
                  if (Poly2ContainsPoly1(outrec.Pts, outrec2.Pts))
                {
                  //OutRec1 is contained by OutRec2 ...
                  outrec2.IsHole = outrec.IsHole;
                  outrec.IsHole = !outrec2.IsHole;
                  outrec2.FirstLeft = outrec.FirstLeft;
                  outrec.FirstLeft = outrec2;
                  if (m_UsingPolyTree) FixupFirstLefts2(outrec, outrec2);
                }
                  else
                {
                  //the 2 polygons are separate ...
                  outrec2.IsHole = outrec.IsHole;
                  outrec2.FirstLeft = outrec.FirstLeft;
                  if (m_UsingPolyTree) FixupFirstLefts1(outrec, outrec2);
                }
                op2 = op; //ie get ready for the next iteration
              }
              op2 = op2.Next;
            }
            op = op.Next;
          }
          while (op != outrec.Pts);
        }
      }
      //------------------------------------------------------------------------------

      public static double Area(List<Vector2> poly)
      {
        int cnt = (int)poly.Count;
        if (cnt < 3) return 0;
        double a = 0;
        for (int i = 0, j = cnt - 1; i < cnt; ++i)
        {
          a += ((double)poly[j].x + poly[i].x) * ((double)poly[j].y - poly[i].y);
          j = i;
        }
        return -a * 0.5;
      }
      //------------------------------------------------------------------------------

      internal double Area(OutRec outRec)
      {
        return Area(outRec.Pts);
      }
      //------------------------------------------------------------------------------

      internal double Area(OutPt op)
      {
        OutPt opFirst = op;
        if (op == null) return 0;
        double a = 0;
        do {
          a = a + (double)(op.Prev.Pt.x + op.Pt.x) * (double)(op.Prev.Pt.y - op.Pt.y);
          op = op.Next;
        } while (op != opFirst);
        return a * 0.5;
      }

      //------------------------------------------------------------------------------
      // SimplifyPolygon functions ...
      // Convert self-intersecting polygons into simple polygons
      //------------------------------------------------------------------------------

     

      public static List<List<Vector2>> SimplifyPolygon(List<Vector2> poly, 
            PolyFillType fillType = PolyFillType.pftEvenOdd)
      {
          List<List<Vector2>> result = new List<List<Vector2>>();
          Clipper c = new Clipper();
          c.StrictlySimple = true;
          c.AddPath(poly, PolyType.ptSubject, true);
          c.Execute(ClipType.ctUnion, result, fillType, fillType);
          return result;
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> SimplifyPolygons(List<List<Vector2>> polys,
          PolyFillType fillType = PolyFillType.pftEvenOdd)
      {
          List<List<Vector2>> result = new List<List<Vector2>>();
          Clipper c = new Clipper();
          c.StrictlySimple = true;
          c.AddPaths(polys, PolyType.ptSubject, true);
          c.Execute(ClipType.ctUnion, result, fillType, fillType);
          return result;
      }
      //------------------------------------------------------------------------------

      private static double DistanceSqrd(Vector2 pt1, Vector2 pt2)
      {
        double dx = ((double)pt1.x - pt2.x);
        double dy = ((double)pt1.y - pt2.y);
        return (dx*dx + dy*dy);
      }
      //------------------------------------------------------------------------------

      private static double DistanceFromLineSqrd(Vector2 pt, Vector2 ln1, Vector2 ln2)
      {
        //The equation of a line in general form (Ax + By + C = 0)
        //given 2 points (x¹,y¹) & (x²,y²) is ...
        //(y¹ - y²)x + (x² - x¹)y + (y² - y¹)x¹ - (x² - x¹)y¹ = 0
        //A = (y¹ - y²); B = (x² - x¹); C = (y² - y¹)x¹ - (x² - x¹)y¹
        //perpendicular distance of point (x³,y³) = (Ax³ + By³ + C)/Sqrt(A² + B²)
        //see http://en.wikipedia.org/wiki/Perpendicular_distance
        double A = ln1.y - ln2.y;
        double B = ln2.x - ln1.x;
        double C = A * ln1.x  + B * ln1.y;
        C = A * pt.x + B * pt.y - C;
        return (C * C) / (A * A + B * B);
      }
      //---------------------------------------------------------------------------

      private static bool SlopesNearCollinear(Vector2 pt1, 
          Vector2 pt2, Vector2 pt3, double distSqrd)
      {
        //this function is more accurate when the point that's GEOMETRICALLY 
        //between the other 2 points is the one that's tested for distance.  
        //nb: with 'spikes', either pt1 or pt3 is geometrically between the other pts                    
        if (Math.Abs(pt1.x - pt2.x) > Math.Abs(pt1.y - pt2.y))
	      {
          if ((pt1.x > pt2.x) == (pt1.x < pt3.x))
            return DistanceFromLineSqrd(pt1, pt2, pt3) < distSqrd;
          else if ((pt2.x > pt1.x) == (pt2.x < pt3.x))
            return DistanceFromLineSqrd(pt2, pt1, pt3) < distSqrd;
		      else
	          return DistanceFromLineSqrd(pt3, pt1, pt2) < distSqrd;
	      }
	      else
	      {
          if ((pt1.y > pt2.y) == (pt1.y < pt3.y))
            return DistanceFromLineSqrd(pt1, pt2, pt3) < distSqrd;
          else if ((pt2.y > pt1.y) == (pt2.y < pt3.y))
            return DistanceFromLineSqrd(pt2, pt1, pt3) < distSqrd;
		      else
            return DistanceFromLineSqrd(pt3, pt1, pt2) < distSqrd;
	      }
      }
      //------------------------------------------------------------------------------

      private static bool PointsAreClose(Vector2 pt1, Vector2 pt2, double distSqrd)
      {
          double dx = (double)pt1.x - pt2.x;
          double dy = (double)pt1.y - pt2.y;
          return ((dx * dx) + (dy * dy) <= distSqrd);
      }
      //------------------------------------------------------------------------------

      private static OutPt ExcludeOp(OutPt op)
      {
        OutPt result = op.Prev;
        result.Next = op.Next;
        op.Next.Prev = result;
        result.Idx = 0;
        return result;
      }
      //------------------------------------------------------------------------------

      public static List<Vector2> CleanPolygon(List<Vector2> path, double distance = 1.415)
      {
        //distance = proximity in units/pixels below which vertices will be stripped. 
        //Default ~= sqrt(2) so when adjacent vertices or semi-adjacent vertices have 
        //both x & y coords within 1 unit, then the second vertex will be stripped.

        int cnt = path.Count;

        if (cnt == 0) return new List<Vector2>();

        OutPt [] outPts = new OutPt[cnt];
        for (int i = 0; i < cnt; ++i) outPts[i] = new OutPt();

        for (int i = 0; i < cnt; ++i)
        {
          outPts[i].Pt = path[i];
          outPts[i].Next = outPts[(i + 1) % cnt];
          outPts[i].Next.Prev = outPts[i];
          outPts[i].Idx = 0;
        }

        double distSqrd = distance * distance;
        OutPt op = outPts[0];
        while (op.Idx == 0 && op.Next != op.Prev)
        {
          if (PointsAreClose(op.Pt, op.Prev.Pt, distSqrd))
          {
            op = ExcludeOp(op);
            cnt--;
          }
          else if (PointsAreClose(op.Prev.Pt, op.Next.Pt, distSqrd))
          {
            ExcludeOp(op.Next);
            op = ExcludeOp(op);
            cnt -= 2;
          }
          else if (SlopesNearCollinear(op.Prev.Pt, op.Pt, op.Next.Pt, distSqrd))
          {
            op = ExcludeOp(op);
            cnt--;
          }
          else
          {
            op.Idx = 1;
            op = op.Next;
          }
        }

        if (cnt < 3) cnt = 0;
        List<Vector2> result = new List<Vector2>(cnt);
        for (int i = 0; i < cnt; ++i)
        {
          result.Add(op.Pt);
          op = op.Next;
        }
        outPts = null;
        return result;
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> CleanPolygons(List<List<Vector2>> polys,
          double distance = 1.415)
      {
        List<List<Vector2>> result = new List<List<Vector2>>(polys.Count);
        for (int i = 0; i < polys.Count; i++)
          result.Add(CleanPolygon(polys[i], distance));
        return result;
      }
      //------------------------------------------------------------------------------

      internal static List<List<Vector2>> Minkowski(List<Vector2> pattern, List<Vector2> path, bool IsSum, bool IsClosed)
      {
        int delta = (IsClosed ? 1 : 0);
        int polyCnt = pattern.Count;
        int pathCnt = path.Count;
        List<List<Vector2>> result = new List<List<Vector2>>(pathCnt);
        if (IsSum)
          for (int i = 0; i < pathCnt; i++)
          {
            List<Vector2> p = new List<Vector2>(polyCnt);
            foreach (Vector2 ip in pattern)
              p.Add(new Vector2(path[i].x + ip.x, path[i].y + ip.y));
            result.Add(p);
          }
        else
          for (int i = 0; i < pathCnt; i++)
          {
            List<Vector2> p = new List<Vector2>(polyCnt);
            foreach (Vector2 ip in pattern)
              p.Add(new Vector2(path[i].x - ip.x, path[i].y - ip.y));
            result.Add(p);
          }

        List<List<Vector2>> quads = new List<List<Vector2>>((pathCnt + delta) * (polyCnt + 1));
        for (int i = 0; i < pathCnt - 1 + delta; i++)
          for (int j = 0; j < polyCnt; j++)
          {
            List<Vector2> quad = new List<Vector2>(4);
            quad.Add(result[i % pathCnt][j % polyCnt]);
            quad.Add(result[(i + 1) % pathCnt][j % polyCnt]);
            quad.Add(result[(i + 1) % pathCnt][(j + 1) % polyCnt]);
            quad.Add(result[i % pathCnt][(j + 1) % polyCnt]);
            if (!Orientation(quad)) quad.Reverse();
            quads.Add(quad);
          }
        return quads;
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> MinkowskiSum(List<Vector2> pattern, List<Vector2> path, bool pathIsClosed)
      {
        List<List<Vector2>> paths = Minkowski(pattern, path, true, pathIsClosed);
        Clipper c = new Clipper();
        c.AddPaths(paths, PolyType.ptSubject, true);
        c.Execute(ClipType.ctUnion, paths, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
        return paths;
      }
      //------------------------------------------------------------------------------

      private static List<Vector2> TranslatePath(List<Vector2> path, Vector2 delta) 
      {
        List<Vector2> outPath = new List<Vector2>(path.Count);
        for (int i = 0; i < path.Count; i++)
          outPath.Add(new Vector2(path[i].x + delta.x, path[i].y + delta.y));
        return outPath;
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> MinkowskiSum(List<Vector2> pattern, List<List<Vector2>> paths, bool pathIsClosed)
      {
        List<List<Vector2>> solution = new List<List<Vector2>>();
        Clipper c = new Clipper();
        for (int i = 0; i < paths.Count; ++i)
        {
          List<List<Vector2>> tmp = Minkowski(pattern, paths[i], true, pathIsClosed);
          c.AddPaths(tmp, PolyType.ptSubject, true);
          if (pathIsClosed)
          {
            List<Vector2> path = TranslatePath(paths[i], pattern[0]);
            c.AddPath(path, PolyType.ptClip, true);
          }
        }
        c.Execute(ClipType.ctUnion, solution, 
          PolyFillType.pftNonZero, PolyFillType.pftNonZero);
        return solution;
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> MinkowskiDiff(List<Vector2> poly1, List<Vector2> poly2)
      {
        List<List<Vector2>> paths = Minkowski(poly1, poly2, false, true);
        Clipper c = new Clipper();
        c.AddPaths(paths, PolyType.ptSubject, true);
        c.Execute(ClipType.ctUnion, paths, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
        return paths;
      }
      //------------------------------------------------------------------------------

      internal enum NodeType { ntAny, ntOpen, ntClosed };

      public static List<List<Vector2>> PolyTreeToPaths(PolyTree polytree)
      {

        List<List<Vector2>> result = new List<List<Vector2>>();
        result.Capacity = polytree.Total;
        AddPolyNodeToPaths(polytree, NodeType.ntAny, result);
        return result;
      }
      //------------------------------------------------------------------------------

      internal static void AddPolyNodeToPaths(PolyNode polynode, NodeType nt, List<List<Vector2>> paths)
      {
        bool match = true;
        switch (nt)
        {
          case NodeType.ntOpen: return;
          case NodeType.ntClosed: match = !polynode.IsOpen; break;
          default: break;
        }

        if (polynode.m_polygon.Count > 0 && match)
          paths.Add(polynode.m_polygon);
        foreach (PolyNode pn in polynode.Childs)
          AddPolyNodeToPaths(pn, nt, paths);
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> OpenPathsFromPolyTree(PolyTree polytree)
      {
        List<List<Vector2>> result = new List<List<Vector2>>();
        result.Capacity = polytree.ChildCount;
        for (int i = 0; i < polytree.ChildCount; i++)
          if (polytree.Childs[i].IsOpen)
            result.Add(polytree.Childs[i].m_polygon);
        return result;
      }
      //------------------------------------------------------------------------------

      public static List<List<Vector2>> ClosedPathsFromPolyTree(PolyTree polytree)
      {
        List<List<Vector2>> result = new List<List<Vector2>>();
        result.Capacity = polytree.Total;
        AddPolyNodeToPaths(polytree, NodeType.ntClosed, result);
        return result;
      }
      //------------------------------------------------------------------------------

  } //end Clipper

  public class ClipperOffset
  {
    private List<List<Vector2>> m_destPolys;
    private List<Vector2> m_srcPoly;
    private List<Vector2> m_destPoly;
    private List<Vector2> m_normals = new List<Vector2>();
    private double m_delta, m_sinA, m_sin, m_cos;
    private double m_miterLim, m_StepsPerRad;

    private Vector2 m_lowest;
    private PolyNode m_polyNodes = new PolyNode();

    public double ArcTolerance { get; set; }
    public double MiterLimit { get; set; }

    private const double two_pi = Math.PI * 2;
    private const double def_arc_tolerance = 0.25;

    public ClipperOffset(
      double miterLimit = 2.0, double arcTolerance = def_arc_tolerance)
    {
      MiterLimit = miterLimit;
      ArcTolerance = arcTolerance;
      m_lowest.x = -1;
    }
    //------------------------------------------------------------------------------

    public void Clear()
    {
      m_polyNodes.Childs.Clear();
      m_lowest.x = -1;
    }
    //------------------------------------------------------------------------------

    internal static float Round(double value)
    {
      return value < 0 ? (float)(value - 0.5) : (float)(value + 0.5);
    }
    //------------------------------------------------------------------------------

    public void AddPath(List<Vector2> path, JoinType joinType, EndType endType)
    {
      int highI = path.Count - 1;
      if (highI < 0) return;
      PolyNode newNode = new PolyNode();
      newNode.m_jointype = joinType;
      newNode.m_endtype = endType;

      //strip duplicate points from path and also get index to the lowest point ...
      if (endType == EndType.etClosedLine || endType == EndType.etClosedPolygon)
        while (highI > 0 && path[0] == path[highI]) highI--;
      newNode.m_polygon.Capacity = highI + 1;
      newNode.m_polygon.Add(path[0]);
      int j = 0, k = 0;
      for (int i = 1; i <= highI; i++)
        if (newNode.m_polygon[j] != path[i])
        {
          j++;
          newNode.m_polygon.Add(path[i]);
          if (path[i].y > newNode.m_polygon[k].y ||
            (path[i].y == newNode.m_polygon[k].y &&
            path[i].x < newNode.m_polygon[k].x)) k = j;
        }
      if (endType == EndType.etClosedPolygon && j < 2) return;

      m_polyNodes.AddChild(newNode);

      //if this path's lowest pt is lower than all the others then update m_lowest
      if (endType != EndType.etClosedPolygon) return;
      if (m_lowest.x < 0)
        m_lowest = new Vector2(m_polyNodes.ChildCount - 1, k);
      else
      {
        Vector2 ip = m_polyNodes.Childs[(int)m_lowest.x].m_polygon[(int)m_lowest.y];
        if (newNode.m_polygon[k].y > ip.y ||
          (newNode.m_polygon[k].y == ip.y &&
          newNode.m_polygon[k].x < ip.x))
          m_lowest = new Vector2(m_polyNodes.ChildCount - 1, k);
      }
    }
    //------------------------------------------------------------------------------

    public void AddPaths(List<List<Vector2>> paths, JoinType joinType, EndType endType)
    {
      foreach (List<Vector2> p in paths)
        AddPath(p, joinType, endType);
    }
    //------------------------------------------------------------------------------

    private void FixOrientations()
    {
      //fixup orientations of all closed paths if the orientation of the
      //closed path with the lowermost vertex is wrong ...
      if (m_lowest.x >= 0 && 
        !Clipper.Orientation(m_polyNodes.Childs[(int)m_lowest.x].m_polygon))
      {
        for (int i = 0; i < m_polyNodes.ChildCount; i++)
        {
          PolyNode node = m_polyNodes.Childs[i];
          if (node.m_endtype == EndType.etClosedPolygon ||
            (node.m_endtype == EndType.etClosedLine && 
            Clipper.Orientation(node.m_polygon)))
            node.m_polygon.Reverse();
        }
      }
      else
      {
        for (int i = 0; i < m_polyNodes.ChildCount; i++)
        {
          PolyNode node = m_polyNodes.Childs[i];
          if (node.m_endtype == EndType.etClosedLine &&
            !Clipper.Orientation(node.m_polygon))
          node.m_polygon.Reverse();
        }
      }
    }
    //------------------------------------------------------------------------------

    internal static Vector2 GetUnitNormal(Vector2 pt1, Vector2 pt2)
    {
      float dx = (pt2.x - pt1.x);
            float dy = (pt2.y - pt1.y);
      if ((dx == 0) && (dy == 0)) return new Vector2();

      float f = 1 * 1.0f / Mathf.Sqrt(dx * dx + dy * dy);
      dx *= f;
      dy *= f;

      return new Vector2(dy, -dx);
    }
    //------------------------------------------------------------------------------

    private void DoOffset(double delta)
    {
      m_destPolys = new List<List<Vector2>>();
      m_delta = delta;

      //if Zero offset, just copy any CLOSED polygons to m_p and return ...
      if (ClipperBase.near_zero(delta)) 
      {
        m_destPolys.Capacity = m_polyNodes.ChildCount;
        for (int i = 0; i < m_polyNodes.ChildCount; i++)
        {
          PolyNode node = m_polyNodes.Childs[i];
          if (node.m_endtype == EndType.etClosedPolygon)
            m_destPolys.Add(node.m_polygon);
        }
        return;
      }

      //see offset_triginometry3.svg in the documentation folder ...
      if (MiterLimit > 2) m_miterLim = 2 / (MiterLimit * MiterLimit);
      else m_miterLim = 0.5;

      double y;
      if (ArcTolerance <= 0.0) 
        y = def_arc_tolerance;
      else if (ArcTolerance > Math.Abs(delta) * def_arc_tolerance)
        y = Math.Abs(delta) * def_arc_tolerance;
      else 
        y = ArcTolerance;
      //see offset_triginometry2.svg in the documentation folder ...
      double steps = Math.PI / Math.Acos(1 - y / Math.Abs(delta));
      m_sin = Math.Sin(two_pi / steps);
      m_cos = Math.Cos(two_pi / steps);
      m_StepsPerRad = steps / two_pi;
      if (delta < 0.0) m_sin = -m_sin;

      m_destPolys.Capacity = m_polyNodes.ChildCount * 2;
      for (int i = 0; i < m_polyNodes.ChildCount; i++)
      {
        PolyNode node = m_polyNodes.Childs[i];
        m_srcPoly = node.m_polygon;

        int len = m_srcPoly.Count;

        if (len == 0 || (delta <= 0 && (len < 3 || 
          node.m_endtype != EndType.etClosedPolygon)))
            continue;

        m_destPoly = new List<Vector2>();

        if (len == 1)
        {
          if (node.m_jointype == JoinType.jtRound)
          {
            double X = 1.0, Y = 0.0;
            for (int j = 1; j <= steps; j++)
            {
              m_destPoly.Add(new Vector2(
                Round(m_srcPoly[0].x + X * delta),
                Round(m_srcPoly[0].y + Y * delta)));
              double X2 = X;
              X = X * m_cos - m_sin * Y;
              Y = X2 * m_sin + Y * m_cos;
            }
          }
          else
          {
            double X = -1.0, Y = -1.0;
            for (int j = 0; j < 4; ++j)
            {
              m_destPoly.Add(new Vector2(
                Round(m_srcPoly[0].x + X * delta),
                Round(m_srcPoly[0].y + Y * delta)));
              if (X < 0) X = 1;
              else if (Y < 0) Y = 1;
              else X = -1;
            }
          }
          m_destPolys.Add(m_destPoly);
          continue;
        }

        //build m_normals ...
        m_normals.Clear();
        m_normals.Capacity = len;
        for (int j = 0; j < len - 1; j++)
          m_normals.Add(GetUnitNormal(m_srcPoly[j], m_srcPoly[j + 1]));
        if (node.m_endtype == EndType.etClosedLine || 
          node.m_endtype == EndType.etClosedPolygon)
          m_normals.Add(GetUnitNormal(m_srcPoly[len - 1], m_srcPoly[0]));
        else
          m_normals.Add(new Vector2(m_normals[len - 2].x, m_normals[len - 2].y));

        if (node.m_endtype == EndType.etClosedPolygon)
        {
          int k = len - 1;
          for (int j = 0; j < len; j++)
            OffsetPoint(j, ref k, node.m_jointype);
          m_destPolys.Add(m_destPoly);
        }
        else if (node.m_endtype == EndType.etClosedLine)
        {
          int k = len - 1;
          for (int j = 0; j < len; j++)
            OffsetPoint(j, ref k, node.m_jointype);
          m_destPolys.Add(m_destPoly);
          m_destPoly = new List<Vector2>();
          //re-build m_normals ...
          Vector2 n = m_normals[len - 1];
          for (int j = len - 1; j > 0; j--)
            m_normals[j] = new Vector2(-m_normals[j - 1].x, -m_normals[j - 1].y);
          m_normals[0] = new Vector2(-n.x, -n.y);
          k = 0;
          for (int j = len - 1; j >= 0; j--)
            OffsetPoint(j, ref k, node.m_jointype);
          m_destPolys.Add(m_destPoly);
        }
        else
        {
          int k = 0;
          for (int j = 1; j < len - 1; ++j)
            OffsetPoint(j, ref k, node.m_jointype);

          Vector2 pt1;
          if (node.m_endtype == EndType.etOpenButt)
          {
            int j = len - 1;
            pt1 = new Vector2((float)Round(m_srcPoly[j].x + m_normals[j].x *
              delta), (float)Round(m_srcPoly[j].y + m_normals[j].y * delta));
            m_destPoly.Add(pt1);
            pt1 = new Vector2((float)Round(m_srcPoly[j].x - m_normals[j].x *
              delta), (float)Round(m_srcPoly[j].y - m_normals[j].y * delta));
            m_destPoly.Add(pt1);
          }
          else
          {
            int j = len - 1;
            k = len - 2;
            m_sinA = 0;
            m_normals[j] = new Vector2(-m_normals[j].x, -m_normals[j].y);
            if (node.m_endtype == EndType.etOpenSquare)
              DoSquare(j, k);
            else
              DoRound(j, k);
          }

          //re-build m_normals ...
          for (int j = len - 1; j > 0; j--)
            m_normals[j] = new Vector2(-m_normals[j - 1].x, -m_normals[j - 1].y);

          m_normals[0] = new Vector2(-m_normals[1].x, -m_normals[1].y);

          k = len - 1;
          for (int j = k - 1; j > 0; --j)
            OffsetPoint(j, ref k, node.m_jointype);

          if (node.m_endtype == EndType.etOpenButt)
          {
            pt1 = new Vector2((float)Round(m_srcPoly[0].x - m_normals[0].x * delta),
              (float)Round(m_srcPoly[0].y - m_normals[0].y * delta));
            m_destPoly.Add(pt1);
            pt1 = new Vector2((float)Round(m_srcPoly[0].x + m_normals[0].x * delta),
              (float)Round(m_srcPoly[0].y + m_normals[0].y * delta));
            m_destPoly.Add(pt1);
          }
          else
          {
            k = 1;
            m_sinA = 0;
            if (node.m_endtype == EndType.etOpenSquare)
              DoSquare(0, 1);
            else
              DoRound(0, 1);
          }
          m_destPolys.Add(m_destPoly);
        }
      }
    }
    //------------------------------------------------------------------------------

    public void Execute(ref List<List<Vector2>> solution, double delta)
    {
      solution.Clear();
      FixOrientations();
      DoOffset(delta);
      //now clean up 'corners' ...
      Clipper clpr = new Clipper();
      clpr.AddPaths(m_destPolys, PolyType.ptSubject, true);
      if (delta > 0)
      {
        clpr.Execute(ClipType.ctUnion, solution,
          PolyFillType.pftPositive, PolyFillType.pftPositive);
      }
      else
      {
        Rect r = Clipper.GetBounds(m_destPolys);
        List<Vector2> outer = new List<Vector2>(4);

        outer.Add(new Vector2(r.xMin - 10, r.yMax + 10));
        outer.Add(new Vector2(r.xMax + 10, r.yMax + 10));
        outer.Add(new Vector2(r.xMax + 10, r.yMin - 10));
        outer.Add(new Vector2(r.xMin - 10, r.yMin - 10));

        clpr.AddPath(outer, PolyType.ptSubject, true);
        clpr.ReverseSolution = true;
        clpr.Execute(ClipType.ctUnion, solution, PolyFillType.pftNegative, PolyFillType.pftNegative);
        if (solution.Count > 0) solution.RemoveAt(0);
      }
    }
    //------------------------------------------------------------------------------

    public void Execute(ref PolyTree solution, double delta)
    {
      solution.Clear();
      FixOrientations();
      DoOffset(delta);

      //now clean up 'corners' ...
      Clipper clpr = new Clipper();
      clpr.AddPaths(m_destPolys, PolyType.ptSubject, true);
      if (delta > 0)
      {
        clpr.Execute(ClipType.ctUnion, solution,
          PolyFillType.pftPositive, PolyFillType.pftPositive);
      }
      else
      {
        Rect r = Clipper.GetBounds(m_destPolys);
        List<Vector2> outer = new List<Vector2>(4);

        outer.Add(new Vector2(r.xMin - 10, r.yMax + 10));
        outer.Add(new Vector2(r.xMax + 10, r.yMax + 10));
        outer.Add(new Vector2(r.xMax + 10, r.yMin - 10));
        outer.Add(new Vector2(r.xMin - 10, r.yMin - 10));

        clpr.AddPath(outer, PolyType.ptSubject, true);
        clpr.ReverseSolution = true;
        clpr.Execute(ClipType.ctUnion, solution, PolyFillType.pftNegative, PolyFillType.pftNegative);
        //remove the outer PolyNode rectangle ...
        if (solution.ChildCount == 1 && solution.Childs[0].ChildCount > 0)
        {
          PolyNode outerNode = solution.Childs[0];
          solution.Childs.Capacity = outerNode.ChildCount;
          solution.Childs[0] = outerNode.Childs[0];
          solution.Childs[0].m_Parent = solution;
          for (int i = 1; i < outerNode.ChildCount; i++)
            solution.AddChild(outerNode.Childs[i]);
        }
        else
          solution.Clear();
      }
    }
    //------------------------------------------------------------------------------

    void OffsetPoint(int j, ref int k, JoinType jointype)
    {
      //cross product ...
      m_sinA = (m_normals[k].x * m_normals[j].y - m_normals[j].x * m_normals[k].y);

      if (Math.Abs(m_sinA * m_delta) < 1.0) 
      {
        //dot product ...
        double cosA = (m_normals[k].x * m_normals[j].x + m_normals[j].y * m_normals[k].y); 
        if (cosA > 0) // angle ==> 0 degrees
        {
          m_destPoly.Add(new Vector2(Round(m_srcPoly[j].x + m_normals[k].x * m_delta),
            Round(m_srcPoly[j].y + m_normals[k].y * m_delta)));
          return; 
        }
        //else angle ==> 180 degrees   
      }
      else if (m_sinA > 1.0) m_sinA = 1.0;
      else if (m_sinA < -1.0) m_sinA = -1.0;
      
      if (m_sinA * m_delta < 0)
      {
        m_destPoly.Add(new Vector2(Round(m_srcPoly[j].x + m_normals[k].x * m_delta),
          Round(m_srcPoly[j].y + m_normals[k].y * m_delta)));
        m_destPoly.Add(m_srcPoly[j]);
        m_destPoly.Add(new Vector2(Round(m_srcPoly[j].x + m_normals[j].x * m_delta),
          Round(m_srcPoly[j].y + m_normals[j].y * m_delta)));
      }
      else
        switch (jointype)
        {
          case JoinType.jtMiter:
            {
              double r = 1 + (m_normals[j].x * m_normals[k].x +
                m_normals[j].y * m_normals[k].y);
              if (r >= m_miterLim) DoMiter(j, k, r); else DoSquare(j, k);
              break;
            }
          case JoinType.jtSquare: DoSquare(j, k); break;
          case JoinType.jtRound: DoRound(j, k); break;
        }
      k = j;
    }
    //------------------------------------------------------------------------------

    internal void DoSquare(int j, int k)
    {
      double dx = Math.Tan(Math.Atan2(m_sinA,
          m_normals[k].x * m_normals[j].x + m_normals[k].y * m_normals[j].y) / 4);
      m_destPoly.Add(new Vector2(
          Round(m_srcPoly[j].x + m_delta * (m_normals[k].x - m_normals[k].y * dx)),
          Round(m_srcPoly[j].y + m_delta * (m_normals[k].y + m_normals[k].x * dx))));
      m_destPoly.Add(new Vector2(
          Round(m_srcPoly[j].x + m_delta * (m_normals[j].x + m_normals[j].y * dx)),
          Round(m_srcPoly[j].y + m_delta * (m_normals[j].y - m_normals[j].x * dx))));
    }
    //------------------------------------------------------------------------------

    internal void DoMiter(int j, int k, double r)
    {
      double q = m_delta / r;
      m_destPoly.Add(new Vector2(Round(m_srcPoly[j].x + (m_normals[k].x + m_normals[j].x) * q),
          Round(m_srcPoly[j].y + (m_normals[k].y + m_normals[j].y) * q)));
    }
    //------------------------------------------------------------------------------

    internal void DoRound(int j, int k)
    {
      double a = Math.Atan2(m_sinA,
      m_normals[k].x * m_normals[j].x + m_normals[k].y * m_normals[j].y);
      int steps = Math.Max((int)Round(m_StepsPerRad * Math.Abs(a)),1);

      double X = m_normals[k].x, Y = m_normals[k].y, X2;
      for (int i = 0; i < steps; ++i)
      {
        m_destPoly.Add(new Vector2(
            Round(m_srcPoly[j].x + X * m_delta),
            Round(m_srcPoly[j].y + Y * m_delta)));
        X2 = X;
        X = X * m_cos - m_sin * Y;
        Y = X2 * m_sin + Y * m_cos;
      }
      m_destPoly.Add(new Vector2(
      Round(m_srcPoly[j].x + m_normals[j].x * m_delta),
      Round(m_srcPoly[j].y + m_normals[j].y * m_delta)));
    }
    //------------------------------------------------------------------------------
  }

  class ClipperException : Exception
  {
      public ClipperException(string description) : base(description){}
  }
  //------------------------------------------------------------------------------

} //end ClipperLib namespace
