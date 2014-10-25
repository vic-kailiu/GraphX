using GraphX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using YAXLib;

namespace ShowcaseExample
{
    public class DataVertex: VertexBase, INotifyPropertyChanged
    {
        private string name = "";
        private string profession = "";
        private double contentWidth = 300;
        private double contentHeight = 100;
        private bool contentVisible = false;

        public DateTime DateTimeValue;
        public string Text { get; set; }
        public string Name 
        { 
            get
            {
                return this.name;

            }
            set
            {
                if (this.name!=value)
                {
                    this.name = value;
                    OnPropertyChanged("Name");
                }
            }
        }
        public string Profession 
        {
            get
            {
                return this.profession;
            }
            set
            {
                if (this.profession!=value)
                {
                    this.profession = value;
                    OnPropertyChanged("Profession");
                }
            }
        }
        public string Gender { get; set; }
        public int Age { get; set; }
        public int layerLevel { get; set; }

        [YAXDontSerialize]
        public double ContentWidth
        {
            get
            {
                return this.contentWidth;
            }
            set
            {
                if (this.contentWidth != value)
                {
                    this.contentWidth = value;
                    OnPropertyChanged("ContentWidth");
                }
            }
        }

        [YAXDontSerialize]
        public double ContentHeight
        {
            get
            {
                return this.contentHeight;
            }
            set
            {
                if (this.contentHeight != value)
                {
                    this.contentHeight = value;
                    OnPropertyChanged("ContentHeight");
                }
            }
        }

        [YAXDontSerialize]
        public bool ContentVisible
        {
            get
            { 
                return this.contentVisible;
            }
            set
            {
                if (this.contentVisible != value)
                {
                    this.contentVisible = value;
                    OnPropertyChanged("ContentVisible");
                }
            }
        }

        [YAXDontSerialize]
        public ImageSource DataImage { get; set; }

        [YAXDontSerialize]
        public ImageSource PersonImage { get; set; }

        #region Calculated or static props
        [YAXDontSerialize]
        public DataVertex Self
        {
            get { return this; }
        }

        public override string ToString()
        {
            return Text;
        }

        private string[] imgArray = new string[4]
        {
            @"pack://application:,,,/GraphX.Controls;component/Images/help_black.png",
            @"pack://application:,,,/ShowcaseExample;component/Images/skull_bw.png",
            @"pack://application:,,,/ShowcaseExample;component/Images/wrld.png",
            @"pack://application:,,,/ShowcaseExample;component/Images/birdy.png",
        };
        private string[] textArray = new string[4]
        {
            @"",
            @"Skully",
            @"Worldy",
            @"Birdy",
        };

        #endregion

        /// <summary>
        /// Default constructor for this class
        /// (required for serialization).
        /// </summary>
        public DataVertex():this("")
        {
        }

        private static readonly Random Rand = new Random();

        public DataVertex(string text = "")
        {
            var num = Rand.Next(0, 3);
            if (string.IsNullOrEmpty(text)) Text = num == 0 ? text : textArray[num];
            else Text = text;
            DataImage = new BitmapImage(new Uri(imgArray[num], UriKind.Absolute)) { CacheOption = BitmapCacheOption.OnLoad };
        }

        private List<DataVertex> childVertex = new List<DataVertex>();
        private DataVertex parentVertex;

        public List<DataVertex> ChildVertex
        {
            get { return this.childVertex; }
        }

        public DataVertex ParentVertex
        {
            get { return this.parentVertex; }
            set
            {
                if (this.parentVertex != value)
                    this.parentVertex = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
