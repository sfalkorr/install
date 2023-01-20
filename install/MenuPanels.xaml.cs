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
using System.Windows.Media.Animation;
using static installEAS.MainWindow;
using static installEAS.Animate;
using installEAS;
using System.Windows.Threading;

namespace installEAS
{

    public partial class MenuPanels :UserControl
    {


        public MenuPanels()
        {
            InitializeComponent();
            PanelTopMain.IsEnabled = true;
        }
        [STAThread]
        public void btnMainMenu1_OnClick( object sender, RoutedEventArgs e )
        {

        }

        private void BtnMainMenu2_OnClick( object sender, RoutedEventArgs e )
        {

        }

        private  void btnMainMenu3_OnClick( object sender, RoutedEventArgs e )
        {
            Animate.AnimateGridAsync( MenuMain.PanelTopMain, 400 );
            
            Animate.AnimateGridAsync( MenuMain.PanelTopAdd, 500 );
            //MainWindow.MainFrame.Dispatcher.InvokeAsync( async () =>
            //{
            //    await Animate.AnimateGridAsync( MenuMain.PanelTopMain, 400 );
            //    await Task.Delay( 300 );
            //    await Animate.AnimateGridAsync( MenuMain.PanelTopAdd, 500 );
            //}, DispatcherPriority.Send );
        }

        private void BtnMainMenu4_OnClick( object sender, RoutedEventArgs e )
        {
            MainFrame.CloseMain();

        }

        private new void MouseLeave( object sender, MouseEventArgs e )
        {

                var element = (FrameworkElement)sender;
                ColorAnimation( new InClassName( element, controlFrom, controlTo, 150 ) );


            //Dispatcher.InvokeAsync( () =>
            //{
            //    var element = (FrameworkElement)sender;
            //    ColorAnimation( new InClassName( element, controlFrom, controlTo, 150 ) );

            //}, DispatcherPriority.Normal );
        }

        private  void BtnMenuAdd0_OnClick( object sender, RoutedEventArgs e )
        {
            Animate.AnimateGridAsync( MenuMain.PanelTopAdd, 500 );
            
            Animate.AnimateGridAsync( MenuMain.PanelTopMain, 400 );
            //MainWindow.MainFrame.Dispatcher.InvokeAsync( async () =>
            //{
            //    await Animate.AnimateGridAsync( MenuMain.PanelTopAdd, 500 );
            //    await Task.Delay( 300 );
            //    await Animate.AnimateGridAsync( MenuMain.PanelTopMain, 400 );


            //}, DispatcherPriority.Send );

        }
    }
}
