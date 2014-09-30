using GraphX;
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

namespace ShowcaseExample
{
    public partial class MainWindow
    {
        private void ThemedGraph_Constructor()
        {
            var tg_Logic = new LogicCoreExample();
            tg_Area.LogicCore = tg_Logic;

            tg_Logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog;
            tg_Logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            //tg_Logic.DefaultOverlapRemovalAlgorithmParams = tg_Logic.AlgorithmFactory.CreateOverlapRemovalParameters(OverlapRemovalAlgorithmTypeEnum.FSA);
            //(tg_Logic.DefaultOverlapRemovalAlgorithmParams as OverlapRemovalParameters).HorizontalGap = 150;
            //(tg_Logic.DefaultOverlapRemovalAlgorithmParams as OverlapRemovalParameters).VerticalGap = 150;
            tg_Logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            tg_Logic.EdgeCurvingEnabled = true;
            tg_Logic.AsyncAlgorithmCompute = true;

            tg_Logic.Graph = new GraphExample();

            //tg_Area.MoveAnimation = AnimationFactory.CreateMoveAnimation(MoveAnimation.Move, TimeSpan.FromSeconds(0.5));
            //dg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade, TimeSpan.FromSeconds(0.3));
            //dg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.Scale);

            tg_highlightStrategy.ItemsSource = Enum.GetValues(typeof(HighlightStrategy)).Cast<HighlightStrategy>();
            tg_highlightStrategy.SelectedItem = HighlightStrategy.UseExistingControls;
            tg_highlightType.ItemsSource = Enum.GetValues(typeof(GraphControlType)).Cast<GraphControlType>();
            tg_highlightType.SelectedItem = GraphControlType.VertexAndEdge;
            tg_highlightEdgeType.ItemsSource = Enum.GetValues(typeof(EdgesType)).Cast<EdgesType>();
            tg_highlightEdgeType.SelectedItem = EdgesType.All;

            tg_highlightEnabled_Checked(null, null);

            tg_Area.VertexSelected += tg_Area_VertexSelected;
            tg_Area.GenerateGraphFinished += tg_Area_GenerateGraphFinished;
            tg_Area.RelayoutFinished += tg_Area_RelayoutFinished;
            //tg_dragMoveEdges.Checked += tg_dragMoveEdges_Checked;
            //tg_dragMoveEdges.Unchecked += tg_dragMoveEdges_Checked;

            ZoomControl.SetViewFinderVisibility(tg_zoomctrl, System.Windows.Visibility.Visible);

            // Animation
            tg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade);
            tg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.None);

            TGRegisterCommands();
        }


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
            DragBehaviour.SetIsDragEnabled(vc, true);
            DragBehaviour.SetUpdateEdgesOnMove(vc, true);
            tg_Area.AddVertex(data, vc);

            tg_Area.RelayoutGraph(true);

            if (tg_Area.LogicCore.AsyncAlgorithmCompute)
                tg_loader.Visibility = System.Windows.Visibility.Visible;

            tg_zoomctrl.ZoomToFill();
        }

        void tg_Area_RelayoutFinished(object sender, EventArgs e)
        {
            if (tg_Area.LogicCore.AsyncAlgorithmCompute)
                tg_loader.Visibility = System.Windows.Visibility.Collapsed;

            tg_zoomctrl.ZoomToFill();
        }

        void tg_Area_GenerateGraphFinished(object sender, EventArgs e)
        {
            if (tg_Area.LogicCore.AsyncAlgorithmCompute)
                tg_loader.Visibility = System.Windows.Visibility.Collapsed;

            tg_highlightStrategy_SelectionChanged(null, null);
            tg_highlightType_SelectionChanged(null, null);
            tg_highlightEnabled_Checked(null, null);
            tg_highlightEdgeType_SelectionChanged(null, null);

            foreach (var item in tg_Area.VertexList)
            {
                DragBehaviour.SetIsDragEnabled(item.Value, true);
                DragBehaviour.SetUpdateEdgesOnMove(item.Value, true);
            }

            tg_Area.SetEdgesDashStyle(EdgeDashStyle.Dash);
            tg_zoomctrl.ZoomToFill();// ZoomToFill(); //manually update zoom control to fill the area
        }

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
    }
}
