﻿using GraphX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using GraphX.Models;
using System.Windows.Input;
using ShowcaseExample.Models;
using GraphX.GraphSharp.Algorithms.OverlapRemoval;
using System.Windows.Media.Imaging;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Markup;
using GraphX.Controls;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace ShowcaseExample
{
    public enum RoutedCommands
    {
        EdgeDrag,
        VertexDragDrop
    }

    public partial class MainWindow
    {
        private static MainWindow mw = null;
        public static MainWindow MW()
        {
            if (mw == null)
                mw = new MainWindow();
            return mw;
        }

        public void RouteCommand(VertexControl vc, RoutedCommands rc)
        {
            GraphAreaExample parent = (GraphAreaExample) vc.Parent;
            if (parent == null || parent == bg_Area)
                return;

            switch (rc)
            {
                case RoutedCommands.EdgeDrag:
                    if (_isInEDMode)
                        return;
                    StartEdgeDragging(vc);
                    return;
                case RoutedCommands.VertexDragDrop:
                    StartVertexDragDrop(vc);
                    return;
            }
        }

        private void ThemedGraph_Constructor()
        {
            SetupForeground();
            SetupBackground();

            tg_zoomctrl.SetValue(BackgroundProperty, null);
            //tg_zoomctrl.IsMouseWheelEnabled = false;
            SetForegroundSyncToBackground();

            // related highlighting option
            tg_highlightStrategy.ItemsSource = Enum.GetValues(typeof(HighlightStrategy)).Cast<HighlightStrategy>();
            tg_highlightStrategy.SelectedItem = HighlightStrategy.UseExistingControls;
            tg_highlightType.ItemsSource = Enum.GetValues(typeof(GraphControlType)).Cast<GraphControlType>();
            tg_highlightType.SelectedItem = GraphControlType.VertexAndEdge;
            tg_highlightEdgeType.ItemsSource = Enum.GetValues(typeof(EdgesType)).Cast<EdgesType>();
            tg_highlightEdgeType.SelectedItem = EdgesType.All;
            tg_highlightEnabled_Checked(null, null);

            TGRegisterCommands();
        }

        private void SetupForeground()
        {
            var tg_Logic = new LogicCoreExample();
            tg_Logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog;
            tg_Logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            tg_Logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            tg_Logic.EdgeCurvingEnabled = true;
            tg_Logic.AsyncAlgorithmCompute = false;

            tg_Logic.Graph = new GraphExample();

            tg_Area.LogicCore = tg_Logic;

            //tg_Area.MoveAnimation = AnimationFactory.CreateMoveAnimation(MoveAnimation.Move, TimeSpan.FromSeconds(0.5));
            //tg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade, TimeSpan.FromSeconds(0.3));
            //tg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.Scale);

            tg_Area.VertexSelected += tg_Area_VertexSelected;

            //edge dragging
            tg_Area.VertexMouseMove += tg_Area_VertexMouseMove;
            bg_zoomctrl.PreviewMouseMove += tg_Area_MouseMove;
            tg_zoomctrl.MouseDown += tg_zoomctrl_MouseDown;
            //tg_Area.RelayoutFinished += tg_Area_RelayoutFinished;

            ZoomControl.SetViewFinderVisibility(tg_zoomctrl, System.Windows.Visibility.Visible);
            tg_zoomctrl.Zoom = 1;

            // Animation
            tg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade);
            tg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.None);
        }

        private void SetupBackground()
        {
            var bg_Logic = new LogicCoreExample();
            bg_Logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog;
            bg_Logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            bg_Logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            bg_Logic.EdgeCurvingEnabled = true;
            bg_Logic.AsyncAlgorithmCompute = false;

            bg_Logic.Graph = new GraphExample();

            bg_Area.LogicCore = bg_Logic;

            //bg_Area.MoveAnimation = AnimationFactory.CreateMoveAnimation(MoveAnimation.Move, TimeSpan.FromSeconds(0.5));
            //bg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade, TimeSpan.FromSeconds(0.3));
            //bg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.Scale);

            //bg_Area.VertexSelected += tg_Area_VertexSelected;
            //bg_Area.VertexMouseMove += tg_Area_VertexMouseMove;
            //bg_zoomctrl.PreviewMouseMove += tg_Area_MouseMove;
            //bg_zoomctrl.MouseDown += tg_zoomctrl_MouseDown;
            //bg_Area.RelayoutFinished += tg_Area_RelayoutFinished;

            ZoomControl.SetViewFinderVisibility(bg_zoomctrl, System.Windows.Visibility.Visible);
            bg_zoomctrl.Zoom = 1;

            // Animation
            bg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade);
            bg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.None);
        }

        #region ZoomControl Sync

        private void SetForegroundSyncToBackground()
        {
            tg_zoomctrl.PropertyChanged -= tg_zoomctrl_PropertyChanged;
            bg_zoomctrl.PropertyChanged += bg_zoomctrl_PropertyChanged;
            tg_zoomctrl.IsAnimationDisabled = true;
            bg_zoomctrl.IsAnimationDisabled = false;
        }

        private void bg_zoomctrl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            tg_zoomctrl.Zoom = bg_zoomctrl.Zoom;
            tg_zoomctrl.TranslateX = bg_zoomctrl.TranslateX;
            tg_zoomctrl.TranslateY = bg_zoomctrl.TranslateY;
        }

        private void SetBackgroundSyncToForeground()
        {
            bg_zoomctrl.PropertyChanged -= bg_zoomctrl_PropertyChanged;
            tg_zoomctrl.PropertyChanged += tg_zoomctrl_PropertyChanged;
            bg_zoomctrl.IsAnimationDisabled = true;
            tg_zoomctrl.IsAnimationDisabled = false;
        }
        private void tg_zoomctrl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            bg_zoomctrl.Zoom = tg_zoomctrl.Zoom;
            bg_zoomctrl.TranslateX = tg_zoomctrl.TranslateX;
            bg_zoomctrl.TranslateY = tg_zoomctrl.TranslateY;
        }

        #endregion

        #region Commands

        #region TGRelayoutCommand
        private bool TGRelayoutCommandCanExecute(object sender)
        {
            return true; // tg_Area.Graph != null && tg_Area.VertexList.Count > 0;
        }

        private void TGRelayoutCommandExecute(object sender)
        {
            if (tg_Area.LogicCore.AsyncAlgorithmCompute)
                tg_loader.Visibility = System.Windows.Visibility.Visible;

            tg_Area.RelayoutGraph(true);
            /*if (tg_edgeMode.SelectedIndex == 0 && tg_Area.EdgesList.Count == 0)
                tg_Area.GenerateAllEdges();*/
        }
        #endregion

        void TGRegisterCommands()
        {
            tg_but_relayout.Command = new SimpleCommand(TGRelayoutCommandCanExecute, TGRelayoutCommandExecute);
        }
        #endregion

        void tg_Area_VertexSelected(object sender, VertexSelectedEventArgs args)
        {
            if (_isInEDMode)
            {
                if (_edVertex == args.VertexControl)
                    return;

                var data = new DataEdge(_edVertex.Vertex as DataVertex, args.VertexControl.Vertex as DataVertex);
                tg_Area.LogicCore.Graph.AddEdge(data);
                var ec = new EdgeControl(_edVertex, args.VertexControl, data) { DataContext = data };
                tg_Area.InsertEdge(data, ec);

                _isInEDMode = false;
                clearEdgeDrawing();
                return;
            }

            if (args.MouseArgs.RightButton == MouseButtonState.Pressed)
            {
                args.VertexControl.ContextMenu = new System.Windows.Controls.ContextMenu();
                var menuitem = new System.Windows.Controls.MenuItem() { Header = "Delete item", Tag = args.VertexControl };
                menuitem.Click += tg_deleteitem_Click;
                args.VertexControl.ContextMenu.Items.Add(menuitem);

                var str = new StringBuilder();
                using (var writer = new StringWriter(str))
                    XamlWriter.Save(args.VertexControl.ContextMenu.Template, writer);
                Debug.Write(str);
            }
        }

        void tg_deleteitem_Click(object sender, RoutedEventArgs e)
        {
            var vc = (sender as System.Windows.Controls.MenuItem).Tag as VertexControl;
            if (vc != null)
            {
                foreach (var item in tg_Area.GetRelatedControls(vc, GraphControlType.Edge, EdgesType.All))
                {
                    var ec = item as EdgeControl;
                    tg_Area.LogicCore.Graph.RemoveEdge(ec.Edge as DataEdge);
                    tg_Area.RemoveEdge(ec.Edge as DataEdge);
                }
                tg_Area.RemoveVertex(vc.Vertex as DataVertex);
                tg_Area.LogicCore.Graph.RemoveVertex(vc.Vertex as DataVertex);
            }
        }

        private void tg_but_randomgraph_Click(object sender, RoutedEventArgs e)
        {
            addVertex(Rand.Next(0, 200), Rand.Next(0, 200));
        }

        #region Manual edge drawing

        private bool _isInEDMode = false;
        private PathGeometry _edGeo;
        private VertexControl _edVertex;
        private EdgeControl _edEdge;
        private DataVertex _edFakeDV;

        private void StartEdgeDragging(VertexControl vc)
        {
            if (_isInEDMode)
                return;

            _edVertex = vc;
            Point startPoint = tg_zoomctrl.TranslatePoint(Mouse.GetPosition(tg_zoomctrl), tg_Area);
            Point pos = new Point(startPoint.X + 2, startPoint.Y + 2);
            _edFakeDV = new DataVertex() { ID = Guid.NewGuid() };
            _edGeo = new PathGeometry(new PathFigureCollection() { 
                                        new PathFigure() 
                                        { 
                                            IsClosed = false, 
                                            StartPoint = startPoint, 
                                            Segments = new PathSegmentCollection() { new PolyLineSegment(new List<Point>() { pos }, true) }
                                        } });
            var dedge = new DataEdge(_edVertex.Vertex as DataVertex, _edFakeDV);
            _edEdge = new EdgeControl(_edVertex, null, dedge) { ManualDrawing = true };
            tg_Area.AddEdge(dedge, _edEdge);
            tg_Area.LogicCore.Graph.AddVertex(_edFakeDV);
            tg_Area.LogicCore.Graph.AddEdge(dedge);
            _edEdge.SetEdgePathManually(_edGeo);
            _isInEDMode = true;
        }

        void tg_Area_VertexMouseMove(object sender, VertexMovedEventArgs e)
        {
            if (_isInEDMode && _edGeo != null && _edEdge != null && _edVertex != null
                && _edVertex != e.VertexControl)
            {
                VertexControl vc = e.VertexControl;
                var pos = vc.GetPosition();
                if (vc.ActualWidth > 0)
                {
                    pos.X += vc.ActualWidth / 2;
                    pos.Y += vc.ActualHeight / 2;
                }
                var lastseg = _edGeo.Figures[0].Segments[_edGeo.Figures[0].Segments.Count - 1] as PolyLineSegment;
                lastseg.Points[lastseg.Points.Count - 1] = pos;
                _edEdge.SetEdgePathManually(_edGeo);
            }
        }

        void tg_zoomctrl_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isInEDMode && _edGeo != null && _edEdge != null && _edVertex != null && e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                //place point
                var pos = tg_zoomctrl.TranslatePoint(e.GetPosition(tg_zoomctrl), tg_Area);
                pos.X += 2;
                pos.Y += 2;
                var lastseg = _edGeo.Figures[0].Segments[_edGeo.Figures[0].Segments.Count - 1] as PolyLineSegment;
                lastseg.Points.Add(pos);
                _edEdge.SetEdgePathManually(_edGeo);
            }
        }

        void tg_Area_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isInEDMode && _edGeo != null && _edEdge != null && _edVertex != null)
            {
                var pos = tg_zoomctrl.TranslatePoint(e.GetPosition(tg_zoomctrl), tg_Area);
                pos.X += 2;
                pos.Y += 2;
                var lastseg = _edGeo.Figures[0].Segments[_edGeo.Figures[0].Segments.Count - 1] as PolyLineSegment;
                lastseg.Points[lastseg.Points.Count - 1] = pos;
                _edEdge.SetEdgePathManually(_edGeo);
            }
        }

        void clearEdgeDrawing()
        {
            _edGeo = null;

            if (_edFakeDV != null)
                tg_Area.LogicCore.Graph.RemoveVertex(_edFakeDV);
            _edFakeDV = null;

            _edVertex = null;

            DataEdge dedge = _edEdge.Edge as DataEdge;
            if (dedge != null)
            {
                tg_Area.RemoveEdge(dedge);
                tg_Area.LogicCore.Graph.RemoveEdge(dedge);
            }
            _edEdge = null;
        }

        #endregion

        private void StartVertexDragDrop(VertexControl vc)
        {
            DragDrop.DoDragDrop(vc, vc.Vertex, DragDropEffects.Link);
        }

        //void tg_Area_RelayoutFinished(object sender, EventArgs e)
        //{
        //    if (tg_Area.LogicCore.AsyncAlgorithmCompute)
        //        tg_loader.Visibility = System.Windows.Visibility.Collapsed;

        //    tg_zoomctrl.ZoomToFill();
        //}

        private void tg_highlightStrategy_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (var item in tg_Area.VertexList)
                HighlightBehaviour.SetHighlightStrategy(item.Value, (HighlightStrategy)tg_highlightStrategy.SelectedItem);
            foreach (var item in tg_Area.EdgesList)
                HighlightBehaviour.SetHighlightStrategy(item.Value, (HighlightStrategy)tg_highlightStrategy.SelectedItem);
        }

        private void tg_highlightType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (var item in tg_Area.VertexList)
                HighlightBehaviour.SetHighlightControl(item.Value, (GraphControlType)tg_highlightType.SelectedItem);
            foreach (var item in tg_Area.EdgesList)
                HighlightBehaviour.SetHighlightControl(item.Value, (GraphControlType)tg_highlightType.SelectedItem);
        }

        private void tg_highlightEnabled_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in tg_Area.VertexList)
                HighlightBehaviour.SetIsHighlightEnabled(item.Value, (bool)tg_highlightEnabled.IsChecked);
            foreach (var item in tg_Area.EdgesList)
                HighlightBehaviour.SetIsHighlightEnabled(item.Value, (bool)tg_highlightEnabled.IsChecked);

            tg_highlightStrategy.IsEnabled = tg_highlightType.IsEnabled = tg_highlightEdgeType.IsEnabled = (bool)tg_highlightEnabled.IsChecked;
        }

        private void tg_highlightEdgeType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            foreach (var item in tg_Area.VertexList)
                HighlightBehaviour.SetHighlightEdges(item.Value, (EdgesType)tg_highlightEdgeType.SelectedItem);
            foreach (var item in tg_Area.EdgesList)
                HighlightBehaviour.SetHighlightEdges(item.Value, (EdgesType)tg_highlightEdgeType.SelectedItem);
        }

        private void bg_zoomctrl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = bg_zoomctrl.TranslatePoint(e.GetPosition(bg_zoomctrl), bg_Area);
                addVertex(pos.X, pos.Y);
            }
        }

        private void tg_zoomctrl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            bg_zoomctrl.RouteMouseWheel(e.GetPosition(bg_zoomctrl), e.Delta);
            e.Handled = true;
        }


        private void addVertex(double x, double y)
        {
            var data = new DataVertex("Vertex " + tg_Area.VertexList.Count() + 1);

            data.Age = Rand.Next(18, 75);
            data.Gender = ThemedDataStorage.Gender[Rand.Next(0, 2)];
            if (data.Gender == ThemedDataStorage.Gender[0])
                data.PersonImage = new BitmapImage(new Uri(@"pack://application:,,,/ShowcaseExample;component/Images/female.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
            else data.PersonImage = new BitmapImage(new Uri(@"pack://application:,,,/ShowcaseExample;component/Images/male.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
            data.Profession = ThemedDataStorage.Professions[Rand.Next(0, ThemedDataStorage.Professions.Count - 1)];
            data.Name = ThemedDataStorage.Names[Rand.Next(0, ThemedDataStorage.Names.Count - 1)];

            tg_Area.LogicCore.Graph.AddVertex(data);

            VertexControl vc = new VertexControl(data);
            tg_Area.AddVertex(data, vc);

            vc.SetPosition(new Point(x, y));
            if (tg_Area.VertexList.Count == 1)
            {
                //tg_zoomctrl.ZoomToFill();
                Button button = bg_zoomctrl.ViewFinder.FindName("FillButton") as Button;
                if (button != null)
                {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
                    IInvokeProvider invokeProv = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invokeProv.Invoke();
                }
            }

            var data1 = new DataVertex("Vertex1111");

            data1.Age = Rand.Next(18, 75);
            data1.Gender = ThemedDataStorage.Gender[Rand.Next(0, 2)];
            if (data1.Gender == ThemedDataStorage.Gender[0])
                data1.PersonImage = new BitmapImage(new Uri(@"pack://application:,,,/ShowcaseExample;component/Images/female.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
            else data1.PersonImage = new BitmapImage(new Uri(@"pack://application:,,,/ShowcaseExample;component/Images/male.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
            data1.Profession = ThemedDataStorage.Professions[Rand.Next(0, ThemedDataStorage.Professions.Count - 1)];
            data1.Name = "TheOne";

            bg_Area.LogicCore.Graph.AddVertex(data1);

            VertexControl vc1 = new VertexControl(data1);
            bg_Area.AddVertex(data1, vc1);

            vc1.SetPosition(new Point(0, 0));
        }
    }
}
