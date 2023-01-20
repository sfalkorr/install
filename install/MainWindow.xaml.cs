using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using InstallEAS;

using static installEAS.GlobalVariables;
using static installEAS.LogHelper;
using static installEAS.ProgressBarExtensions;
using static installEAS.MenuPanels;
using static installEAS.Animate;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Effects;

//using System.Drawing;

//using static System.Net.Mime.MediaTypeNames;
//using Exception = System.Exception;

namespace installEAS
{


    public partial class MainWindow :Window
    {
        public static MainWindow MainFrame = null;
        public DoubleAnimation _oa, _Close;
        private Timer _timer;

        DropShadowEffect shadowEffectR, shadowEffectL, shadowEffectB, shadowEffectT;
        public MainWindow()
        {
            InitializeComponent();
            
            MainFrame = this;
            Left = 5; Top = 5;
            StateChanged += MainWindowStateChangeRaised;
            SizeChanged += MainWin_SizeChanged;
            ThemesController.CurrentTheme = ThemesController.ThemeTypes.ColorDark;
            switch (ThemesController.CurrentTheme)
            {
                case ThemesController.ThemeTypes.ColorDark:
                    controlFrom = "#FF333333"; controlTo = "#00202020"; closeFrom = "#FF902020"; closeTo = "#00202020";
                    break;
                case ThemesController.ThemeTypes.ColorBlue:
                    controlFrom = "#FF32506E"; controlTo = "#0032506E"; closeFrom = "#FF902020"; closeTo = "#0032506E";
                    break;
            }

            _oa = new DoubleAnimation(); _oa.From = 0.1; _oa.To = 0.97; _oa.Duration = new Duration( TimeSpan.FromMilliseconds( 400 ) );
            _Close = new DoubleAnimation(); _Close.From = 0.97; _Close.To = 0.1; _Close.Duration = new Duration( TimeSpan.FromMilliseconds( 400 ) ); _Close.FillBehavior = FillBehavior.HoldEnd;

            roundw.Visibility = Visibility.Hidden;
            MenuMain.MenuMain.PanelTopMain.IsEnabled = false;
            MenuMain.MenuMain.PanelTopAdd.IsEnabled = false;



            shadowEffectR = new DropShadowEffect
            {
                Color = new Color { A = 255, R = 0, G = 0, B = 0 },
                Direction = 200,
                BlurRadius = 0,
                ShadowDepth = 3,
                Opacity = 0.5
            };

            gridR.Effect = shadowEffectR;
            //this.customContextMenu.Margin = new Thickness( 5 );


        }
        private void MainWindowStateChangeRaised( object sender, EventArgs e )
        {
            if (WindowState == WindowState.Maximized)
            {
                MainWindowBorder.BorderThickness = new Thickness( 7 );
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWindowBorder.BorderThickness = new Thickness( 0 );
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }

        private void MainWin_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            rtb.Document.PageWidth = 10000;
            if (_timer != null) { _timer.Dispose(); _timer = null; }
            _timer = new Timer( _ =>
            {
                DispatcherEx.InvokeOrExecute( Dispatcher, () => { rtb.Document.PageWidth = double.NaN; rtb.ScrollToEnd(); }, DispatcherPriority.Send );
            }, null, 200, Timeout.Infinite );
        }

        public void ChangeTheme()
        {
            switch (ThemesController.CurrentTheme)
            {
                case ThemesController.ThemeTypes.ColorBlue:
                    ThemesController.SetTheme( ThemesController.ThemeTypes.ColorDark );
                    controlFrom = "#FF333333"; controlTo = "#00202020";
                    closeFrom = "#FF902020"; closeTo = "#00202020";
                    break;
                case ThemesController.ThemeTypes.ColorDark:
                    ThemesController.SetTheme( ThemesController.ThemeTypes.ColorBlue );
                    controlFrom = "#FF32506E"; controlTo = "#0032506E";
                    closeFrom = "#FF902020"; closeTo = "#0032506E";
                    break;
            }
        }



        public  void CloseMain()
        {
            SystemCommands.CloseWindow( MainFrame );

        }
        public void CommandBinding_Executed_Close( object sender, ExecutedRoutedEventArgs e ) { SystemCommands.CloseWindow( this ); }

        public void CommandBinding_Executed_Maximize( object sender, ExecutedRoutedEventArgs e ) { SystemCommands.MaximizeWindow( this ); }

        public void CommandBinding_Executed_Minimize( object sender, ExecutedRoutedEventArgs e ) { SystemCommands.MinimizeWindow( this ); }

        public void CommandBinding_Executed_Restore( object sender, ExecutedRoutedEventArgs e ) { SystemCommands.RestoreWindow( this ); }

        private void MainWindow_OnClosing( object sender, CancelEventArgs e )
        {
            e.Cancel = true;
            MessageBoxResult result = MessageBoxEx.Show( "Действительно закрыть приложение?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxButtonDefault.OK );
            if (result.ToString() == "OK")
            {
                _Close.Completed += ( s, _ ) => Process.GetCurrentProcess().Kill();
                BeginAnimation( OpacityProperty, _Close );
            }
        }

        private void MainWindow_OnActivated( object sender, EventArgs e )
        {

        }

        private void MainWindow_OnLoaded( object sender, RoutedEventArgs e )
        {
            BeginAnimation( OpacityProperty, _oa );
        }

        private void MainWindow_OnMouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {

        }

        private void MainWindow_OnSizeChanged( object sender, SizeChangedEventArgs e )
        {

        }

        private new void MouseLeave( object sender, MouseEventArgs e )
        {
            MainFrame.Dispatcher.InvokeAsync( () =>
            {
                var element = (FrameworkElement)sender;
                ColorAnimation( element.Name == "CloseButton" ? new InClassName( element, closeFrom, closeTo, 150 ) : new InClassName( element, controlFrom, controlTo, 150 ) );

            }, DispatcherPriority.Normal );



        }

        private void ThemeButton_OnClick( object sender, RoutedEventArgs e )
        {
            ChangeTheme();
        }

        private void Pb_OnMouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {

        }

        public void ToClip()
        {
            var txt = rtb.Selection.Text.ToString();
            Clipboard.SetText( txt );
            rtb.Selection.Select( rtb.CaretPosition, rtb.CaretPosition );
        }

        private void Rtb_OnMouseMove( object sender, MouseEventArgs e )
        {
            var PosCursor = rtb.CaretPosition.GetTextInRun( LogicalDirection.Forward );
            switch (e.LeftButton)
            {
                case MouseButtonState.Released when !rtb.Selection.IsEmpty:
                    try { ToClip(); } catch(Exception ex) { clog(ex.Message);}
                    break;
                case MouseButtonState.Pressed when PosCursor == "":
                    DragMove();
                    break;
            }
        }

        [STAThread]

        private void Btn1_OnClick( object sender, RoutedEventArgs e )
        {
            //log.mLog("Тестовая мессага", Brushes.GreenYellow);
            //log.mLog(OSBuild.ToString());
            //Class1.Test2();
            ProgressBarSet(100, TimeSpan.FromMilliseconds(5000));


        }
        private void Btn2_OnClick( object sender, RoutedEventArgs e )
        {
            mLog( "This works fantastically! I'm using this with an animation that slides an image on- or off-screen by modifying the RenderTransform of the element, and therefore it needs to know the absolute position of the element", Brushes.GreenYellow );
        }
        private void Btn3_OnClick( object sender, RoutedEventArgs e )
        {
            pb.SetPercent( 0, TimeSpan.FromMilliseconds( 1 ) );
        }
        private async void Btn4_OnClick( object sender, RoutedEventArgs e )
        {
            //Replica.ReplicaGetSqlPackage();
            await Replica.ReplicaSqlPackageStartAsync();

        }

        private void MenuMain_PreviewKeyDown( object sender, KeyEventArgs e )
        {

        }

        private void MainWin_KeyDown( object sender, KeyEventArgs e )
        {
            if (Keyboard.IsKeyDown( Key.X ) && Keyboard.IsKeyDown( Key.LeftAlt )) Close();
            if (Keyboard.IsKeyDown( Key.OemTilde ) && Keyboard.IsKeyDown( Key.LeftAlt ))
            {
                MainFrame.Dispatcher.InvokeAsync( () =>
                {
                    AnimateGridAsync( MenuMain.PanelTopMain, 400 );
                }, DispatcherPriority.Send );

            }

            if (Keyboard.IsKeyDown( Key.OemTilde ) && Keyboard.IsKeyDown( Key.LeftCtrl ))
            {
                MainFrame.Dispatcher.InvokeAsync( () =>
                {
                    AnimateGridAsync( MenuMain.PanelTopAdd, 500 );
                }, DispatcherPriority.Send );

            }



            if (MenuMain.PanelTopAdd.IsEnabled == true)
            {
                if (Keyboard.IsKeyDown( Key.D0 ) || Keyboard.IsKeyDown( Key.NumPad0 )) MenuMain.btnMenuAdd0.RaiseEvent( new RoutedEventArgs( ButtonBase.ClickEvent ) );
                if (Keyboard.IsKeyDown( Key.D1 ) || Keyboard.IsKeyDown( Key.NumPad1 )) MenuMain.btnMenuAdd1.RaiseEvent( new RoutedEventArgs( ButtonBase.ClickEvent ) );
            }
            if (MenuMain.PanelTopMain.IsEnabled == true)
            {
                if (Keyboard.IsKeyDown( Key.D3 ) || Keyboard.IsKeyDown( Key.NumPad3 )) MenuMain.btnMenu3.RaiseEvent( new RoutedEventArgs( ButtonBase.ClickEvent ) );
                if (Keyboard.IsKeyDown( Key.D4 ) || Keyboard.IsKeyDown( Key.NumPad4 )) MenuMain.btnMenu4.RaiseEvent( new RoutedEventArgs( ButtonBase.ClickEvent ) );
            }




            //Animate.AnimateGridAsync( MenuMain.PanelTopMain, "PanelTopMain" );
        }
        private void Btn5_OnClick( object sender, RoutedEventArgs e )
        {
            roundw.Start();
        }

        private  void Btn6_OnClick(object sender, RoutedEventArgs e)
        {


            //Animate.AnimateGridAsync( MenuMain.PanelTopMain, "PanelTopMain" );
            //Console.WriteLine( MenuMain.PanelTopMain.IsEnabled );
            //MenuMain.PanelTopMain.IsEnabled = true;
            //Console.WriteLine( MenuMain.PanelTopMain.IsEnabled );
            //MessageBoxEx.Show( "  Тут произошло нечто непонятное. Мы не знаем что это такое\nЕсли бы знали что это такое, но мы не знаем, что тут произошло", MessageBoxButtonEx.OK, MessageBoxImage.Information );
            //MessageBoxResult result = MessageBoxEx.Show( "Действительно закрыть приложение?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxButtonDefault.OK );
            //await Animate.AnimateGridAsync(InstallEAS.MenuMain.)
        }
    }
    //public static class Animate
    //{
    //    public static void AnimateGridAsync( Grid Target, string TargetName )
    //    {
    //        if (Target.Children != null)
    //        {
    //            var PanelTopPos = -((Target.Children.OfType<StackPanel>().FirstOrDefault()).Children.Count * 40);
    //            var relativePoint = Convert.ToInt32( Target.TransformToAncestor( MainWindow.MainFrame ).Transform( new Point( 0, 0 ) ).Y ) - 30;
    //            var sb = new Storyboard();
    //            var ta = new ThicknessAnimation
    //            {
    //                BeginTime = new TimeSpan( 0 ),
    //                Duration = new Duration( TimeSpan.FromSeconds( 0.5 ) ),
    //                DecelerationRatio = 0.1,
    //                AccelerationRatio = 0,
    //                SpeedRatio = 1
                    
    //            };

    //            Storyboard.SetTargetProperty( ta, new PropertyPath( FrameworkElement.MarginProperty ) );

    //            sb.Children.Add( ta );
    //            if (Target.IsEnabled == false)
    //            {
    //                ta.From = new Thickness( 0, PanelTopPos, 0, 0 );
    //                ta.To = new Thickness( 0, 0, 0, 0 );
    //                sb.Begin( Target );
    //                Target.Opacity = 1;
    //                Target.IsEnabled = true;
    //            }
    //            else
    //            {
                    
    //                ta.From = new Thickness( 0, relativePoint, 0, 0 );
    //                ta.To = new Thickness( 0, PanelTopPos, 0, 0 );
    //                sb.Begin( Target );
    //                Target.Opacity = 0.5;
    //                Target.IsEnabled = false;
    //            }
    //        }

    //        //return Task.CompletedTask;
    //    }

    //}

}
