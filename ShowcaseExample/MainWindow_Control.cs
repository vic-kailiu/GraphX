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
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using ShowcaseExample.Controls;
using ShowcaseExample.Utils;
using Acrobat;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

namespace ShowcaseExample
{
    public enum RoutedCommands
    {
        EdgeDrag,
        VertexDragDrop,
        ChangeTitle,
        ChangeAuthor,
        MergeVertex,
        IncludeVertex,
        ToggleContent,
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

        public void RouteCommand(VertexControl vc, RoutedCommands rc, Object parameter)
        {
            VertexControl paraVC = null;

            if (parameter!=null)    // parse parameter to GUID, then find the vc
            {
                paraVC = tg_Area.GetVertexControl((string)parameter);
            }

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
                case RoutedCommands.ChangeTitle:
                case RoutedCommands.ChangeAuthor:
                    DoChangeText(vc, rc);
                    return;
                case RoutedCommands.MergeVertex:
                    DoMergeVertex(vc, paraVC);
                    return;
                case RoutedCommands.IncludeVertex:
                    DoIncludeVertex(vc, paraVC);
                    return;
                case RoutedCommands.ToggleContent:
                    DoToggleChildVertex(vc);
                    return;
            }
        }

        private void ThemedGraph_Constructor()
        {
            //InitCBViewer();

            var tg_Logic = new LogicCoreExample();
            tg_Area.LogicCore = tg_Logic;

            tg_Logic.DefaultLayoutAlgorithm = LayoutAlgorithmTypeEnum.LinLog;
            tg_Logic.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            tg_Logic.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER;
            tg_Logic.EdgeCurvingEnabled = true;
            tg_Logic.AsyncAlgorithmCompute = false;

            tg_Logic.Graph = new GraphExample();

            //tg_Area.MoveAnimation = AnimationFactory.CreateMoveAnimation(MoveAnimation.Move, TimeSpan.FromSeconds(0.5));
            //tg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade, TimeSpan.FromSeconds(0.3));
            //tg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.Scale);

            tg_highlightStrategy.ItemsSource = Enum.GetValues(typeof(HighlightStrategy)).Cast<HighlightStrategy>();
            tg_highlightStrategy.SelectedItem = HighlightStrategy.UseExistingControls;
            tg_highlightType.ItemsSource = Enum.GetValues(typeof(GraphControlType)).Cast<GraphControlType>();
            tg_highlightType.SelectedItem = GraphControlType.VertexAndEdge;
            tg_highlightEdgeType.ItemsSource = Enum.GetValues(typeof(EdgesType)).Cast<EdgesType>();
            tg_highlightEdgeType.SelectedItem = EdgesType.All;

            tg_highlightEnabled_Checked(null, null);

            tg_Area.VertexSelected += tg_Area_VertexSelected;
            tg_Area.VertexMouseMove += tg_Area_VertexMouseMove;
            tg_zoomctrl.PreviewMouseMove += tg_Area_MouseMove;
            tg_zoomctrl.MouseDown += tg_zoomctrl_MouseDown;
            tg_zoomctrl.AreaSelected += dg_zoomctrl_AreaSelected;
            tg_Area.RelayoutFinished += tg_Area_RelayoutFinished;

            ZoomControl.SetViewFinderVisibility(tg_zoomctrl, System.Windows.Visibility.Visible);
            tg_zoomctrl.Zoom = 1;

            // Animation
            tg_Area.DeleteAnimation = AnimationFactory.CreateDeleteAnimation(DeleteAnimation.Fade);
            tg_Area.MouseOverAnimation = AnimationFactory.CreateMouseOverAnimation(MouseOverAnimation.None);

            TGRegisterCommands();
        }

        #region Clipboard viewer related methods

        /// <summary>
        /// Next clipboard viewer window 
        /// </summary>
        private IntPtr hWndNextViewer;

        /// <summary>
        /// The <see cref="HwndSource"/> for this window.
        /// </summary>
        private HwndSource hWndSource;

        private bool isViewing;

        private void InitCBViewer()
        {
            WindowInteropHelper wih = new WindowInteropHelper(this);
            hWndSource = HwndSource.FromHwnd(wih.Handle);

            hWndSource.AddHook(this.WinProc);   // start processing window messages
            hWndNextViewer = Win32.SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer
            isViewing = true;
        }

        private void CloseCBViewer()
        {
            // remove this window from the clipboard viewer chain
            Win32.ChangeClipboardChain(hWndSource.Handle, hWndNextViewer);

            hWndNextViewer = IntPtr.Zero;
            hWndSource.RemoveHook(this.WinProc);
            isViewing = false;
        }

        private void DrawContent()
        {
            //pnlContent.Children.Clear();

            if (Clipboard.ContainsText())
            {
                // we have some text in the clipboard.
                TextBox tb = new TextBox();
                tb.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                tb.Text = Clipboard.GetText();
                tb.IsReadOnly = true;
                tb.TextWrapping = TextWrapping.NoWrap;
                //pnlContent.Children.Add(tb);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                // we have a file drop list in the clipboard
                ListBox lb = new ListBox();
                lb.ItemsSource = Clipboard.GetFileDropList();
                //pnlContent.Children.Add(lb);
            }
            else if (Clipboard.ContainsImage())
            {
                // Because of a known issue in WPF,
                // we have to use a workaround to get correct
                // image that can be displayed.
                // The image have to be saved to a stream and then 
                // read out to workaround the issue.
                MemoryStream ms = new MemoryStream();
                BmpBitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));
                enc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                BmpBitmapDecoder dec = new BmpBitmapDecoder(ms,
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

                Image img = new Image();
                img.Stretch = Stretch.Uniform;
                img.Source = dec.Frames[0];
                //pnlContent.Children.Add(img);
            }
            else
            {
                Label lb = new Label();
                lb.Content = "The type of the data in the clipboard is not supported by this sample.";
                //pnlContent.Children.Add(lb);
            }
        }

        private IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32.WM_CHANGECBCHAIN:
                    if (wParam == hWndNextViewer)
                    {
                        // clipboard viewer chain changed, need to fix it.
                        hWndNextViewer = lParam;
                    }
                    else if (hWndNextViewer != IntPtr.Zero)
                    {
                        // pass the message to the next viewer.
                        Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    }
                    break;

                case Win32.WM_DRAWCLIPBOARD:
                    // clipboard content changed
                    this.DrawContent();
                    // pass the message to the next viewer.
                    Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }

        #endregion

        #region Commands

        CAcroApp mApp;
        CAcroPDDoc pdDoc;
        CAcroAVDoc avDoc;

        #region TGRelayoutCommand
        private bool TGRelayoutCommandCanExecute(object sender)
        {
            return true; // tg_Area.Graph != null && tg_Area.VertexList.Count > 0;
        }

        private void TGRelayoutCommandExecute(object sender)
        {
            InitCBViewer();
            return;

            CAcroAVPageView pageView = avDoc.GetAVPageView() as CAcroAVPageView;
            if (pageView == null)
                return;

            AcroRectClass rect = new AcroRectClass();
            rect.Top = 100;
            rect.bottom = 200;
            rect.Left = 100;
            rect.right = 300;
            CAcroPDPage page = pdDoc.AcquirePage(pageView.GetPageNum()) as CAcroPDPage;
            CAcroPDAnnot annot = page.AddNewAnnot(-1, "Text", rect) as CAcroPDAnnot;
            int foo = 0;

                //tg_Area.VertexList.Keys.First().ContentVisibility = System.Windows.Visibility.Visible;
            //if (tg_Area.LogicCore.AsyncAlgorithmCompute)
            //    tg_loader.Visibility = System.Windows.Visibility.Visible;

            //tg_Area.RelayoutGraph(true);
            /*if (tg_edgeMode.SelectedIndex == 0 && tg_Area.EdgesList.Count == 0)
                tg_Area.GenerateAllEdges();*/
        }
        #endregion

        void TGRegisterCommands()
        {
            tg_but_relayout.Command = new SimpleCommand(TGRelayoutCommandCanExecute, TGRelayoutCommandExecute);
        }
        
        private void tg_but_randomgraph_Click(object sender, RoutedEventArgs e)
        {
            //Initialize Acrobat by creating App object
            mApp = new AcroAppClass();
            //Show Acrobat
            mApp.Show();
            //set AVDoc object
            avDoc = new AcroAVDocClass();

            //constant, hard coding for a pdf to open, it can be changed when needed.
            String szPdfPathConst = Directory.GetCurrentDirectory() + "\\1.pdf";
            if (avDoc.Open(szPdfPathConst, ""))
            {
                //set the pdDoc object and get some data
                pdDoc = (CAcroPDDoc)avDoc.GetPDDoc();
            }
        }
        
        #endregion

        #region AddVertex

        private void tg_zoomctrl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = tg_zoomctrl.TranslatePoint(e.GetPosition(tg_zoomctrl), tg_Area);
                addVertex(pos.X, pos.Y);
            }
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

            data.layerLevel = 0;

            tg_Area.LogicCore.Graph.AddVertex(data);

            VertexControl vc = new VertexControl(data);
            DragBehaviour.SetIsDragEnabled(vc, true);
            DragBehaviour.SetUpdateEdgesOnMove(vc, true);
            tg_Area.GraphAddVertex(data, vc);

            vc.SetPosition(new Point(x, y));
            if (tg_Area.VertexList.Count == 1)
            {
                //tg_zoomctrl.ZoomToFill();
                Button button = tg_zoomctrl.ViewFinder.FindName("FillButton") as Button;
                if (button != null)
                {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
                    IInvokeProvider invokeProv = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invokeProv.Invoke();
                }
            }

            vc.PositionChanged += vc_PositionChanged;
        }

        #endregion

        #region multi select vertex and dragging

        private List<VertexControl> SelectedVertices = new List<VertexControl>();

        void dg_zoomctrl_AreaSelected(object sender, AreaSelectedEventArgs args)
        {
            var r = args.Rectangle;
            foreach (var item in tg_Area.VertexList)
            {
                var offset = item.Value.GetPosition();
                var irect = new Rect(offset.X, offset.Y, item.Value.ActualWidth, item.Value.ActualHeight);
                if (irect.IntersectsWith(r))
                    SelectVertex(item.Value);
            }
        }

        private void SelectVertex(VertexControl vc)
        {
            if (SelectedVertices.Contains(vc))
            {
                SelectedVertices.Remove(vc);
                HighlightBehaviour.SetHighlighted(vc, false);
                DragBehaviour.SetIsTagged(vc, false);
            }
            else
            {
                SelectedVertices.Add(vc);
                HighlightBehaviour.SetHighlighted(vc, true);
                DragBehaviour.SetIsTagged(vc, true);
            }
        }

        #endregion

        #region Vertex Content Related

        private void DoChangeText(VertexControl vc, RoutedCommands rc)
        {
            InputDialogWindow inputDialog = new InputDialogWindow("Please enter your name:", "John Doe");
            if (inputDialog.ShowDialog() == true)
            {
                switch (rc)
                {
                    case RoutedCommands.ChangeTitle:
                        ((DataVertex)vc.DataContext).Name = inputDialog.Answer;
                        break;
                    case RoutedCommands.ChangeAuthor:
                        ((DataVertex)vc.DataContext).Profession = inputDialog.Answer;
                        break;
                }
            }
        }
                
        #endregion

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
                ////place point
                //var pos = tg_zoomctrl.TranslatePoint(e.GetPosition(tg_zoomctrl), tg_Area);
                //pos.X += 2;
                //pos.Y += 2;
                //var lastseg = _edGeo.Figures[0].Segments[_edGeo.Figures[0].Segments.Count - 1] as PolyLineSegment;
                //lastseg.Points.Add(pos);
                //_edEdge.SetEdgePathManually(_edGeo);

                _isInEDMode = false;
                clearEdgeDrawing();
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

        void tg_Area_VertexSelected_ED(VertexControl vc)
        {
            if (_edVertex == vc)
                return;

            var data = new DataEdge(_edVertex.Vertex as DataVertex, vc.Vertex as DataVertex);
            tg_Area.LogicCore.Graph.AddEdge(data);
            var ec = new EdgeControl(_edVertex, vc, data) { DataContext = data };
            tg_Area.GraphAddEdge(data, ec);

            _isInEDMode = false;
            clearEdgeDrawing();
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

        #region multilayer vertex

        #region DragDrop

        private void StartVertexDragDrop(VertexControl vc)
        {
            DragDrop.DoDragDrop(vc, ((DataVertex)vc.Vertex).ID.ToString(), DragDropEffects.Link);
        }

        // Case for merge with vertex that have a parent already,
        // it should do:
        // Move the pre_folder to higher level
        // Create new vertex on that level
        // Change the parent for merge target
        // Add merge sender to newly created folder vertex
        // update new folder -> parent (pre_folder)
        private void DoMergeVertex(VertexControl vc, VertexControl paraVC)
        {
            DataVertex dv = vc.Vertex as DataVertex;
            DataVertex paraDv = paraVC.Vertex as DataVertex;

            DataVertex pre_Parent = null;
            if (dv.ParentVertex !=null)
            {
                pre_Parent = dv.ParentVertex;
                pre_Parent.ChildVertex.Remove(dv);
                Promote(pre_Parent, pre_Parent.layerLevel + 1);
            }

            // place paraVC to the mouse position
            Point mousePosition;
            //mousePosition = tg_zoomctrl.TranslatePoint(Mouse.GetPosition(tg_zoomctrl), tg_Area);
            // the Mouse.GetPosition won't work correctly, since mouse is captured at that time
            // http://tech.pro/tutorial/893/wpf-snippet-reliably-getting-the-mouse-position
            mousePosition = MouseUtilities.CorrectGetPosition(tg_Area);
            paraVC.SetPosition(mousePosition);

            //create folder node
            var data = new DataVertex("FolderVertex " + tg_Area.VertexList.Count() + 1);

            data.Age = Rand.Next(18, 75);
            data.Gender = ThemedDataStorage.Gender[Rand.Next(0, 2)];
            if (data.Gender == ThemedDataStorage.Gender[0])
                data.PersonImage = new BitmapImage(new Uri(@"pack://application:,,,/ShowcaseExample;component/Images/female.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
            else data.PersonImage = new BitmapImage(new Uri(@"pack://application:,,,/ShowcaseExample;component/Images/male.png", UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
            data.Profession = ThemedDataStorage.Professions[Rand.Next(0, ThemedDataStorage.Professions.Count - 1)];
            data.Name = "BIG Boss";

            data.layerLevel = (dv.layerLevel > paraDv.layerLevel ? dv.layerLevel : paraDv.layerLevel)
                               + 1;

            data.ChildVertex.Add(dv);
            data.ChildVertex.Add(paraDv);
            dv.ParentVertex = data;
            paraDv.ParentVertex = data;

            if (pre_Parent != null)
            {
                data.ParentVertex = pre_Parent;
                pre_Parent.ChildVertex.Add(data);
            }

            tg_Area.LogicCore.Graph.AddVertex(data);

            VertexControl fvc = new VertexControl(data);
            DragBehaviour.SetIsDragEnabled(fvc, true);
            DragBehaviour.SetUpdateEdgesOnMove(fvc, true);
            tg_Area.GraphAddVertex(data, fvc);

            data.ContentVisible = true;
            updateVertexLayout(data, true);
            //IterateUpdateParent(data);

            fvc.SizeChanged += fvc_SizeChanged;
            fvc.PositionChanged += vc_PositionChanged;
        }

        void fvc_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VertexControl vc = sender as VertexControl;
            vc.SizeChanged -= fvc_SizeChanged; // used for the first time only
            IterateUpdateParent(vc.Vertex as DataVertex);
        }

        private void Promote(DataVertex dataVertex, int level)
        {
            int pre_level = dataVertex.layerLevel;
            dataVertex.layerLevel = level;
            tg_Area.MoveLayer(dataVertex, tg_Area.VertexList[dataVertex], pre_level);
        }

        private void DoIncludeVertex(VertexControl vc, VertexControl paraVC)
        {
            DataVertex dv = vc.Vertex as DataVertex;
            DataVertex paraDv = paraVC.Vertex as DataVertex;

            // place paraVC to the mouse position
            Point mousePosition;
            mousePosition = MouseUtilities.CorrectGetPosition(tg_Area);
            paraVC.SetPosition(mousePosition);

            dv.ChildVertex.Add(paraDv);
            paraDv.ParentVertex = dv;

            if (dv.layerLevel <= paraDv.layerLevel)
            {
                Promote(dv, paraDv.layerLevel + 1);
            }

            updateVertexLayout(dv, false);
            IterateUpdateParent(dv);
        }

        private void updateVertexLayout(DataVertex dv, bool reset)
        {
            if (dv.ChildVertex.Count == 0)
                return;

            Rect contentRect = GetContentRect(dv.ChildVertex);
            Rect dvContentRect;

            if (reset)
            {
                dvContentRect = contentRect;
            }
            else
            {
                VertexControl vc = tg_Area.VertexList[dv];
                dvContentRect = new Rect(vc.GetPosition().X,
                                         vc.GetPosition().Y + VisualConfig.ContentVPos,
                                         dv.ContentWidth, dv.ContentHeight);
                if (dvContentRect.Contains(contentRect))
                    return;

                dvContentRect.Union(contentRect);
            }

            tg_Area.VertexList[dv].SetPosition(new Point(dvContentRect.X,
                                                         dvContentRect.Y - VisualConfig.ContentVPos));
            dv.ContentWidth = dvContentRect.Width;
            dv.ContentHeight = dvContentRect.Height;
        }

        private Rect GetContentRect(List<DataVertex> dvlist)
        {
            VertexControl vc0 = tg_Area.VertexList[dvlist[0]];
            Rect contentRect = new Rect(vc0.GetPosition().X, vc0.GetPosition().Y,
                                        vc0.ActualWidth, vc0.ActualHeight);
            foreach (DataVertex child in dvlist)
            {
                VertexControl vc = tg_Area.VertexList[child];

                contentRect.Union(new Rect(vc.GetPosition().X, vc.GetPosition().Y,
                                           vc.ActualWidth, vc.ActualHeight));
            }

            // add margin to the rect
            contentRect.Union(new Point(contentRect.X - VisualConfig.ContentMargin,
                                        contentRect.Y - VisualConfig.ContentMargin));
            contentRect.Union(new Point(contentRect.Right + VisualConfig.ContentMargin,
                                        contentRect.Bottom + VisualConfig.ContentMargin));

            return contentRect;
        }

        #endregion

        private int DraggedFolderLevel = 0;

        #region update child vertex by Tagging them once a parent vertex is selected

        private void tg_Area_VertexSelected_LeftClick(VertexControl vertexControl)
        {
            DataVertex dv = vertexControl.Vertex as DataVertex;
            if (dv.ChildVertex.Count == 0)
                return;

            DraggedFolderLevel = dv.layerLevel;

            DragBehaviour.SetIsTagged(vertexControl, true);

            foreach (DataVertex child in dv.ChildVertex)
            {
                DragBehaviour.SetIsTagged(tg_Area.VertexList[child], true);
                IterateSetTagged(child, true);
            }

            vertexControl.PreviewMouseUp += vertexControl_MouseUp;
        }

        private void vertexControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DataVertex dv = ((VertexControl)sender).Vertex as DataVertex;
            if (dv.ChildVertex.Count == 0)
                return;

            DraggedFolderLevel = 0;

            DragBehaviour.SetIsTagged((VertexControl)sender, false);

            foreach (DataVertex child in dv.ChildVertex)
            {
                DragBehaviour.SetIsTagged(tg_Area.VertexList[child], false);
                IterateSetTagged(child, false);
            }

            ((VertexControl)sender).MouseUp -= vertexControl_MouseUp;
        }

        private void IterateSetTagged(DataVertex dv, bool tagged)
        {
            foreach (DataVertex child in dv.ChildVertex)
            {
                DragBehaviour.SetIsTagged(tg_Area.VertexList[child], tagged);
                IterateSetTagged(child, tagged);
            }
        }

        #endregion

        #region used for update parent vertex
        void vc_PositionChanged(object sender, VertexPositionEventArgs args)
        {
            DataVertex dv= ((VertexControl)sender).Vertex as DataVertex;

            if (DraggedFolderLevel > dv.layerLevel)
                return;

            // update parent
            if (dv.ParentVertex !=null )
            {
                VertexControl parentVC = tg_Area.VertexList[dv.ParentVertex];
                updateVertexLayout(dv.ParentVertex, false);
                IterateUpdateParent(dv.ParentVertex);
            }
        }

        void IterateUpdateParent(DataVertex dv)
        {
            if (dv.ParentVertex != null)
            {
                VertexControl parentVC = tg_Area.VertexList[dv.ParentVertex];
                updateVertexLayout(dv.ParentVertex, false);
                IterateUpdateParent(dv.ParentVertex);
            }
        }

        #endregion

        private List<DataVertex> involvedVertex = new List<DataVertex>();

        private void DoToggleChildVertex(VertexControl vc)
        {
            DataVertex dv = vc.Vertex as DataVertex;

            foreach(DataVertex child in dv.ChildVertex)
            {
                VertexControl childVisual = tg_Area.VertexList[child];
                childVisual.Visibility = dv.ContentVisible ? Visibility.Visible : Visibility.Hidden;
                involvedVertex.Add(child);

                IterateTogglChildVertex(child, dv.ContentVisible);
            }

            foreach (var edge in tg_Area.EdgesList)
            {
                DataVertex relatedVertex = null;
                if (involvedVertex.Contains(edge.Key.Source))
                    relatedVertex = edge.Key.Source;
                else if (involvedVertex.Contains(edge.Key.Target))
                    relatedVertex = edge.Key.Target;

                if (relatedVertex != null)
                    edge.Value.Visibility = tg_Area.VertexList[relatedVertex].Visibility;
            }

        }

        private void IterateTogglChildVertex(DataVertex dv, bool show)
        {
            foreach (DataVertex child in dv.ChildVertex)
            {
                VertexControl childVisual = tg_Area.VertexList[child];
                // shows only if the both command(show, !show) and the contentvisible property of the vertex itself are true
                childVisual.Visibility = (dv.ContentVisible && show) ? Visibility.Visible : Visibility.Hidden;
                involvedVertex.Add(child);
                IterateTogglChildVertex(child, show);
            }
        }

        #endregion

        #region Template Remaining

        void tg_Area_VertexSelected(object sender, VertexSelectedEventArgs args)
        {
            if (_isInEDMode)
            {
                tg_Area_VertexSelected_ED(args.VertexControl);
                return;
            }

            if (args.MouseArgs.LeftButton == MouseButtonState.Pressed)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    SelectVertex(args.VertexControl);
                else
                tg_Area_VertexSelected_LeftClick(args.VertexControl);
            }

            if (args.MouseArgs.RightButton == MouseButtonState.Pressed)
            {
                tg_Area_VertexSelected_RighClick(args.VertexControl);
            }
        }

        void tg_Area_VertexSelected_RighClick(VertexControl vc)
        {
            vc.ContextMenu = new System.Windows.Controls.ContextMenu();
            var menuitem = new System.Windows.Controls.MenuItem() { Header = "Delete item", Tag = vc };
            menuitem.Click += tg_deleteitem_Click;
            vc.ContextMenu.Items.Add(menuitem);

            var str = new StringBuilder();
            using (var writer = new StringWriter(str))
                XamlWriter.Save(vc.ContextMenu.Template, writer);
            Debug.Write(str);
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

        void tg_Area_RelayoutFinished(object sender, EventArgs e)
        {
            if (tg_Area.LogicCore.AsyncAlgorithmCompute)
                tg_loader.Visibility = System.Windows.Visibility.Collapsed;

            tg_zoomctrl.ZoomToFill();
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

        #endregion
    }
}