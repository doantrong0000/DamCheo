﻿using System.Windows;
using System.Windows.Controls;

namespace BimSpeedStructureBeamDesign.BeamRebar.View
{
   public static class ScrollViewerEx
   {
      public static readonly DependencyProperty AutoScrollProperty =
          DependencyProperty.RegisterAttached("AutoScrollToEnd",
              typeof(bool), typeof(ScrollViewerEx),
              new PropertyMetadata(false, HookupAutoScrollToEnd));

      public static readonly DependencyProperty AutoScrollHandlerProperty =
          DependencyProperty.RegisterAttached("AutoScrollToEndHandler",
              typeof(ScrollViewerAutoScrollToEndHandler), typeof(ScrollViewerEx));

      private static void HookupAutoScrollToEnd(DependencyObject d,
          DependencyPropertyChangedEventArgs e)
      {
         var scrollViewer = d as ScrollViewer;
         if (scrollViewer == null) return;

         SetAutoScrollToEnd(scrollViewer, (bool)e.NewValue);
      }

      public static bool GetAutoScrollToEnd(ScrollViewer instance)
      {
         return (bool)instance.GetValue(AutoScrollProperty);
      }

      public static void SetAutoScrollToEnd(ScrollViewer instance, bool value)
      {
         var oldHandler = (ScrollViewerAutoScrollToEndHandler)instance.GetValue(AutoScrollHandlerProperty);
         if (oldHandler != null)
         {
            oldHandler.Dispose();
            instance.SetValue(AutoScrollHandlerProperty, null);
         }

         instance.SetValue(AutoScrollProperty, value);
         if (value)
            instance.SetValue(AutoScrollHandlerProperty, new ScrollViewerAutoScrollToEndHandler(instance));
      }
   }

   public class ScrollViewerAutoScrollToEndHandler : DependencyObject, IDisposable
   {
      private readonly ScrollViewer m_scrollViewer;
      private bool m_doScroll = false;

      public ScrollViewerAutoScrollToEndHandler(ScrollViewer scrollViewer)
      {
         if (scrollViewer == null)
         {
            throw new ArgumentNullException("scrollViewer");
         }

         m_scrollViewer = scrollViewer;
         m_scrollViewer.ScrollToEnd();
         m_scrollViewer.ScrollChanged += ScrollChanged;
      }

      private void ScrollChanged(object sender, ScrollChangedEventArgs e)
      {
         // User scroll event : set or unset autoscroll mode
         if (e.ExtentHeightChange == 0)
         {
            m_doScroll = m_scrollViewer.VerticalOffset == m_scrollViewer.ScrollableHeight;
         }

         // Content scroll event : autoscroll eventually
         if (m_doScroll && e.ExtentHeightChange != 0)
         {
            m_scrollViewer.ScrollToVerticalOffset(m_scrollViewer.ExtentHeight);
         }
      }

      public void Dispose()
      {
         m_scrollViewer.ScrollChanged -= ScrollChanged;
      }
   }
}