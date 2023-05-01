using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

using static installEAS.Helpers.Log;
using static installEAS.Variables;
using static installEAS.Helpers.Animate;
using static System.Windows.Input.Key;

namespace installEAS.Controls
{
    /// <summary>
    /// Логика взаимодействия для InputBox.xaml
    /// </summary>
    public partial class InputBox
    {
        public InputBox()
        {
            InitializeComponent();


            tbox.IsEnabled = false;
            
            textBox.IsEnabled = false;
        }

        private void TextBox_OnKeyDownKeyDown( object sender, KeyEventArgs e )
        {
            if (e.Key != Enter || textBox.Text == "") return;
            if (Regex.IsMatch( textBox.Text, "^[0-9A-Z!@#$%^&*()_+=?-]+$", RegexOptions.IgnoreCase ))
            {
                log( textBox.Text );
                textBox.IsEnabled = false;
                textBox.Clear();
                //sqlpass = InputBox.textBox.Text;
            }
            else
            {
                log( $"Недопустимый ввод {textBox.Text}", Brushes.Tomato );
                textBox.Clear();
            }
        }

    }


}
