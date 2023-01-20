using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using installEAS;
using static installEAS.Animate;

namespace InstallEAS
{

    // the WPF MessageBoxButtons enum does not include AbortRetryCancel or RetryCancel - WTF microsoft!?
    // these buttons cannot be used with the standard messagebox
    /// <summary>
    /// Message box button groups for MessageBoxEx
    /// </summary>
    public enum MessageBoxButtonEx { OK = 0, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel }

    // the wpf message box does not use the DialogResult enum for return values. At the same time, they 
    // don't include these values in the MessageBoxResult enum.  - WTF microsoft!?
    /// <summary>
    /// Message box result for MessageBoxEx
    /// </summary>
    public enum MessageBoxResultEx { None = 0, OK, Cancel, Abort, Retry, Ignore, Yes, No }

    /// <summary>
    /// Default button for MessageBoxEx
    /// </summary> 
    public enum MessageBoxButtonDefault
    {
        OK, Cancel, Yes, No, Abort, Retry, Ignore, // specific button
        Button1, Button2, Button3,                 // button by ordinal left-to-right position
        MostPositive, LeastPositive,               // button by positivity
        Forms,                                     // button according to the Windows.Forms standard messagebox
        None                                       // no default button
    }

    public sealed partial class MessageBoxEx :Window, INotifyPropertyChanged
    {
        #region static fields

        private static double screenWidth = SystemParameters.WorkArea.Width - 100;

        private static bool enableCloseButton = true;
        private static ContentControl parentWindow;
        private static string buttonTemplateName;
        private static SolidColorBrush messageBackground;
        private static SolidColorBrush messageForeground;
        private static SolidColorBrush buttonBackground;
        private static double maxFormWidth = screenWidth;
        private static bool isSilent = false;
        private static Visibility showDetailsBtn = Visibility.Collapsed;
        private static string detailsText;
        private static Visibility showCheckBox = Visibility.Collapsed;
        private static MsgBoxExCheckBoxData checkBoxData = null;
        private static System.Windows.Media.FontFamily msgFontFamily = new System.Windows.Media.FontFamily( "Segoe UI" );
        private static double msgFontSize = 12;
        private static Uri url = null;
        private static Visibility showUrl = Visibility.Collapsed;
        private static string urlDisplayName = null;
        private static SolidColorBrush urlForeground = new SolidColorBrush( DefaultUrlForegroundColor );
        private static string delegateToolTip;
        private static List<string> installedFonts = new List<string>();
        public static MessageBoxButtonDefault staticButtonDefault;

        #endregion static fields

        #region static properties

        public static System.Windows.Media.Color DefaultUrlForegroundColor => System.Windows.Media.Colors.Blue;

        /// <summary>
        /// Get/set the icon tooltip text
        /// </summary>
        private static string MsgBoxIconToolTip { get; set; }
        /// <summary>
        /// Get/set the external icon delegate object
        /// </summary>
        private static MsgBoxExDelegate DelegateObj { get; set; }
        /// <summary>
        /// Get/set the flag that indicates whether the parent messagebox is closed after the 
        /// external action is finished.
        /// </summary>
        private static bool ExitAfterErrorAction { get; set; }

        /// <summary>
        /// Get/set the parent content control
        /// </summary>
        public static ContentControl ParentWindow { get => parentWindow;
            set => parentWindow = value;
        }
        /// <summary>
        /// Get/set the button template name (for styling buttons)
        /// </summary>
        public static string ButtonTemplateName { get => buttonTemplateName;
            set => buttonTemplateName = value;
        }
        /// <summary>
        /// Get/set the brush for the message text background
        /// </summary>
        public static SolidColorBrush MessageBackground { get => messageBackground;
            set => messageBackground = value;
        }
        /// <summary>
        /// Get/set the brush for the message text foreground
        /// </summary>
        public static SolidColorBrush MessageForeground { get => messageForeground;
            set => messageForeground = value;
        }
        /// <summary>
        /// Get/set the brush for the button panel background
        /// </summary>
        public static SolidColorBrush ButtonBackground { get => buttonBackground;
            set => buttonBackground = value;
        }
        /// <summary>
        /// Get/set max form width
        /// </summary>
        public static double MaxFormWidth { get => maxFormWidth;
            set => maxFormWidth = value;
        }
        /// <summary>
        /// Get the visibility of the no button
        /// </summary>
        public static Visibility ShowDetailsBtn { get => showDetailsBtn;
            set => showDetailsBtn = value;
        }
        /// <summary>
        /// Get/set details text
        /// </summary>
        public static string DetailsText { get => detailsText;
            set => detailsText = value;
        }
        /// <summary>
        /// Get/set the visibility of the checkbox
        /// </summary>
        public static Visibility ShowCheckBox { get => showCheckBox;
            set => showCheckBox = value;
        }
        /// <summary>
        /// Get/set the checkbox data object
        /// </summary>
        public static MsgBoxExCheckBoxData CheckBoxData { get => checkBoxData;
            set => checkBoxData = value;
        }
        /// <summary>
        /// Get/set the font family
        /// </summary>
        public static System.Windows.Media.FontFamily MsgFontFamily { get => msgFontFamily;
            set => msgFontFamily = value;
        }
        /// <summary>
        /// Get/set the font size
        /// </summary>
        public static double MsgFontSize { get => msgFontSize;
            set => msgFontSize = value;
        }
        /// <summary>
        /// Get/set the uri object that represents the desired URL
        /// </summary>
        public static Uri Url { get => url;
            set => url = value;
        }
        /// <summary>
        /// Get/set the visibility of the checkbox
        /// </summary>
        public static Visibility ShowUrl { get => showUrl;
            set => showUrl = value;
        }
        /// <summary>
        /// Get/set the optional url display name
        /// </summary>
        public static string UrlDisplayName { get => urlDisplayName;
            set => urlDisplayName = value;
        }
        /// <summary>
        /// Get/set the brush for the message text background
        /// </summary>
        public static SolidColorBrush UrlForeground { get => urlForeground;
            set => urlForeground = value;
        }
        /// <summary>
        /// Get/set the delegate tooltip text
        /// </summary>
        public static string DelegateToolTip { get => delegateToolTip;
            set => delegateToolTip = value;
        }

        #endregion static properties

        #region Show and ShowEx

        /// <summary>
        /// Does the work of actually opening the messagebox
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <param name="buttons"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        private static MessageBoxResult OpenMessageBox( Window owner, string msg, string title, MessageBoxButton buttons, MessageBoxImage image )
        {
            owner ??= Application.Current.MainWindow is { Visibility: Visibility.Visible } ? Application.Current.MainWindow : null;

            var form = new MessageBoxEx( msg, title, buttons, image ) { Owner = owner };

            form.ShowDialog();
            return form.MessageResult;
        }

        ///<summary>
        ///Does the work of actually opening the messagebox with extended functionality
        ///</summary>
        ///<param name="msg"></param>
        ///<param name="title"></param>
        ///<param name="buttons"></param>
        ///<param name="image"></param>
        ///<returns></returns>
        private static MessageBoxResultEx OpenMessageBox( Window owner, string msg, string title, MessageBoxButtonEx buttons, MessageBoxImage image )
        {
            owner ??= Application.Current.MainWindow is { Visibility: Visibility.Visible } ? Application.Current.MainWindow : null;
            var form = new MessageBoxEx( msg, title, buttons, image ) { Owner = owner };
            form.ShowDialog();
            return form.MessageResultEx;
        }

        /// <summary>
        /// Show the message box with the same characteristics as the standard WPF message box. The args <br/> 
        /// parameter can accept one or more of the following parameters. Sequence is not a factor, and <br/>
        /// once a value that is NOT the default is encountered, evaluation stops for that object type. <br /><br/>
        /// -- expected args: <see cref="string"/> (title), <see cref="Window"/> (owner), <see cref="MessageBoxButton"/>, <see cref="MessageBoxImage"/>, <see cref="MessageBoxButtonDefault"/><br /><br />
        /// -------- 
        /// </summary>
        /// <param name="msg">The message to display</param>
        /// <param name="args">An object array that contains 1 or more objects (see summary).</param>
        /// <returns>Button that was clicked to dismiss the form.</returns>
        public static MessageBoxResult Show( string msg, params object[] args )
        {
            string title = null;
            Window owner = null;
            var buttons = MessageBoxButton.OK;
            var image = MessageBoxImage.None;
            var buttonDefault = MessageBoxButtonDefault.Forms;
            foreach (var item in args)
            {
                if (item is string ttl && !string.IsNullOrEmpty( title )) { title = ttl; }
                if (item is MessageBoxButton btn && buttons == MessageBoxButton.OK) { buttons = btn; }
                if (item is MessageBoxImage img && image == MessageBoxImage.None) { image = img; }
                if (item is MessageBoxButtonDefault def && buttonDefault == MessageBoxButtonDefault.Forms) { buttonDefault = def; }
                if (item is Window wnd && owner == null) { owner = wnd; }
            }
            staticButtonDefault = buttonDefault;

            title = (string.IsNullOrEmpty( title )) ? string.Empty : title.Trim();
            if (!string.IsNullOrEmpty(title)) return OpenMessageBox(owner, msg, title, buttons, image);
            title = image != MessageBoxImage.None ? image.ToString() : _DEFAULT_CAPTION;
            return OpenMessageBox( owner, msg, title, buttons, image );
        }

        /// <summary>
        /// Show the message box with extended message box functionality. The args parameter can accept <br />
        /// one or more of the following parameters. Sequence is not a factor, and once a value that is NOT <br />
        /// the default is encountered, evaluation stops for that object type. <br /><br/>
        /// -- expected args: <see cref="string"/> (title), <see cref="Window"/> (owner), <see cref="MessageBoxButtonEx"/>, <see cref="MessageBoxImage"/>, <see cref="MessageBoxButtonDefault"/>, <see cref="MsgBoxExtendedFunctionality"/><br /><br />
        /// -------- 
        /// </summary>
        /// <param name="msg">The message to display</param>
        /// <param name="args">An object array that contains 1 or more objects (see summary).</param>
        /// <returns>Button that was clicked to dismiss the form.</returns>
        public static MessageBoxResultEx ShowEx( string msg, params object[] args )
        {
            string title = null;
            Window owner = null;
            var image = MessageBoxImage.None;
            var buttons = MessageBoxButtonEx.OK;
            var buttonDefault = MessageBoxButtonDefault.Forms;
            MsgBoxExtendedFunctionality data = null;
            foreach (var item in args)
            {
                switch (item)
                {
                    case string ttl when !string.IsNullOrEmpty( title ):
                        title = ttl;
                        break;
                    case MessageBoxButtonEx btn when buttons == MessageBoxButtonEx.OK:
                        buttons = btn;
                        break;
                }

                if (item is MessageBoxImage img && image == MessageBoxImage.None) { image = img; }
                switch (item)
                {
                    case MessageBoxButtonDefault def when buttonDefault == MessageBoxButtonDefault.Forms:
                        buttonDefault = def;
                        break;
                    case Window wnd when owner == null:
                        owner = wnd;
                        break;
                    case MsgBoxExtendedFunctionality mef when data == null:
                        data = mef;
                        break;
                }
            }
            staticButtonDefault = buttonDefault;

            if (data != null)
            {
                // details text ===================
                DetailsText = data.DetailsText;

                // checkbox =======================
                ShowCheckBox = Visibility.Collapsed;
                CheckBoxData = data.CheckBoxData;

                // clickable icon =================
                DelegateObj = data.MessageDelegate;
                ExitAfterErrorAction = data.ExitAfterAction;
                DelegateToolTip = data.DelegateToolTip;

                // url ============================
                // assume we're not showing a url
                ShowUrl = Visibility.Collapsed;
                Url = null;
                UrlDisplayName = null;
                // now, see if we want to
                if (data.URL != null)
                {
                    // if the url is ultimately null, no url will be displayed, and none of the following 
                    // settings really mean anything
                    Url = data.URL.URL;
                    ShowUrl = (Url == null) ? Visibility.Collapsed : Visibility.Visible;
                    UrlDisplayName = (Url == null) ? null : data.URL.DisplayName;
                    // make sure we actually set a color. If the color was not included, use the message text color
                    UrlForeground = (data.URL.Foreground != null) ? new SolidColorBrush( data.URL.Foreground ) : new SolidColorBrush( DefaultUrlForegroundColor );
                }
            }

            title = true ? string.Empty : title.Trim();
            if (string.IsNullOrEmpty( title.Trim() ))
            {
                if (image != MessageBoxImage.None)
                {
                    title = image.ToString();
                }
                else
                {
                    title = _DEFAULT_CAPTION;
                }
            }
            return OpenMessageBox( owner, msg, title, buttons, image );
        }


        /// <summary>
        /// Set the background color for the message area
        /// </summary>
        /// <param name="color">The color to set. Can be a name (White) or an octet string(#FFFFFF).</param>
        public static void SetMessageBackground( System.Windows.Media.Color color )
        {
            try
            {
                MessageBackground = new SolidColorBrush( color );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message, ex.ToString() );
            }
        }

        /// <summary>
        /// Set the foreground color for the message area
        /// </summary>
        /// <param name="color">The color to set. Can be a name (White) or an octet string(#FFFFFF).</param>
        public static void SetMessageForeground( System.Windows.Media.Color color )
        {
            try
            {
                MessageForeground = new SolidColorBrush( color );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message, ex.ToString() );
            }
        }

        /// <summary>
        /// Set the background color for the button panel area
        /// </summary>
        /// <param name="color">The color to set. Can be a name (White) or an octet string(#FFFFFF).</param>
        public static void SetButtonBackground( System.Windows.Media.Color color )
        {
            try
            {
                ButtonBackground = new SolidColorBrush( color );
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message, ex.ToString() );
            }
        }

        /// <summary>
        ///  Create a WPF-compatible Color from an string (such as "White", "white", or "#FFFFFF").
        /// </summary>
        /// <param name="colorOctet"></param>
        /// <returns>A Media.Color. If color is invalid, returns #000000.</returns>
        public static System.Windows.Media.Color ColorFromString( string colorString )
        {
            var wpfColor = System.Windows.Media.Colors.Black;
            try
            {
                wpfColor = (System.Windows.Media.Color)(System.Windows.Media.ColorConverter.ConvertFromString( colorString ));
            }
            catch (Exception) { }
            return wpfColor;
        }

        // font

        /// <summary>
        /// Set the font family and size from the application's main window
        /// </summary>
        public static void SetFont()
        {
            if (Application.Current.MainWindow == null) return;
            MsgFontFamily = Application.Current.MainWindow.FontFamily;
            MsgFontSize = Application.Current.MainWindow.FontSize;
        }

        /// <summary>
        /// Set the font family and size from the specified content control (usually the parent window)
        /// </summary>
        /// <param name="parent"></param>
        public static void SetFont( ContentControl parent )
        {
            MsgFontFamily = parent.FontFamily;
            MsgFontSize = parent.FontSize;
        }

        /// <summary>
        /// Set the font family and size
        /// </summary>
        /// <param name="familyName">The name of the desired font family (will not be set if null/empty)</param>
        /// <param name="size">Size of font (min size is 1)</param>
        public static void SetFont( string familyName, double size )
        {
            if (!IsFontFamilyValid( familyName ))
            {
                if (!string.IsNullOrEmpty( familyName ))
                {
                    MsgFontFamily = new System.Windows.Media.FontFamily( familyName );
                }
            }
            MsgFontSize = Math.Max( 1.0, size );
        }

        private static bool IsFontFamilyValid( string name )
        {
            //if (installedFonts.Count == 0)
            //{
            //    using (InstalledFontCollection fontsCollection = new InstalledFontCollection())
            //    {
            //        installedFonts = (from x in fontsCollection.Families select x.Name).ToList();
            //    }
            //}
            return installedFonts.Contains( name );
        }

        // mechanicals 

        /// <summary>
        /// Set the custom button template *NAME*
        /// </summary>
        /// <param name="name"></param>
        public static void SetButtonTemplateName( string name )
        {
            ButtonTemplateName = name;
        }

        /// <summary>
        /// Sets the max form width to largest of 300 or the specified value
        /// </summary>
        /// <param name="value"></param>
        public static void SetMaxFormWidth( double value )
        {
            MaxFormWidth = Math.Max( value, 300 );
            double minWidth = 300;
            MaxFormWidth = Math.Max( minWidth, Math.Min( value, screenWidth ) );
        }

        /// <summary>
        /// Resets the configuration items to default values
        /// </summary>
        public static void ResetToDefaults()
        {
            MsgFontSize = 12d;
            MsgFontFamily = new System.Windows.Media.FontFamily( "Segoe UI" );
            DelegateObj = null;
            DetailsText = null;
            MessageForeground = null;
            MessageBackground = null;
            ButtonBackground = null;
            ParentWindow = null;
            isSilent = false;
            enableCloseButton = true;
            ButtonTemplateName = null;
            MsgBoxIconToolTip = null;
            ShowCheckBox = Visibility.Collapsed;
            CheckBoxData = null;
            ExitAfterErrorAction = false;
            MaxFormWidth = 800;
            Url = null;
            ShowUrl = Visibility.Collapsed;
            UrlDisplayName = null;
            UrlForeground = new SolidColorBrush( DefaultUrlForegroundColor );
            staticButtonDefault = MessageBoxButtonDefault.Forms;
        }

        public static void EnableCloseButton( bool enable )
        {
            enableCloseButton = enable;
        }

        // message box icon 
        /// <summary>
        ///  Toggle system sounds associated with MessageBoxImage icons
        /// </summary>
        /// <param name="quiet"></param>
        public static void SetAsSilent( bool quiet )
        {
            isSilent = quiet;
        }

        /// <summary>
        /// Specify the button that will be displayed as the default button in the in the message 
        /// box. the message box will return to the default "Forms" setting when it is dismissed.
        /// </summary>
        /// <param name="buttonDefault"></param>
        public static void SetDefaultButton( MessageBoxButtonDefault buttonDefault )
        {
            staticButtonDefault = buttonDefault;
        }

        #endregion static configuration methods

    }



    public abstract class MsgBoxExCheckBoxData :INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        private bool isModified = false;
        public bool IsModified { get => this.isModified;
            set { if (value != this.isModified) { this.isModified = true; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies that the property changed, and sets IsModified to true.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void NotifyPropertyChanged( [CallerMemberName] String propertyName = "" )
        {
            if (this.PropertyChanged == null) return;
            this.PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
            if (propertyName != "IsModified")
            {
                this.IsModified = true;
            }
        }

        #endregion INotifyPropertyChanged

        private string checkBoxText;
        private bool checkBoxIsChecked;

        /// <summary>
        /// Get/set the text content of the checkbox
        /// </summary>
        public string CheckBoxText { get => this.checkBoxText;
            set { if (value != this.checkBoxText) { this.checkBoxText = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag that indicates whether the checkbox is checked
        /// </summary>
        public bool CheckBoxIsChecked { get => this.checkBoxIsChecked;
            set { if (value != this.checkBoxIsChecked) { this.checkBoxIsChecked = value; this.NotifyPropertyChanged(); } } }
    }



    public abstract class MsgBoxExDelegate
    {
        /// <summary>
        /// Get/set the message text from the calling message box
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Get/set the details text (if it was specified in the messagebox)
        /// </summary>
        public string Details { get; set; }
        /// <summary>
        /// Get/set the message datetime at which this object was created
        /// </summary>
        public DateTime MessageDate { get; set; }

        /// <summary>
        /// Performs the desired action, and returns the result. MUST BE OVERIDDEN IN INHERITING CLASS. 
        /// </summary>
        /// <returns></returns>
        public virtual MessageBoxResult PerformAction( string message, string details = null )
        {
            throw new NotImplementedException();
        }
    }



    public abstract class MsgBoxExtendedFunctionality
    {
        public MessageBoxButtonDefault ButtonDefault { get; set; }
        /// <summary>
        /// Get/set the details text to display
        /// </summary>
        public string DetailsText { get; set; }

        /// <summary>
        /// Get/set the checkbox data object
        /// </summary>
        public MsgBoxExCheckBoxData CheckBoxData { get; set; }

        /// <summary>
        /// Get/set the clickable icon delegate object
        /// </summary>
        public MsgBoxExDelegate MessageDelegate { get; set; }
        /// <summary>
        /// Get/set the flag indicating whether the messagebox is dismissed after the delegate 
        /// action has completed.
        /// </summary>
        public bool ExitAfterAction { get; set; }
        public string DelegateToolTip { get; set; }

        /// <summary>
        /// Get/set the url
        /// </summary>
        public MsgBoxUrl URL { get; set; }

        public MsgBoxExtendedFunctionality()
        {
            this.ButtonDefault = MessageBoxButtonDefault.Forms;
            this.DetailsText = null;
            this.CheckBoxData = null;
            this.MessageDelegate = null;
            this.URL = null;
            this.DelegateToolTip = "Click this icon for additional info/actions.";
        }
    }


    public abstract class MsgBoxUrl
    {
        /// <summary>
        /// Get/set the web link. Any Uri type other than "http" is ignored. The URL is also used for the tooltip.
        /// </summary>
        public Uri URL { get; set; }
        /// <summary>
        /// Get/set the optional display name for the web link
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Get/set the foreground color for the web link
        /// </summary>
        public System.Windows.Media.Color Foreground { get; set; }

        public MsgBoxUrl()
        {
            // default color
            this.Foreground = MessageBoxEx.DefaultUrlForegroundColor;
        }
    }
    /// <summary>
    /// Логика взаимодействия для MessageBoxEx.xaml
    /// </summary>
	public sealed partial class MessageBoxEx :Window, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        private bool isModified = false;
        public bool IsModified { get => this.isModified;
            set { if (value != this.isModified) { this.isModified = true; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies that the property changed, and sets IsModified to true.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void NotifyPropertyChanged( [CallerMemberName] String propertyName = "" )
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
                if (propertyName != "IsModified")
                {
                    this.IsModified = true;
                }
            }
        }

        #endregion INotifyPropertyChanged

        // so we can run the browser when the optional URL is clicked
        [DllImport( "shell32.dll" )]
        public static extern IntPtr ShellExecute( IntPtr hwnd, string lpOperation
                                                 , string lpFile, string lpParameters
                                                 , string lpDirectory, int nShowCmd );

        private const string _DEFAULT_CAPTION = "Application Message";

        #region fields

        private double screenHeight;
        private string message;
        private string messageTitle;
        private MessageBoxButton? buttons;
        private MessageBoxResult messageResult;
        private MessageBoxButtonEx? buttonsEx;
        private MessageBoxResultEx messageResultEx;
        private ImageSource messageIcon;
        private MessageBoxImage msgBoxImage;
        private double buttonWidth = 0d;
        private bool expanded = false;
        private bool isDefaultOK;
        private bool isDefaultCancel;
        private bool isDefaultYes;
        private bool isDefaultNo;
        private bool isDefaultAbort;
        private bool isDefaultRetry;
        private bool isDefaultIgnore;

        private bool usingExButtons = false;

        #endregion fields

        #region properties

        /// <summary>
        /// Get/set the screen's work area height
        /// </summary>
        public double ScreenHeight { get => this.screenHeight;
            set { if (value != this.screenHeight) { this.screenHeight = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the message text
        /// </summary>
        public string Message { get => this.message;
            set { if (value != this.message) { this.message = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the form caption 
        /// </summary>
        public string MessageTitle { get => this.messageTitle;
            set { if (value != this.messageTitle) { this.messageTitle = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the message box result (which button was pressed to dismiss the form)
        /// </summary>
        public MessageBoxResult MessageResult { get => this.messageResult;
            set => this.messageResult = value;
        }
        public MessageBoxResultEx MessageResultEx { get => this.messageResultEx;
            set => this.messageResultEx = value;
        }
        /// <summary>
        /// Get/set the buttons ued in the form (and update visibility for them)
        /// </summary>
        public MessageBoxButton? Buttons
        {
            get => this.buttons;
            set
            {
                if (value != this.buttons)
                {
                    this.buttons = value;
                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged( "ShowOk" );
                    this.NotifyPropertyChanged( "ShowCancel" );
                    this.NotifyPropertyChanged( "ShowYes" );
                    this.NotifyPropertyChanged( "ShowNo" );

                }
            }
        }
        public MessageBoxButtonEx? ButtonsEx
        {
            get => this.buttonsEx;
            set
            {
                if (value == this.buttonsEx) return;
                this.buttonsEx = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged( "ShowOk" );
                this.NotifyPropertyChanged( "ShowCancel" );
                this.NotifyPropertyChanged( "ShowYes" );
                this.NotifyPropertyChanged( "ShowNo" );
                this.NotifyPropertyChanged( "ShowAbort" );
                this.NotifyPropertyChanged( "ShowRetry" );
                this.NotifyPropertyChanged( "ShowIgnore" );
            }
        }
        /// <summary>
        /// Get the visibility of the ok button
        /// </summary>
        public Visibility ShowOk =>
            (!this.usingExButtons && this.Buttons == MessageBoxButton.OK ||
             !this.usingExButtons && this.Buttons == MessageBoxButton.OKCancel ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.OK ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.OKCancel) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get the visibility of the cancel button
        /// </summary>
        public Visibility ShowCancel =>
            (!this.usingExButtons && this.Buttons == MessageBoxButton.OKCancel ||
             !this.usingExButtons && this.Buttons == MessageBoxButton.YesNoCancel ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.OKCancel ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNoCancel ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.RetryCancel) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get the visibility of the yes button
        /// </summary>
        public Visibility ShowYes =>
            (!this.usingExButtons && this.Buttons == MessageBoxButton.YesNo ||
             !this.usingExButtons && this.Buttons == MessageBoxButton.YesNoCancel ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNo ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get the visibility of the no button
        /// </summary>
        public Visibility ShowNo =>
            (!this.usingExButtons && this.Buttons == MessageBoxButton.YesNo ||
             !this.usingExButtons && this.Buttons == MessageBoxButton.YesNoCancel ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNo ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.YesNoCancel) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get the visibility of the retry button
        /// </summary>
        public Visibility ShowRetry =>
            (this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.AbortRetryIgnore ||
             this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.RetryCancel) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get the visibility of the abort button
        /// </summary>
        public Visibility ShowAbort => (this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.AbortRetryIgnore) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get the visibility of the ignore button
        /// </summary>
        public Visibility ShowIgnore => (this.usingExButtons && this.ButtonsEx == MessageBoxButtonEx.AbortRetryIgnore) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get this visibility of the message box icon
        /// </summary>
        public Visibility ShowIcon => (this.MessageIcon != null) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Get/set the icon specified by the user
        /// </summary>
        public ImageSource MessageIcon { get => this.messageIcon;
            set { if (value != this.messageIcon) { this.messageIcon = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the width of the largest button (so all buttons are the same width as the widest button)
        /// </summary>
        public double ButtonWidth { get => this.buttonWidth;
            set { if (value != this.buttonWidth) { this.buttonWidth = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag inidcating whether the expander is expanded
        /// </summary>
        public bool Expanded { get => this.expanded;
            set { if (value != expanded) { this.expanded = value; this.NotifyPropertyChanged(); } } }

        // default button flags

        /// <summary>
        /// Get/set the flag indicating whether OK is the default button
        /// </summary>
        public bool IsDefaultOK { get => this.isDefaultOK;
            set { if (value != this.isDefaultOK) { this.isDefaultOK = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag indicating whether Cancel is the default button
        /// </summary>
        public bool IsDefaultCancel { get => this.isDefaultCancel;
            set { if (value != this.isDefaultCancel) { this.isDefaultCancel = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag indicating whether Yes is the default button
        /// </summary>
        public bool IsDefaultYes { get => this.isDefaultYes;
            set { if (value != this.isDefaultYes) { this.isDefaultYes = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag indicating whether No is the default button
        /// </summary>
        public bool IsDefaultNo { get => this.isDefaultNo;
            set { if (value != this.isDefaultNo) { this.isDefaultNo = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag indicating whether Abort is the default button
        /// </summary>
        public bool IsDefaultAbort { get => this.isDefaultAbort;
            set { if (value != this.isDefaultAbort) { this.isDefaultAbort = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag indicating whether Retry is the default button
        /// </summary>
        public bool IsDefaultRetry { get => this.isDefaultRetry;
            set { if (value != this.isDefaultRetry) { this.isDefaultRetry = value; this.NotifyPropertyChanged(); } } }
        /// <summary>
        /// Get/set the flag indicating whether Ignore is the default button
        /// </summary>
        public bool IsDefaultIgnore { get => this.isDefaultIgnore;
            set { if (value != this.isDefaultIgnore) { this.isDefaultIgnore = value; this.NotifyPropertyChanged(); } } }


        #endregion properties

        #region constructors

        /// <summary>
        /// Default constructor for VS designer
        /// </summary>
        private MessageBoxEx()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.LargestButtonWidth();
        }

        /// <summary>
        /// Constructor for standard buttons
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <param name="buttons">(Optinal) Message box button(s) to be displayed (default = OK)</param>
        /// <param name="image">(Optional) Message box image to display (default = None)</param>
        public MessageBoxEx( string msg, string title, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None )
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Init( msg, title, buttons, image );
        }

        /// <summary>
        /// Constructor for extended buttons
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        /// <param name="buttons">(Optinal) Message box button(s) to be displayed (default = OK)</param>
        /// <param name="image">(Optional) Message box image to display (default = None)</param>
        public MessageBoxEx( string msg, string title, MessageBoxButtonEx buttons = MessageBoxButtonEx.OK, MessageBoxImage image = MessageBoxImage.None )
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Init( msg, title, buttons, image );
        }

        #endregion constructors

        #region non-static methods

        /// <summary>
        /// Performs message box initialization when using standard message box buttons
        /// </summary>
        /// <param name="msg">The message to display</param>
        /// <param name="title">Window title</param>
        /// <param name="buttons">What buttons are to be displayed</param>
        /// <param name="image">What message box icon image is to be displayed</param>
        private void Init( string msg, string title, MessageBoxButton buttons, MessageBoxImage image )
        {
            this.InitTop( msg, title );
            this.usingExButtons = false;
            this.ButtonsEx = null;
            this.Buttons = buttons;
            this.SetButtonTemplates();
            this.InitBottom( image );
            this.FindDefaultButton( staticButtonDefault );
        }

        /// <summary>
        /// Performs message box initialization when using extended message box buttons
        /// </summary>
        /// <param name="msg">The message to display</param>
        /// <param name="title">Window title</param>
        /// <param name="buttons">What buttons are to be displayed</param>
        /// <param name="image">What message box icon image is to be displayed</param>
        private void Init( string msg, string title, MessageBoxButtonEx buttons, MessageBoxImage image )
        {
            this.InitTop( msg, title );
            this.usingExButtons = true;
            this.Buttons = null;
            this.ButtonsEx = buttons;
            this.SetButtonTemplates();
            this.InitBottom( image );
            this.FindDefaultButtonEx( staticButtonDefault );
        }

        /// <summary>
        /// Init the common stuff BEFORE buttons are processed
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="title"></param>
        private void InitTop( string msg, string title )
        {
            // determine whether or not to show the details pane and checkbox
            ShowDetailsBtn = (string.IsNullOrEmpty( DetailsText )) ? Visibility.Collapsed : Visibility.Visible;
            ShowCheckBox = (CheckBoxData == null) ? Visibility.Collapsed : Visibility.Visible;

            // Well, the binding for family/size don't appear to be working, so I have to set them 
            // manually. Weird...
            this.FontFamily = MsgFontFamily;
            this.FontSize = MsgFontSize;
            this.LargestButtonWidth();

            // determine the screen area height, and the height of the textblock
            this.ScreenHeight = SystemParameters.WorkArea.Height - 150;

            // configure the form based on specified criteria
            this.Message = msg;
            this.MessageTitle = (string.IsNullOrEmpty( title.Trim() )) ? _DEFAULT_CAPTION : title;

            // url (if specified)
            if (Url == null) return;
            this.tbUrl.Text = (string.IsNullOrEmpty( UrlDisplayName )) ? Url.ToString() : UrlDisplayName;
            this.tbUrl.ToolTip = new ToolTip() { Content = Url.ToString() };
        }

        /// <summary>
        /// Init common stuff AFTER buttons are processed
        /// </summary>
        /// <param name="image"></param>
        private void InitBottom( MessageBoxImage image )
        {
            // set the form's colors (you can also set these colors in your program's startup code 
            // (either in app.xaml.cs or MainWindow.cs) before you use the MessageBox for the 
            // first time
            MessageBackground = MessageBackground == null ? new SolidColorBrush( Colors.White ) : MessageBackground;
            MessageForeground = MessageForeground == null ? new SolidColorBrush( Colors.Black ) : MessageForeground;
            ButtonBackground = (ButtonBackground == null) ? new SolidColorBrush( ColorFromString( "#cdcdcd" ) ) : ButtonBackground;

            this.MessageIcon = null;

            this.msgBoxImage = image;

            if (DelegateObj != null)
            {
                Style style = (Style)(this.FindResource( "ImageOpacityChanger" ));
                if (style != null)
                {
                    this.imgMsgBoxIcon.Style = style;
                    if (!string.IsNullOrEmpty( DelegateToolTip ))
                    {
                        ToolTip tooltip = new ToolTip() { Content = DelegateToolTip };
                        // for some reason, Image elements can't do tooltips, so I assign the tootip 
                        // to the parent grid. This seems to work fine.
                        this.imgGrid.ToolTip = tooltip;
                    }
                }
            }

            // multiple images have the same ordinal value, and are indicated in the comments below. 
            // WTF Microsoft? 
            switch ((int)image)
            {
                case 16: // MessageBoxImage.Error, MessageBoxImage.Stop, MessageBox.Image.Hand
                    {
                        this.MessageIcon = this.GetIcon( SystemIcons.Error );
                        if (!isSilent) { SystemSounds.Hand.Play(); }
                    }
                    break;

                case 64: // MessageBoxImage.Information, MessageBoxImage.Asterisk 
                    {
                        this.MessageIcon = this.GetIcon( SystemIcons.Information );
                        if (!isSilent) { SystemSounds.Asterisk.Play(); }
                    }
                    break;

                case 32: // MessageBoxImage.Question
                    {
                        this.MessageIcon = this.GetIcon( SystemIcons.Question );
                        if (!isSilent) { SystemSounds.Question.Play(); }
                    }
                    break;

                case 48: // MessageBoxImage.Warning, MessageBoxImage.Exclamation
                    {
                        this.MessageIcon = this.GetIcon( SystemIcons.Warning );
                        if (!isSilent) { SystemSounds.Exclamation.Play(); }
                    }
                    break;
                default:
                    this.MessageIcon = null;
                    break;
            }
        }

        /// <summary>
        /// Converts the specified icon into a WPF-comptible ImageSource object.
        /// </summary>
        /// <param name="icon"></param>
        /// <returns>An ImageSource object that represents the specified icon.</returns>
        public ImageSource GetIcon( System.Drawing.Icon icon )
        {
            var image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon( icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions() );
            return image;
        }

        // The form is rendered and position BEFORE the SizeToContent property takes effect, 
        // so we have to take stepts to re-center it after the size changes. This code takes care 
        // of the re-positioning, and is called from the SizeChanged event handler.
        /// <summary>
        /// Center the form on the screen.
        /// </summary>
        private void CenterInScreen()
        {
            double width = this.ActualWidth;
            double height = this.ActualHeight;
            this.Left = (SystemParameters.WorkArea.Width - width) / 2 + SystemParameters.WorkArea.Left;
            this.Top = (SystemParameters.WorkArea.Height - height) / 2 + SystemParameters.WorkArea.Top;
        }

        /// <summary>
        /// Calculate the width of the largest button.
        /// </summary>
        private void LargestButtonWidth()
        {
            // we base the width on the width of the content. This allows us to avoid the problems 
            // with button width/actualwidth properties, especially when a given button is 
            // Collapsed.
            Typeface typeface = new Typeface( this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch );

            StackPanel panel = (StackPanel)this.stackButtons.Child;
            double width = 0;
            string largestName = string.Empty;
            foreach (Button button in panel.Children)
            {
                // Using the FormattedText object 
                // will strip whitespace before measuring the text, so we convert spaces to double 
                // hyphens to compensate (I like to pad button Content with a leading and trailing 
                // space) so that the button is wide enough to present a more padded appearance.
                FormattedText formattedText = new FormattedText( (button.Name == "btnDetails") ? "--Details--" : ((string)(button.Content)).Replace( " ", "--" ),
                                                                CultureInfo.CurrentUICulture,
                                                                FlowDirection.LeftToRight,
                                                                typeface,
                                                                FontSize = this.FontSize,
                                                                System.Windows.Media.Brushes.Black,
                                                                VisualTreeHelper.GetDpi( this ).PixelsPerDip );
                if (width < formattedText.Width)
                {
                    largestName = button.Name;
                }
                width = Math.Max( width, formattedText.Width );
            }
            this.ButtonWidth = Math.Ceiling( width/*width + polyArrow.Width+polyArrow.Margin.Right+Margin.Left*/);
        }

        /// <summary>
        /// Sets the custom button template if necessary and possible
        /// </summary>
        private void SetButtonTemplates()
        {
            // set the button template (if specified)
            if (!string.IsNullOrEmpty( ButtonTemplateName ))
            {
                bool foundResource = true;
                try
                {
                    this.FindResource( ButtonTemplateName );
                }
                catch (Exception)
                {
                    foundResource = false;
                }
                if (foundResource)
                {
                    this.btnOK.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                    this.btnYes.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                    this.btnNo.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                    this.btnCancel.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                    this.btnAbort.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                    this.btnRetry.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                    this.btnIgnore.SetResourceReference( Control.TemplateProperty, ButtonTemplateName );
                }
            }
        }

        /// <summary>
        /// Find the default button based on the extended buttons displayed, and the default button specified
        /// </summary>
        /// <param name="buttonDefault"></param>
        private void FindDefaultButtonEx( MessageBoxButtonDefault buttonDefault )
        {
            // determine default button
            this.IsDefaultOK = false;
            this.IsDefaultCancel = false;
            this.IsDefaultYes = false;
            this.IsDefaultNo = false;
            this.IsDefaultAbort = false;
            this.IsDefaultRetry = false;
            this.IsDefaultIgnore = false;
            if (buttonDefault != MessageBoxButtonDefault.None)
            {
                switch (this.ButtonsEx)
                {
                    case MessageBoxButtonEx.OK: this.IsDefaultOK = true; break;
                    case MessageBoxButtonEx.OKCancel:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.OK:
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultOK = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.Cancel:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultCancel = true; break;

                                // windows.forms.messagebox default
                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultOK = true; break;
                            }
                        }
                        break;
                    case MessageBoxButtonEx.YesNoCancel:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.Yes: break;
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultYes = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.No: this.IsDefaultNo = true; break;

                                case MessageBoxButtonDefault.Button3:
                                case MessageBoxButtonDefault.Cancel:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultCancel = true; break;

                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultYes = true; break;
                            }
                        }
                        break;
                    case MessageBoxButtonEx.YesNo:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.Yes:
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultYes = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.No:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultNo = true; break;

                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultYes = true; break;
                            }
                        }
                        break;
                    case MessageBoxButtonEx.RetryCancel:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.Retry:
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultRetry = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.Cancel:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultCancel = true; break;

                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultRetry = true; break;
                            }
                        }
                        break;
                    case MessageBoxButtonEx.AbortRetryIgnore:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.Abort:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultAbort = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.Retry: this.IsDefaultRetry = true; break;

                                case MessageBoxButtonDefault.Button3:
                                case MessageBoxButtonDefault.Ignore:
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultIgnore = true; break;

                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultAbort = true; break;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Find the default button based on the standard buttons displayed, and the default button specified
        /// </summary>
        /// <param name="buttonDefault"></param>
        private void FindDefaultButton( MessageBoxButtonDefault buttonDefault )
        {
            // determine default button
            this.IsDefaultOK = false;
            this.IsDefaultCancel = false;
            this.IsDefaultYes = false;
            this.IsDefaultNo = false;
            this.IsDefaultAbort = false;
            this.IsDefaultRetry = false;
            this.IsDefaultIgnore = false;
            if (buttonDefault != MessageBoxButtonDefault.None)
            {
                switch (this.Buttons)
                {
                    case MessageBoxButton.OK: this.IsDefaultOK = true; break;
                    case MessageBoxButton.OKCancel:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.OK:
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultOK = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.Cancel:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultCancel = true; break;

                                // windows.forms.messagebox default
                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultOK = true; break;
                            }
                        }
                        break;
                    case MessageBoxButton.YesNoCancel:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.Yes: break;
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultYes = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.No: this.IsDefaultNo = true; break;

                                case MessageBoxButtonDefault.Button3:
                                case MessageBoxButtonDefault.Cancel:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultCancel = true; break;

                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultYes = true; break;
                            }
                        }
                        break;
                    case MessageBoxButton.YesNo:
                        {
                            switch (buttonDefault)
                            {
                                case MessageBoxButtonDefault.Button1:
                                case MessageBoxButtonDefault.Yes:
                                case MessageBoxButtonDefault.MostPositive: this.IsDefaultYes = true; break;

                                case MessageBoxButtonDefault.Button2:
                                case MessageBoxButtonDefault.No:
                                case MessageBoxButtonDefault.LeastPositive: this.IsDefaultNo = true; break;

                                case MessageBoxButtonDefault.Forms:
                                default: this.IsDefaultYes = true; break;
                            }
                        }
                        break;
                }
            }
        }

        #endregion non-static methods

        ////////////////////////////////////////////////////////////////////////////////////////////
        // Form events
        ////////////////////////////////////////////////////////////////////////////////////////////

        #region event handlers

        #region buttons

        /// <summary>
        /// Handle the click event for the OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOK_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.OK;
            this.MessageResultEx = MessageBoxResultEx.OK;
            this.DialogResult = true;
        }

        /// <summary>
        /// Handle the click event for the Yes button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnYes_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.Yes;
            this.MessageResultEx = MessageBoxResultEx.Yes;
            this.DialogResult = true;
        }

        /// <summary>
        /// Handle the click event for the No button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnNo_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.No;
            this.MessageResultEx = MessageBoxResultEx.No;
            this.DialogResult = true;
        }

        private void BtnAbort_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.None;
            this.MessageResultEx = MessageBoxResultEx.Abort;
            this.DialogResult = true;
        }

        private void BtnRetry_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.None;
            this.MessageResultEx = MessageBoxResultEx.Retry;
            this.DialogResult = true;
        }

        private void BtnIgnore_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.None;
            this.MessageResultEx = MessageBoxResultEx.Ignore;
            this.DialogResult = true;
        }

        /// <summary>
        /// Handle the click event for the Cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click( object sender, RoutedEventArgs e )
        {
            this.MessageResult = MessageBoxResult.Cancel;
            this.MessageResultEx = MessageBoxResultEx.Cancel;
            this.DialogResult = true;
        }

        #endregion buttons

        /// <summary>
        /// Handle the size changed event so we can re-center the form on the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifiableWindow_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            // we have to do this because the SizeToContent property is evaluated AFTER the window 
            // is positioned.
            this.CenterInScreen();
        }

        /// <summary>
        /// Handle the window loaded event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            // if this in an error message box, this tooltip will be displayed. The intent is to set 
            // this value one time, and use it throughout the application session. However, you can 
            // certainly set it before displaying the messagebox to something that is contextually 
            // appropriate, but you'll have to clear it or reset it each time you use the MessageBox.
            this.imgMsgBoxIcon.ToolTip = (this.msgBoxImage == MessageBoxImage.Error) ? MsgBoxIconToolTip : null;
        }

        /// <summary>
        ///  Handles the window closing event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing( object sender, CancelEventArgs e )
        {
            // we always clear the details text and checkbox data. 
            DetailsText = null;
            CheckBoxData = null;
            // reset the default button to Forms.
            staticButtonDefault = MessageBoxButtonDefault.Forms;

            // if the user didn't click a button to close the form, we set the MessageResult to the 
            // most negative button value that was available.
            if (this.MessageResult != MessageBoxResult.None) return;
            if (usingExButtons)
            {
                switch (this.ButtonsEx)
                {
                    case MessageBoxButtonEx.OK: this.MessageResultEx = MessageBoxResultEx.OK; break;
                    case MessageBoxButtonEx.YesNoCancel:
                    case MessageBoxButtonEx.OKCancel:
                    case MessageBoxButtonEx.RetryCancel:
                    case MessageBoxButtonEx.AbortRetryIgnore: this.MessageResultEx = MessageBoxResultEx.Cancel; break;
                    case MessageBoxButtonEx.YesNo: this.MessageResultEx = MessageBoxResultEx.No; break;
                }
            }
            else
            {
                switch (this.Buttons)
                {
                    case MessageBoxButton.OK: this.MessageResult = MessageBoxResult.OK; break;
                    case MessageBoxButton.YesNoCancel:
                    case MessageBoxButton.OKCancel: this.MessageResult = MessageBoxResult.Cancel; break;
                    case MessageBoxButton.YesNo: this.MessageResult = MessageBoxResult.No; break;
                }
            }
        }

        /// <summary>
        /// Since an icon isn't a button, we have to look for the left-mouse-up event to know it's 
        /// been clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImgMsgBoxIcon_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            // we only want to allow the click if this is an error message, and the delegate 
            // object has been specified.
            if (DelegateObj == null || this.msgBoxImage != MessageBoxImage.Error ||
                this.Buttons != MessageBoxButton.OK) return;
            DelegateObj.PerformAction( this.Message );
            //despite the result of the method, we close this message
            if (!ExitAfterErrorAction) return;
            // make it like the user clicked the titlebar close button
            this.MessageResult = MessageBoxResult.None;
            this.DialogResult = true;
        }

        /// <summary>
        /// Handle the left mouse up event for the url textblock
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbUrl_MouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {

            ShellExecute( IntPtr.Zero, "open", Url.ToString(), "", "", 5 );
        }

        // disables close button
        [DllImport( "user32.dll" )]
        private static extern IntPtr GetSystemMenu( IntPtr hWnd, bool bRevert );
        [DllImport( "user32.dll" )]
        private static extern bool EnableMenuItem( IntPtr hMenu, uint uIDEnableItem, uint uEnable );

        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;
        private const int WM_SHOWWINDOW = 0x00000018;

        private void Window_SourceInitialized( object sender, EventArgs e )
        {
            if (enableCloseButton) return;
            var hWnd = new WindowInteropHelper( this );
            var sysMenu = GetSystemMenu( hWnd.Handle, false );
            EnableMenuItem( sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED );
        }

        #endregion event handlers

        private void Window_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            this.DragMove();
        }




    }
}
